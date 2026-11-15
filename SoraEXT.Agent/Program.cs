using Microsoft.Win32;
using SoraEXT.Core;

// Headless boot task. UI lifetime is independent of mounted filesystems.
// The installation script deploys this executable into protected Program Files.
using var mutex = new Mutex(true, @"Global\SoraEXTMount", out bool owner);
if (!owner || !DriverService.IsAdministrator) return;
var logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SoraEXT", "Logs");
void Log(string message)
{
    try
    {
        Directory.CreateDirectory(logDirectory);
        File.AppendAllText(Path.Combine(logDirectory, $"mount-{DateTime.Today:yyyy-MM-dd}.log"),
            $"{DateTime.Now:O} {message}{Environment.NewLine}");
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
}

var driver = new DriverService();
var completed = new HashSet<string>();
for (int attempt = 0; attempt < 12; attempt++)
{
    try
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Ext2Fsd\Volumes");
        var targets = key?.GetValueNames()
            .Select(id => (Id: id, Options: key.GetValue(id)?.ToString() ?? ""))
            .Where(v => VolumeOptions.MountLetter(v.Options) is not null).ToList() ?? [];
        if (targets.Count == 0 || targets.All(v => completed.Contains(v.Id))) return;
        var scan = await driver.ScanAsync();
        if (scan.DriverRunning)
        {
            foreach (var target in targets.Where(v => !completed.Contains(v.Id)))
            {
                var volume = scan.Volumes.SingleOrDefault(v => v.Id == target.Id);
                if (volume is null) continue;
                var letter = VolumeOptions.MountLetter(target.Options)!.Value;
                if (volume.Mounted)
                {
                    Log($"{target.Id} 已挂载至 {volume.Letter}:");
                    completed.Add(target.Id);
                    continue;
                }
                try
                {
                    bool readOnly = target.Options.Split(';').Contains("Readonly", StringComparer.OrdinalIgnoreCase);
                    await driver.ApplyAsync(volume, letter, readOnly, true);
                    completed.Add(target.Id);
                    Log($"{target.Id} 挂载至 {letter}:");
                }
                catch (Exception ex) { Log($"{target.Id} {ex.Message}"); }
            }
        }
    }
    catch (Exception ex) { Log(ex.Message); }
    if (attempt < 11) await Task.Delay(TimeSpan.FromSeconds(10));
}
Log("启动扫描结束。");
