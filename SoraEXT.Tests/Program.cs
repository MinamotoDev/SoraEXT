using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SoraEXT.Core;

int checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
    checks++;
}

// Independently model the native C ABI, including x64 padding and bitfields.
Check(Marshal.SizeOf<NativeVolumeProperty>() == DriverProtocol.PropertySize, "Native structure size");
Check(Marshal.OffsetOf<NativeVolumeProperty>(nameof(NativeVolumeProperty.Uuid)).ToInt32() == 48, "UUID offset");
Check(Marshal.OffsetOf<NativeVolumeProperty>(nameof(NativeVolumeProperty.Flags2)).ToInt32() == 136, "Flags2 offset");
var query = DriverProtocol.QueryRequest();
Check(BinaryPrimitives.ReadUInt32LittleEndian(query) == 0x4556504d, "Protocol magic");
Check(BinaryPrimitives.ReadUInt32LittleEndian(query.AsSpan(8)) == 6, "Query command");
var original = new byte[272];
Random.Shared.NextBytes(original);
original[4] = original[5] = original[6] = original[7] = 0;
var updated = DriverProtocol.Update(original, false, 'F', true);
Check(updated[12] == 0 && updated[13] == 1 && updated[64] == 0xc6, "Write and mount settings");
Check((BinaryPrimitives.ReadUInt64LittleEndian(updated.AsSpan(136)) & 1) == 0, "Global automount flag is never changed");
Check(updated.AsSpan(148, 8).SequenceEqual(original.AsSpan(148, 8)), "UID/GID preserved");
Check(updated.AsSpan(16, 32).SequenceEqual(original.AsSpan(16, 32)), "Codepage preserved");
Check(updated.AsSpan(68, 64).SequenceEqual(original.AsSpan(68, 64)), "Name filters preserved");
Check(!updated.SequenceEqual(original), "Update produces a separate buffer");
var unmountOnBoot = DriverProtocol.Update(original, true, 'F', false);
Check(unmountOnBoot[64] == 0 && unmountOnBoot[12] == 1, "Manual read-only mode");
try { DriverProtocol.Update(original, false, ':', true); throw new Exception("Invalid letter accepted"); }
catch (ArgumentOutOfRangeException) { checks++; }

var options = VolumeOptions.Merge("Readonly;MountPoint=E:;CodePage=utf8;uid=1000;gid=1000;HidingPrefix=.;", false, true, 'F');
Check(options.Contains("uid=1000;gid=1000;HidingPrefix=.;"), "Unrelated configuration preserved");
Check(options.Contains("MountPoint=F:;") && !options.Contains("Readonly"), "Read/write conversion");
Check(!VolumeOptions.Merge(options, true, false, 'F').Contains("MountPoint"), "Startup mapping disabled");
Check(VolumeOptions.MountLetter(options) == 'F', "Startup letter parser");
Check(VolumeOptions.MountLetter("MountPoint=C:;") is null, "System letter rejected");
Check(VolumeOptions.MountLetter("MountPoint=FF:;") is null, "Malformed startup letter rejected");
Check(VolumeOptions.MountLetter("Readonly;") is null, "Manual mount excluded from boot task");
Check(DriverProtocol.PropertyIoctl == 0x221f40, "IOCTL matches FILE_DEVICE_UNKNOWN / function 2000");

var superblock = new byte[1024];
Check(DriverProtocol.ParseSuperblock(superblock) is null, "Non-ext filesystem rejected");
BinaryPrimitives.WriteUInt16LittleEndian(superblock.AsSpan(56), 0xef53);
Enumerable.Range(0, 16).Select(i => (byte)i).ToArray().CopyTo(superblock, 104);
System.Text.Encoding.UTF8.GetBytes("资料").CopyTo(superblock, 120);
BinaryPrimitives.WriteUInt32LittleEndian(superblock.AsSpan(96), 0x80);
var parsed = DriverProtocol.ParseSuperblock(superblock)!.Value;
Check(parsed.Label == "资料" && parsed.Incompat == 0x80, "UTF-8 label and ext4 feature parsing");
Check(parsed.Id == "{00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F}", "On-disk UUID byte order");
Check(parsed.FileSystem == "EXT4", "64-bit ext filesystem identified as EXT4");
BinaryPrimitives.WriteUInt32LittleEndian(superblock.AsSpan(96), 2);
BinaryPrimitives.WriteUInt32LittleEndian(superblock.AsSpan(92), 4);
Check(DriverProtocol.ParseSuperblock(superblock)!.Value.FileSystem == "EXT3", "Journaled legacy ext filesystem");
BinaryPrimitives.WriteUInt32LittleEndian(superblock.AsSpan(92), 0);
Check(DriverProtocol.ParseSuperblock(superblock)!.Value.FileSystem == "EXT2", "Legacy ext filesystem without journal");
const string linuxType = "{0fc63daf-8483-4772-8e79-3d69d8477de4}";
Check(DriverService.ShouldScanExtPartition(linuxType, 0, false, false, true),
    "Regression: Windows-hidden Linux GPT data partition remains discoverable");
Check(DriverService.ShouldScanExtPartition(null, 0x83, false, false, true),
    "Linux MBR data partition remains discoverable");
Check(!DriverService.ShouldScanExtPartition("{de94bba4-06d1-4d40-a16a-bfd50179d6ac}", 0, false, false, true),
    "Hidden Windows recovery partition remains excluded");
Check(!DriverService.ShouldScanExtPartition("{c12a7328-f81f-11d2-ba4b-00a0c93ec93b}", 0, false, true, true),
    "EFI system partition remains excluded");

Check(L.ResolveLanguage("zh-TW") == "zh-Hant" && L.ResolveLanguage("zh-HK") == "zh-Hant" &&
    L.ResolveLanguage("zh-Hant") == "zh-Hant", "Traditional Chinese locale mapping");
Check(L.ResolveLanguage("zh-CN") == "zh-Hans" && L.ResolveLanguage("ja-JP") == "ja" &&
    L.ResolveLanguage("en-GB") == "en" && L.ResolveLanguage("de-DE") == "en", "Supported language fallback");
Check(L.ResolveLanguage("ko-KR") == "ko" && L.ResolveLanguage("ko") == "ko" &&
    L.ResolveLanguage("kok-IN") == "en", "Korean locale mapping and unrelated language fallback");
Check(L.Resources.All(pair => pair.Value.Length == 4 && pair.Value.All(value => !string.IsNullOrWhiteSpace(value))),
    "Every localized string has all four translations");
Check(L.Resources.All(pair => pair.Value.All(value =>
    CompositeFormat.Parse(value).MinimumArgumentCount == CompositeFormat.Parse(pair.Key).MinimumArgumentCount &&
    Regex.Matches(value, @"\{(\d+)").Select(m => m.Groups[1].Value).Order().SequenceEqual(
        Regex.Matches(pair.Key, @"\{(\d+)").Select(m => m.Groups[1].Value).Order()))),
    "All translation format placeholders match the source");
var originalLanguage = L.LanguageTag;
foreach (string language in new[] { "zh-Hans", "zh-Hant", "ja", "en", "ko" })
{
    L.Configure(language);
    int index = language switch { "zh-Hant" => 0, "ja" => 1, "en" => 2, "ko" => 3, _ => -1 };
    Check(L.Resources.All(pair => CompositeFormat.Parse(pair.Key).MinimumArgumentCount > 0 ||
        L.Get(pair.Key) == (index < 0 ? pair.Key : pair.Value[index])), $"Resource lookup: {language}");
    Check(L.Get("磁盘 {0} · 分区 {1}", 2, 3).Contains('2') &&
        L.Get("更新于 {0:HH:mm:ss} · {1:N0} ms", DateTime.Now, 12d).Contains("ms"), $"Localized formatting: {language}");
}
L.Configure(originalLanguage);

if (args.Contains("--scan"))
{
    var timer = Stopwatch.StartNew();
    var result = await new DriverService().ScanAsync();
    Console.WriteLine($"Scan: {timer.ElapsedMilliseconds} ms; driver={result.DriverStatus}; volumes={result.Volumes.Count}");
    foreach (var volume in result.Volumes)
        Console.WriteLine($"{volume.DisplayLetter} {volume.Label} {volume.FileSystem} {volume.State} protected={volume.IsProtected} identity={volume.IdentityVerified} {volume.Id}");
    foreach (var error in result.Errors) Console.WriteLine($"Scan warning: {error}");
    if (args.Contains("--expect-f"))
        Check(result.Volumes.Any(v => v.Letter == "F" && v.Size > 0 && v.Label == "Spare"), "Live F: volume enumeration");
    if (args.Contains("--expect-spare"))
        Check(result.Volumes.Any(v => v.Label == "Spare" && v.FileSystem == "EXT4" &&
            v.Id == "{88-44-E7-1D-4C-93-62-47-86-49-43-1D-E0-C5-C7-EC}" && v.IdentityVerified),
            "Live unmounted ext4 Spare discovered from its superblock");
    if (args.Contains("--expect-native"))
    {
        Check(result.Errors.Count == 0, "Live scan completed without provider errors");
        string systemLetter = Path.GetPathRoot(Environment.SystemDirectory)![..1];
        var system = result.Volumes.Single(v => v.IsWindowsVolume && v.Letter == systemLetter);
        Check(system.IsProtected, "Live Windows system partition is protected");
        foreach (var volume in result.Volumes.Where(v => v.IsWindowsVolume && v.Mounted))
        {
            var drive = new DriveInfo($"{volume.Letter}:\\");
            Check(drive.DriveFormat.Equals(volume.FileSystem, StringComparison.OrdinalIgnoreCase) &&
                drive.TotalSize == volume.Size, "Native backend matches independent filesystem and capacity query");
        }
    }
}
Console.WriteLine($"PASS: {checks} checks");

[StructLayout(LayoutKind.Sequential)]
struct NativeVolumeProperty
{
    public uint Magic, Flags, Command;
    public byte ReadOnly, Ext3Writable, Ext2, Ext3;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Codepage;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Uuid;
    public byte Letter, CheckBitmap, HidingPrefix, HidingSuffix;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Prefix;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Suffix;
    public ulong Flags2;
    public uint Bitfields;
    public ushort Uid, Gid, Euid, Egid;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)] public uint[] Reserved;
}
