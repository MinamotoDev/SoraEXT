using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;

namespace SoraEXT.Setup;

public sealed partial class MainWindow : Window
{
    private readonly bool uninstall = Environment.GetCommandLineArgs().Contains("--uninstall");
    private bool busy;
    private bool finished;
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
    public MainWindow()
    {
        InitializeComponent();
        Title = SetupStrings.Get("Title");
        Agreement.Content = SetupStrings.Get("Agreement");
        LicenseLink.Content = SetupStrings.Get("License");
        PoweredBy.Text = SetupStrings.Get("PoweredBy");
        Cancel.Content = SetupStrings.Get("Cancel");
        Action.Content = SetupStrings.Get("Install");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "SoraEXT.ico"));
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        else
            ((Microsoft.UI.Xaml.Controls.Grid)Content).Background =
                (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
        double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96d;
        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(560 * scale), (int)(460 * scale)));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }
        AppWindow.Closing += (_, e) => e.Cancel = busy;
        if (uninstall)
        {
            Status.Text = SetupStrings.Get("RemoveDescription");
            Agreement.Visibility = LicenseLink.Visibility = Visibility.Collapsed;
            Action.Content = SetupStrings.Get("Uninstall");
            Action.IsEnabled = true;
        }
    }
    private void AgreementChanged(object sender, RoutedEventArgs e)
    {
        if (Action is not null) Action.IsEnabled = Agreement.IsChecked == true;
    }
    private void CancelClick(object sender, RoutedEventArgs e) => Close();
    private async void ShowLicense(object sender, RoutedEventArgs e)
    {
        var text = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Payload", "LICENSE.txt"));
        await new ContentDialog
        {
            XamlRoot = Content.XamlRoot, Title = SetupStrings.Get("License"), CloseButtonText = SetupStrings.Get("Close"),
            Content = new ScrollViewer { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, MaxHeight = 360 }
        }.ShowAsync();
    }
    private async void ActionClick(object sender, RoutedEventArgs e)
    {
        if (finished)
        {
            if (uninstall) { Close(); return; }
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "MinamotoDev", "SoraEXT");
                Process.Start(new ProcessStartInfo(Path.Combine(directory, "SoraEXT.exe"))
                {
                    UseShellExecute = true, WorkingDirectory = directory
                });
                Close();
            }
            catch (Exception)
            {
                Status.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                Status.Text = SetupStrings.Get("LaunchFailed");
            }
            return;
        }
        busy = true;
        Action.IsEnabled = Cancel.IsEnabled = Agreement.IsEnabled = false;
        Progress.Visibility = Visibility.Visible;
        Status.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        Status.Text = SetupStrings.Get(uninstall ? "Uninstalling" : "Installing");
        try
        {
            var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"))
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (string arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                Path.Combine(AppContext.BaseDirectory, "Payload", "Deploy.ps1"), "-Mode", uninstall ? "Uninstall" : "Install",
                "-Bootstrap", Environment.GetEnvironmentVariable("SORAEXT_SETUP_EXE") ?? "" })
                psi.ArgumentList.Add(arg);
            using var process = Process.Start(psi) ?? throw new IOException("无法启动安装进程。");
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var stdout = await output;
            var stderr = await errors;
            if (process.ExitCode != 0)
                throw new IOException(process.ExitCode == 20 ? "RestartRequired" : "Failed");
            finished = true;
            Status.Text = SetupStrings.Get(uninstall ? "Removed" : "Installed");
            Cancel.Content = SetupStrings.Get("Done");
            Action.Content = SetupStrings.Get("Launch");
            if (uninstall) Action.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Status.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            Status.Text = SetupStrings.Get(ex.Message == "RestartRequired" ? "RestartRequired" : "Failed");
            Action.Content = SetupStrings.Get("Retry");
        }
        finally
        {
            busy = false;
            Progress.Visibility = Visibility.Collapsed;
            Action.IsEnabled = Cancel.IsEnabled = true;
        }
    }
}
