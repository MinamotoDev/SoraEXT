using System.Management;

namespace SoraEXT.Core;

// Windows Storage Management owns native NTFS/ReFS drive-letter assignments.
// This backend does not communicate with Ext4Fsd or Ext2Srv.
internal static class WindowsVolumeService
{
    private const string Scope = @"root\Microsoft\Windows\Storage";

    internal static IReadOnlyList<Volume> Scan(CancellationToken token)
    {
        var result = new List<Volume>();
        using var search = new ManagementObjectSearcher(Scope, "SELECT * FROM MSFT_Partition");
        using var partitions = search.Get();
        foreach (ManagementObject partition in partitions)
        using (partition)
        {
            token.ThrowIfCancellationRequested();
            string type = partition["GptType"]?.ToString()?.Trim('{', '}') ?? "";
            if (Convert.ToBoolean(partition["IsSystem"]) || Convert.ToBoolean(partition["IsHidden"]) ||
                type.Equals("c12a7328-f81f-11d2-ba4b-00a0c93ec93b", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("e3c9e316-0b5c-4db8-817d-f92df00215ae", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("de94bba4-06d1-4d40-a16a-bfd50179d6ac", StringComparison.OrdinalIgnoreCase))
                continue;
            using var volumes = partition.GetRelated("MSFT_Volume");
            foreach (ManagementObject volume in volumes)
            using (volume)
            {
                string fs = volume["FileSystem"]?.ToString() ?? "";
                if (!fs.Equals("NTFS", StringComparison.OrdinalIgnoreCase) &&
                    !fs.Equals("ReFS", StringComparison.OrdinalIgnoreCase)) continue;
                string id = volume["Path"]?.ToString() ?? "";
                if (id.Length == 0) continue;
                string letter = partition["DriveLetter"]?.ToString()?.Trim('\0', ' ', ':') ?? "";
                var disk = Convert.ToInt32(partition["DiskNumber"]);
                result.Add(new Volume
                {
                    Id = id, Device = partition.Path.Path,
                    Label = string.IsNullOrWhiteSpace(volume["FileSystemLabel"]?.ToString())
                        ? L.Get("{0} 分区", fs) : volume["FileSystemLabel"].ToString()!,
                    DiskName = L.Get("磁盘 {0}", disk), DiskNumber = disk,
                    PartitionNumber = Convert.ToInt32(partition["PartitionNumber"]),
                    Size = Convert.ToInt64(volume["Size"]),
                    FreeSpace = volume["SizeRemaining"] is null ? null : Convert.ToInt64(volume["SizeRemaining"]),
                    Letter = letter, FileSystem = fs.Equals("NTFS", StringComparison.OrdinalIgnoreCase) ? "NTFS" : "ReFS",
                    ReadOnly = Convert.ToBoolean(partition["IsReadOnly"]),
                    IsProtected = Convert.ToBoolean(partition["IsBoot"]) ||
                        Convert.ToBoolean(partition["IsSystem"]) || Convert.ToBoolean(partition["IsHidden"]) ||
                        string.Equals(partition["GptType"]?.ToString()?.Trim('{', '}'),
                            "de94bba4-06d1-4d40-a16a-bfd50179d6ac", StringComparison.OrdinalIgnoreCase),
                    FeaturesKnown = false, AutoMount = letter.Length == 1
                });
            }
        }
        return result;
    }

    internal static void ChangeLetter(Volume expected, char? letter)
    {
        // Match both volume GUID and partition identity after refreshing topology.
        var actual = Scan(default).SingleOrDefault(v =>
            v.Id.Equals(expected.Id, StringComparison.OrdinalIgnoreCase) && v.Device == expected.Device)
            ?? throw new IOException(L.Get("分区已断开，请刷新。"));
        if (actual.FileSystem == "NTFS") throw new NotSupportedException(L.Get("NTFS 分区仅支持在文件资源管理器中打开。"));
        if (actual.IsProtected) throw new IOException(L.Get("系统、启动及隐藏分区仅供查看。"));
        if (actual.Letter != expected.Letter) throw new IOException(L.Get("盘符已变化，请刷新。"));
        if (letter is not null && (letter < 'D' || letter > 'Z'))
            throw new ArgumentOutOfRangeException(nameof(letter));
        if (letter is char target)
        {
            if (actual.Mounted)
            {
                if (actual.Letter[0] == target) return;
                throw new IOException(L.Get("更改盘符前请先移除现有盘符。"));
            }
            if (Native.Target(target).Length > 0) throw new IOException(L.Get("{0}: 已被占用。", target));
        }
        else if (!actual.Mounted) return;
        using var partition = new ManagementObject(actual.Device);
        string method = letter is null ? "RemoveAccessPath" : "AddAccessPath";
        using var input = partition.GetMethodParameters(method);
        input["AccessPath"] = $"{letter ?? actual.Letter[0]}:\\";
        using var output = partition.InvokeMethod(method, input, null);
        uint code = output is null ? 2 : Convert.ToUInt32(output["ReturnValue"]);
        if (code != 0) throw new IOException(L.Get("Windows 盘符操作失败（{0}）。", code));
        Native.SHChangeNotify(letter is null ? 0x80u : 0x100u, 0x3005,
            $"{letter ?? actual.Letter[0]}:\\", 0);
    }
}
