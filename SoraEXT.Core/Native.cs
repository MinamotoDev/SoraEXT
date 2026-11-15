using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SoraEXT.Core;

internal static class Native
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeFileHandle CreateFile(string path, uint access, uint share,
        nint security, uint disposition, uint flags, nint template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(SafeFileHandle handle, uint code,
        byte[]? input, uint inputSize, byte[]? output, uint outputSize, out uint returned, nint overlapped);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint QueryDosDevice(string name, StringBuilder target, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeInformation(string root, StringBuilder? label, uint labelSize,
        out uint serial, out uint maxComponent, out uint flags, StringBuilder? filesystem, uint filesystemSize);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlushFileBuffers(SafeFileHandle handle);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern void SHChangeNotify(uint eventId, uint flags, string path, nint extra);

    internal static SafeFileHandle Open(string path, bool write = false, bool queryOnly = false)
    {
        var h = CreateFile(path, queryOnly ? 0 : write ? 0xc0000000u : 0x80000000u, 3, 0, 3, 0, 0);
        if (h.IsInvalid) { int error = Marshal.GetLastWin32Error(); h.Dispose(); throw new Win32Exception(error, L.Get("打开 {0} 失败", path)); }
        return h;
    }

    internal static byte[] Ioctl(SafeFileHandle h, uint code, byte[]? buffer = null)
    {
        var output = new byte[buffer?.Length ?? 0];
        if (!DeviceIoControl(h, code, buffer, (uint)(buffer?.Length ?? 0),
                output, (uint)output.Length, out _, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return output;
    }

    internal static string Target(char letter)
    {
        var buffer = new StringBuilder(1024);
        return QueryDosDevice($"{letter}:", buffer, buffer.Capacity) > 0 ? buffer.ToString() : "";
    }

    internal static bool IsVolumeReadOnly(char letter)
    {
        if (!GetVolumeInformation($"{letter}:\\", null, 0, out _, out _, out var flags, null, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return (flags & 0x80000) != 0; // FILE_READ_ONLY_VOLUME
    }
}
