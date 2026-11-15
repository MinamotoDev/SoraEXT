using System.Collections.ObjectModel;

namespace SoraEXT.App;

public sealed class LogEntry(string time, string title, string detail)
{
    public string Time { get; } = time;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
}

public static class AppLog
{
    public static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SoraEXT");
    public static ObservableCollection<LogEntry> Entries { get; } = [];

    public static void Write(string title, string detail, bool persist = false)
    {
        var entry = new LogEntry(DateTime.Now.ToString("HH:mm:ss"), title, detail);
        // Persist management steps so a stalled native call can be diagnosed
        // even after the application has been restarted.
        if (persist)
        {
            try
            {
                var logs = Path.Combine(DirectoryPath, "Logs");
                Directory.CreateDirectory(logs);
                File.AppendAllText(Path.Combine(logs, $"operations-{DateTime.Today:yyyy-MM-dd}.log"),
                    $"{DateTime.Now:O} {title} {detail}{Environment.NewLine}");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        void Add()
        {
            Entries.Insert(0, entry);
            while (Entries.Count > 100) Entries.RemoveAt(Entries.Count - 1);
        }
        var dispatcher = App.MainWindow?.DispatcherQueue;
        if (dispatcher is null || dispatcher.HasThreadAccess) Add();
        else dispatcher.TryEnqueue(Add);
    }
}
