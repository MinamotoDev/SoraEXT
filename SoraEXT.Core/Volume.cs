namespace SoraEXT.Core;

public sealed record Volume
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Device { get; init; }
    public required string DiskName { get; init; }
    public int DiskNumber { get; init; }
    public int PartitionNumber { get; init; }
    public long Size { get; init; }
    public string Letter { get; init; } = "";
    public string Options { get; init; } = "";
    public string FileSystem { get; init; } = "EXT4";
    public bool IsWindowsVolume => FileSystem is "NTFS" or "ReFS";
    public bool IsProtected { get; init; }
    public bool ReadOnly { get; init; }
    public bool AutoMount { get; init; }
    public string Error { get; init; } = "";
    public uint IncompatFeatures { get; init; }
    public uint ReadOnlyFeatures { get; init; }
    public bool IdentityVerified { get; init; } = true;
    public bool FeaturesKnown { get; init; } = true;
    public long? FreeSpace { get; init; }
    public string FreeSpaceText => FreeSpace is long free ? L.Get("{0:N1} GiB 可用", free / 1073741824d) : "—";
    public double UsedPercent => Size > 0 && FreeSpace is long free ? Math.Clamp(100d * (Size - free) / Size, 0, 100) : 0;
    public string AutoMountText => L.Get(IsWindowsVolume ? "Windows 管理盘符" : AutoMount ? "开机挂载" : "手动挂载");
    public bool Mounted => Letter.Length == 1;
    public string DisplayLetter => Mounted ? $"{Letter}:" : "—";
    public string Location => L.Get("磁盘 {0} · 分区 {1}", DiskNumber, PartitionNumber);
    public string Capacity => $"{Size / 1073741824d:N1} GiB";
    public string State => L.Get(Error.Length > 0 ? "待检查" : Mounted ? (ReadOnly ? "只读" : "读写") : "未挂载");
    public string Subtitle => $"{FileSystem} · {Location} · {Capacity}";
    public string Uuid => Id.Trim('{', '}').Replace("-", "").ToLowerInvariant() is var s && s.Length == 32
        ? $"{s[..8]}-{s[8..12]}-{s[12..16]}-{s[16..20]}-{s[20..]}" : Id;
}

public sealed record ScanResult(IReadOnlyList<Volume> Volumes, string DriverStatus, bool DriverRunning,
    bool SystemStart, IReadOnlyList<string> Errors);

public static class VolumeOptions
{
    public static char? MountLetter(string options)
    {
        var value = options.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(p => p.StartsWith("MountPoint=", StringComparison.OrdinalIgnoreCase));
        if (value is null) return null;
        var letter = value["MountPoint=".Length..].Trim().TrimEnd(':').ToUpperInvariant();
        return letter.Length == 1 && letter[0] is >= 'D' and <= 'Z' ? letter[0] : null;
    }
    // Preserve settings owned by Ext2Mgr, including UID/GID and name filters.
    public static string Merge(string previous, bool readOnly, bool autoMount, char letter)
    {
        if (letter is < 'A' or > 'Z') throw new ArgumentOutOfRangeException(nameof(letter));
        var parts = previous.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !new[] { "Readonly", "WritingSupport", "Ext3ForceWriting", "MountPoint" }
                .Contains(p.Split('=')[0], StringComparer.OrdinalIgnoreCase)).ToList();
        parts.Insert(0, readOnly ? "Readonly" : "Ext3ForceWriting");
        if (autoMount) parts.Add($"MountPoint={letter}:");
        if (!parts.Any(p => p.StartsWith("CodePage=", StringComparison.OrdinalIgnoreCase)))
            parts.Add("CodePage=utf8");
        return string.Join(';', parts) + ";";
    }
}
