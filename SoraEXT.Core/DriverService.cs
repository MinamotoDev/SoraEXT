using System.Buffers.Binary;
using System.ComponentModel;
using System.IO.Pipes;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Security.Principal;
using Microsoft.Win32;

namespace SoraEXT.Core;

public sealed class DriverService
{
    private const string Root = @"SYSTEM\CurrentControlSet\Services\Ext2Fsd";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public static bool IsAdministrator =>
        new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    public static bool StartupTaskInstalled
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MinamotoDev\SoraEXT");
            return key?.GetValue("StartupTask") as string == "SoraEXTMount";
        }
    }

    public Task<ScanResult> ScanAsync(CancellationToken token = default) =>
        Task.Run(() =>
        {
            var result = Scan(token);
            try
            {
                return result with { Volumes = result.Volumes.Concat(WindowsVolumeService.Scan(token)).ToList() };
            }
            catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                return result with { Errors = result.Errors.Append(L.Get("Windows 分区：{0}", ex.Message)).ToList() };
            }
        }, token);

    internal static bool IsLinuxDataPartition(string? gptType, uint mbrType) =>
        string.Equals(gptType?.Trim('{', '}'), "0fc63daf-8483-4772-8e79-3d69d8477de4",
            StringComparison.OrdinalIgnoreCase) || mbrType == 0x83;

    internal static bool ShouldScanExtPartition(string? gptType, uint mbrType,
        bool isBoot, bool isSystem, bool isHidden) =>
        !isBoot && !isSystem && (IsLinuxDataPartition(gptType, mbrType) || !isHidden);

    private static ScanResult Scan(CancellationToken token)
    {
        if (!IsAdministrator) return ScanMounted(token);
        var volumes = new List<Volume>();
        var errors = new List<string>();
        bool running = false, systemStart;
        string state;
        using (var key = Registry.LocalMachine.OpenSubKey(Root))
        {
            systemStart = key?.GetValue("Start") is int start && start == 1;
            state = L.Get(key is null ? "未安装" : "已停止");
        }
        try
        {
            using var svc = new ServiceController("Ext2Fsd");
            running = svc.Status == ServiceControllerStatus.Running;
            if (running) state = L.Get("运行中");
        }
        catch (InvalidOperationException) { }

        var letters = Enumerable.Range('A', 26).ToDictionary(i => (char)i, i => Native.Target((char)i));
        var disks = new Dictionary<int, string>();
        using (var search = new ManagementObjectSearcher("SELECT Index,Model FROM Win32_DiskDrive"))
        using (var result = search.Get())
            foreach (ManagementObject disk in result)
                using (disk) disks[Convert.ToInt32(disk["Index"])] = disk["Model"]?.ToString() ?? L.Get("磁盘");

        using var partitions = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage",
            "SELECT DiskNumber,PartitionNumber,Offset,Size,GptType,MbrType,IsBoot,IsSystem,IsHidden FROM MSFT_Partition");
        using var rows = partitions.Get();
        foreach (ManagementObject row in rows)
        {
            using (row)
            {
                token.ThrowIfCancellationRequested();
                string? gptType = row["GptType"]?.ToString();
                uint mbrType = Convert.ToUInt32(row["MbrType"]);
                // Windows marks Linux data partitions hidden as well. Identify
                // Linux partition types before filtering Windows hidden volumes.
                if (!ShouldScanExtPartition(gptType, mbrType, Convert.ToBoolean(row["IsBoot"]),
                    Convert.ToBoolean(row["IsSystem"]), Convert.ToBoolean(row["IsHidden"]))) continue;
                var disk = Convert.ToInt32(row["DiskNumber"]);
                var part = Convert.ToInt32(row["PartitionNumber"]);
                var target = $@"\Device\Harddisk{disk}\Partition{part}";
                var letter = letters.FirstOrDefault(p => string.Equals(p.Value, target, StringComparison.OrdinalIgnoreCase)).Key;
                // Disk IOCTL resolves aliases (HarddiskVolumeN versus HarddiskN\PartitionN).
                if (letter == default)
                {
                    foreach (var pair in letters.Where(p => p.Value.Length > 0))
                    {
                        try
                        {
                        using var h = Native.Open($@"\\.\{pair.Key}:", queryOnly: true);
                            var buffer = Native.Ioctl(h, 0x2d1080, new byte[12]); // STORAGE_GET_DEVICE_NUMBER
                            if (BitConverter.ToInt32(buffer, 4) == disk && BitConverter.ToInt32(buffer, 8) == part)
                            { letter = pair.Key; break; }
                        }
                        catch (Win32Exception) { }
                    }
                }
                var linux = IsLinuxDataPartition(gptType, mbrType);
                if (!linux && letter != default)
                {
                    try { linux = new DriveInfo($"{letter}:\\").DriveFormat.StartsWith("EXT", StringComparison.OrdinalIgnoreCase); }
                    catch (IOException) { }
                }
                if (!linux) continue;
                try
                {
                    using var physical = Native.Open($@"\\.\PhysicalDrive{disk}");
                    var data = new byte[1024];
                    var count = RandomAccess.Read(physical, data, checked(Convert.ToInt64(row["Offset"]) + 1024));
                    if (count != data.Length) throw new IOException(L.Get("超级块读取不完整。"));
                    var sb = DriverProtocol.ParseSuperblock(data);
                    if (sb is null) continue;
                    using var config = Registry.LocalMachine.OpenSubKey(Root + @"\Volumes");
                    var options = config?.GetValue(sb.Value.Id)?.ToString() ?? "";
                    bool ro = options.Split(';').Contains("Readonly", StringComparer.OrdinalIgnoreCase);
                    string error = "";
                    if (letter != default && running)
                    {
                        try
                        {
                            using var h = Native.Open($@"\\.\{letter}:");
                            var properties = DriverProtocol.Query(h);
                            ro = properties[12] != 0;
                        }
                        catch (Win32Exception ex) { error = ex.Message; }
                    }
                    volumes.Add(new Volume
                    {
                        Id = sb.Value.Id, Label = string.IsNullOrEmpty(sb.Value.Label) ? L.Get("Linux 分区") : sb.Value.Label,
                        FileSystem = sb.Value.FileSystem,
                        Device = target, DiskName = disks.GetValueOrDefault(disk, L.Get("磁盘")),
                        DiskNumber = disk, PartitionNumber = part, Size = Convert.ToInt64(row["Size"]),
                        Letter = letter == default ? "" : letter.ToString(), Options = options, ReadOnly = ro,
                        AutoMount = options.Contains("MountPoint=", StringComparison.OrdinalIgnoreCase),
                        IncompatFeatures = sb.Value.Incompat, ReadOnlyFeatures = sb.Value.ReadOnly, Error = error,
                        FreeSpace = GetFreeSpace(letter)
                    });
                }
                catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
                { errors.Add(L.Get("磁盘 {0} / 分区 {1}：{2}", disk, part, ex.Message)); }
            }
        }
        return new ScanResult(volumes, state, running, systemStart, errors);
    }

    private static long? GetFreeSpace(char letter)
    {
        if (letter == default) return null;
        try { return new DriveInfo($"{letter}:\\").AvailableFreeSpace; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    // Browsing and launching the UI never require elevation. Raw partition
    // discovery is reserved for the explicitly elevated management session.
    private static ScanResult ScanMounted(CancellationToken token)
    {
        var volumes = new List<Volume>();
        var errors = new List<string>();
        bool running = false;
        using var root = Registry.LocalMachine.OpenSubKey(Root);
        using var config = Registry.LocalMachine.OpenSubKey(Root + @"\Volumes");
        try
        {
            using var svc = new ServiceController("Ext2Fsd");
            running = svc.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException) { }
        foreach (var drive in DriveInfo.GetDrives())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (!drive.IsReady || !drive.DriveFormat.StartsWith("EXT", StringComparison.OrdinalIgnoreCase)) continue;
                char letter = drive.Name[0];
                string id = "", options = "";
                bool readOnly = false, verified = false;
                int disk = -1, part = -1;
                using var handle = Native.Open($@"\\.\{letter}:", queryOnly: true);
                try
                {
                    var props = DriverProtocol.Query(handle);
                    id = DriverProtocol.Id(props.AsSpan(48, 16));
                    readOnly = props[12] != 0;
                    verified = true;
                }
                catch (Win32Exception)
                {
                    id = config?.GetValueNames().FirstOrDefault(name =>
                        config.GetValue(name)?.ToString()?.Contains($"MountPoint={letter}:", StringComparison.OrdinalIgnoreCase) == true) ?? "";
                }
                options = config?.GetValue(id)?.ToString() ?? "";
                if (!verified) readOnly = Native.IsVolumeReadOnly(letter);
                try
                {
                    var number = Native.Ioctl(handle, 0x2d1080, new byte[12]);
                    disk = BitConverter.ToInt32(number, 4);
                    part = BitConverter.ToInt32(number, 8);
                }
                catch (Win32Exception) { }
                volumes.Add(new Volume
                {
                    Id = id.Length > 0 ? id : $"mounted:{letter}",
                    Label = drive.VolumeLabel.Length > 0 ? drive.VolumeLabel : L.Get("Linux 分区"),
                    FileSystem = drive.DriveFormat.ToUpperInvariant(),
                    Device = disk >= 0 ? $@"\Device\Harddisk{disk}\Partition{part}" : Native.Target(letter),
                    DiskName = disk >= 0 ? L.Get("磁盘 {0}", disk) : L.Get("本地磁盘"),
                    DiskNumber = disk, PartitionNumber = part, Size = drive.TotalSize,
                    FreeSpace = drive.AvailableFreeSpace, Letter = letter.ToString(),
                    Options = options, ReadOnly = readOnly, IdentityVerified = verified,
                    FeaturesKnown = false, AutoMount = options.Contains("MountPoint=", StringComparison.OrdinalIgnoreCase)
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
            { errors.Add($"{drive.Name}：{ex.Message}"); }
        }
        return new(volumes, L.Get(running ? "运行中" : root is null ? "未安装" : "已停止"),
            running, root?.GetValue("Start") is int start && start == 1, errors);
    }

    public static IReadOnlyList<string> AvailableLetters(string current) =>
        Enumerable.Range('D', 23).Select(i => ((char)i).ToString())
            .Where(l => l == current || Native.Target(l[0]).Length == 0).ToList();

    public async Task ApplyAsync(Volume expected, char letter, bool readOnly, bool autoMount)
    {
        if (!IsAdministrator) throw new UnauthorizedAccessException(L.Get("请以管理员身份管理分区。"));
        await Gate.WaitAsync();
        try
        {
            if (expected.IsWindowsVolume)
            {
                await Task.Run(() => WindowsVolumeService.ChangeLetter(expected, letter));
                return;
            }
            await Task.Run(async () =>
            {
                // Re-read disk identity immediately before any state change.
                var actual = Scan(default).Volumes.SingleOrDefault(v => v.Id == expected.Id && v.Device == expected.Device)
                    ?? throw new IOException(L.Get("分区已断开，请刷新。"));
                if (actual.Mounted && actual.Letter[0] != letter)
                    throw new IOException(L.Get("请先卸载当前盘符。"));
                if (!actual.Mounted && Native.Target(letter).Length != 0)
                    throw new IOException(L.Get("{0}: 已被占用。", letter));
                using var key = Registry.LocalMachine.CreateSubKey(Root + @"\Volumes", true);
                var old = key.GetValue(actual.Id) as string;
                var next = VolumeOptions.Merge(old ?? "", readOnly, autoMount, letter);
                bool added = false;
                byte[]? oldProperties = null;
                try
                {
                    key.SetValue(actual.Id, next, RegistryValueKind.String);
                    if (!actual.Mounted)
                    {
                        await MapAsync(letter, actual.Device, false);
                        added = true;
                    }
                    using var h = Native.Open($@"\\.\{letter}:");
                    oldProperties = DriverProtocol.Query(h);
                    if (DriverProtocol.Id(oldProperties.AsSpan(48, 16)) != actual.Id)
                        throw new IOException(L.Get("卷标识发生变化，请刷新。"));
                    var properties = DriverProtocol.Update(oldProperties, readOnly, letter, autoMount);
                    Native.Ioctl(h, DriverProtocol.PropertyIoctl, properties);
                    if (!readOnly && DriverProtocol.Query(h)[12] != 0)
                        throw new IOException(L.Get("驱动将此分区保持为只读，请检查 ext4 特性。"));
                    Native.SHChangeNotify(0x100, 0x3005, $"{letter}:\\", 0);
                }
                catch (Exception original)
                {
                    var rollbackErrors = new List<string>();
                    try
                    {
                        if (oldProperties is not null)
                        {
                            using var h = Native.Open($@"\\.\{letter}:");
                            BinaryPrimitives.WriteUInt32LittleEndian(oldProperties.AsSpan(8), 7);
                            var flags = BinaryPrimitives.ReadUInt64LittleEndian(oldProperties.AsSpan(136));
                            BinaryPrimitives.WriteUInt64LittleEndian(oldProperties.AsSpan(136), flags & ~1ul);
                            Native.Ioctl(h, DriverProtocol.PropertyIoctl, oldProperties);
                        }
                    }
                    catch (Exception ex) { rollbackErrors.Add(ex.Message); }
                    try { if (added) await MapAsync(letter, actual.Device, true); }
                    catch (Exception ex) { rollbackErrors.Add(ex.Message); }
                    if (old is null) key.DeleteValue(actual.Id, false); else key.SetValue(actual.Id, old);
                    if (rollbackErrors.Count > 0)
                        throw new IOException(original.Message + "\n" + L.Get("恢复配置：{0}", string.Join("\n", rollbackErrors)), original);
                    throw;
                }
            });
        }
        finally { Gate.Release(); }
    }

    public async Task UnmountAsync(Volume expected, IProgress<string>? progress = null)
    {
        if (!IsAdministrator) throw new UnauthorizedAccessException(L.Get("请以管理员身份管理分区。"));
        progress?.Report(L.Get("等待前一项操作"));
        await Gate.WaitAsync();
        try
        {
            if (expected.IsWindowsVolume)
            {
                progress?.Report(L.Get("通过 Windows 移除盘符"));
                await Task.Run(() => WindowsVolumeService.ChangeLetter(expected, null));
                progress?.Report(L.Get("盘符已移除"));
                return;
            }
            await Task.Run(async () =>
            {
                progress?.Report(L.Get("核对分区身份"));
                var actual = Scan(default).Volumes.SingleOrDefault(v => v.Id == expected.Id && v.Device == expected.Device)
                    ?? throw new IOException(L.Get("分区已断开，请刷新。"));
                if (!actual.Mounted) return;
                progress?.Report(L.Get("打开卷"));
                using var h = Native.Open($@"\\.\{actual.Letter}:", !actual.ReadOnly);
                progress?.Report(L.Get("核对驱动 UUID"));
                if (DriverProtocol.Id(DriverProtocol.Query(h).AsSpan(48, 16)) != actual.Id)
                    throw new IOException(L.Get("卷标识发生变化，请刷新。"));
                progress?.Report(L.Get("锁定卷并等待驱动刷新缓存"));
                Native.Ioctl(h, 0x90018); // FSCTL_LOCK_VOLUME: open files block unmount.
                try
                {
                    progress?.Report(L.Get("刷新文件缓冲区"));
                    if (!actual.ReadOnly && !Native.FlushFileBuffers(h)) throw new Win32Exception();
                    progress?.Report(L.Get("卸载文件系统"));
                    Native.Ioctl(h, 0x90020); // FSCTL_DISMOUNT_VOLUME
                    progress?.Report(L.Get("通过 Ext2Srv 移除盘符"));
                    await MapAsync(actual.Letter[0], Native.Target(actual.Letter[0]), true);
                }
                finally
                {
                    progress?.Report(L.Get("释放卷锁"));
                    Native.Ioctl(h, 0x9001c);
                }
                // Close the volume before notifying Shell consumers. FLUSHNOWAIT
                // avoids waiting for every Explorer/extension notification handler.
                progress?.Report(L.Get("关闭卷句柄"));
                h.Dispose();
                progress?.Report(L.Get("通知资源管理器"));
                Native.SHChangeNotify(0x80, 0x3005, $"{actual.Letter}:\\", 0);
                progress?.Report(L.Get("卸载完成"));
            });
        }
        finally { Gate.Release(); }
    }

    private static async Task MapAsync(char letter, string device, bool remove)
    {
        using var svc = new ServiceController("Ext2Srv");
        if (svc.Status != ServiceControllerStatus.Running)
        {
            svc.Start();
            svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
        }
        // Official LocalSystem service creates globally visible DOS drive mappings.
        using var pipe = new NamedPipeClientStream(".", "EXT2MGR_PSRV", PipeDirection.InOut, PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await pipe.ConnectAsync(timeout.Token);
        var name = Encoding.ASCII.GetBytes(device + "\0");
        var request = new byte[28 + name.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(request, 0xbad0bad8);
        BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(8), remove ? 0xbad00003u : 0xbad00002u);
        BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(12), (uint)request.Length);
        BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(16), Environment.ProcessId);
        BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(20), remove ? 7u : 1u);
        request[24] = (byte)letter;
        name.CopyTo(request, 28);
        await pipe.WriteAsync(request, timeout.Token);
        var header = new byte[16];
        await pipe.ReadExactlyAsync(header, timeout.Token);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != 0xbad0bad8 || length is < 28 or > 4096)
            throw new IOException(L.Get("挂载服务响应无效。"));
        var body = new byte[length - 16];
        await pipe.ReadExactlyAsync(body, timeout.Token);
        if (body[9] == 0) throw new IOException(L.Get(remove ? "盘符移除失败。" : "盘符挂载失败。"));
    }
}
