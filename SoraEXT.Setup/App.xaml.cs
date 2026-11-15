using Microsoft.UI.Xaml;

namespace SoraEXT.Setup;

public partial class App : Application
{
    private Window? window;
    public App()
    {
        SetupStrings.Configure();
        InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow();
        window.Activate();
    }
}
