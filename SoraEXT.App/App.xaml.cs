using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace SoraEXT.App;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    internal static Stopwatch StartupTimer { get; } = Stopwatch.StartNew();
    public App()
    {
        string? language = Preferences.Load().Language;
        var arguments = Environment.GetCommandLineArgs();
        int languageArgument = Array.IndexOf(arguments, "--language");
        // Preview-only language override: never modifies saved preferences.
        if (arguments.Contains("--demo") && languageArgument >= 0 && languageArgument + 1 < arguments.Length)
            language = arguments[languageArgument + 1];
        SoraEXT.Core.L.Configure(language);
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = SoraEXT.Core.L.LanguageTag;
        InitializeComponent();
        UnhandledException += (_, args) =>
        {
            AppLog.Write(SoraEXT.Core.L.Get("异常"), args.Exception.ToString());
            try
            {
                Directory.CreateDirectory(AppLog.DirectoryPath);
                File.AppendAllText(Path.Combine(AppLog.DirectoryPath, "crash.log"),
                    $"{DateTime.Now:O}\n{args.Exception}\n");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!SoraEXT.Core.DriverService.IsAdministrator)
        {
            Exit();
            return;
        }
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
