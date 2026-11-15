using System.Buffers.Binary;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SoraEXT.Core;

public static class DriverProtocol
{
    // Verified against Ext4Fsd/include/common.h (MSVC x64 default packing).
    public const int PropertySize = 272;
    internal const uint PropertyIoctl = (0x22u << 16) | (2000u << 2);
    public static byte[] QueryRequest()
    {
        var bytes = new byte[PropertySize];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x4556504d); // 'EVPM'
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 6);
        return bytes;
    }

    public static string Id(ReadOnlySpan<byte> uuid) =>
        "{" + string.Join("-", uuid.ToArray().Select(b => b.ToString("X2"))) + "}";

    internal static byte[] Query(SafeFileHandle handle)
    {
        var result = Native.Ioctl(handle, PropertyIoctl, QueryRequest());
        if (BinaryPrimitives.ReadUInt32LittleEndian(result) != 0x4556504d)
            throw new IOException(L.Get("驱动返回了无效的卷信息。"));
        return result;
    }

    public static byte[] Update(byte[] current, bool readOnly, char letter, bool autoMount)
    {
        if (current.Length != PropertySize) throw new ArgumentException("Invalid volume property size.");
        if (letter is < 'A' or > 'Z') throw new ArgumentOutOfRangeException(nameof(letter));
        var bytes = (byte[])current.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 7);
        bytes[12] = readOnly ? (byte)1 : (byte)0;
        bytes[13] = readOnly ? (byte)0 : (byte)1;
        bytes[64] = autoMount ? (byte)(letter | 0x80) : (byte)0;
        // The volume command's AUTOMOUNT flag changes GLOBAL driver policy.
        // Preserve per-volume user IDs, and leave global policy untouched.
        var flags = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(136));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(136), flags & ~1ul);
        return bytes;
    }

    public static (string Id, string Label, uint Incompat, uint ReadOnly, string FileSystem)? ParseSuperblock(byte[] data)
    {
        if (data.Length < 1024 || BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(56)) != 0xef53)
            return null;
        var label = Encoding.UTF8.GetString(data, 120, 16).TrimEnd('\0', ' ');
        uint compat = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(92));
        uint incompat = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(96));
        uint readOnly = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(100));
        string fileSystem = (incompat & ~0x1fu) != 0 || (readOnly & ~7u) != 0
            ? "EXT4" : (compat & 4) != 0 ? "EXT3" : "EXT2";
        return (Id(data.AsSpan(104, 16)), label, incompat, readOnly, fileSystem);
    }
}
