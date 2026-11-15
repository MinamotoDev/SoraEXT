using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint hwnd, string text, string caption, uint type);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existing, string? replacement, uint flags);

    [STAThread]
    private static int Main(string[] args)
    {
        string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "MinamotoDev", "SetupCache", Guid.NewGuid().ToString("N"));
        try
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                throw new PlatformNotSupportedException("需要 Windows 10 2004（19041）或更新版本，x64。");
            Directory.CreateDirectory(cache);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("setup.zip")!)
            using (var zip = new ZipArchive(stream))
                zip.ExtractToDirectory(cache);
            if (args.Contains("--verify"))
            {
                // Build-time payload verification; never launches deployment.
                if (!File.Exists(Path.Combine(cache, "Payload", "App", "SoraEXT.exe")) ||
                    !File.Exists(Path.Combine(cache, "SoraEXT.Setup.pri")) ||
                    !File.Exists(Path.Combine(cache, "Payload", "Ext2Fsd-0.71-setup.exe")))
                    throw new InvalidDataException("安装包内容不完整。");
                return 0;
            }
            var info = new ProcessStartInfo(Path.Combine(cache, "SoraEXT.Setup.exe")) { UseShellExecute = false };
            if (args.Contains("--uninstall")) info.ArgumentList.Add("--uninstall");
            info.Environment["SORAEXT_SETUP_EXE"] = Environment.ProcessPath!;
            using var process = Process.Start(info)!;
            process.WaitForExit();
            string installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MinamotoDev", "SoraEXT");
            if (args.Contains("--uninstall") && !File.Exists(Path.Combine(installed, "SoraEXT.exe")) &&
                string.Equals(Path.GetDirectoryName(Environment.ProcessPath), installed, StringComparison.OrdinalIgnoreCase))
            {
                MoveFileEx(Environment.ProcessPath!, null, 4);
                MoveFileEx(installed, null, 4);
            }
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            MessageBox(0, ex.Message, "SoraEXT 安装程序", 0x10);
            return 1;
        }
        finally
        {
            try { if (Directory.Exists(cache)) Directory.Delete(cache, true); } catch { }
        }
    }
}
