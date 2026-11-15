using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SoraEXT.Core;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace SoraEXT.App;

public sealed partial class MainWindow : Window
{
    public ObservableCollection<VolumeRow> Volumes { get; } = [];
    public ObservableCollection<LogEntry> Activity => AppLog.Entries;
    private readonly DriverService _driver = new();
    private readonly Preferences _preferences = Preferences.Load();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly UISettings _uiSettings = new();
    private bool _initialized, _busy, _closed, _dialogOpen, _narrow, _driverRunning;
    private DateTime _lastScan = DateTime.MinValue;
    private Volume? _selected;
    private readonly bool _demo = Environment.GetCommandLineArgs().Contains("--demo");
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);

    public MainWindow()
    {
        InitializeComponent();
        Root.Language = L.LanguageTag;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        var scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96d;
        var workArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
            AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min((int)(1220 * scale), workArea.Width - 48);
        var height = Math.Min((int)(820 * scale), workArea.Height - 48);
        AppWindow.MoveAndResize(new RectInt32(workArea.X + (workArea.Width - width) / 2,
            workArea.Y + (workArea.Height - height) / 2, width, height));
        AppWindow.Title = "SoraEXT";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "SoraEXT.ico"));
        SessionLabel.Text = _demo ? L.Get("界面预览") : DriverService.IsAdministrator ? L.Get("管理员") : "";
        AdminButton.Visibility = DriverService.IsAdministrator || _demo ? Visibility.Collapsed : Visibility.Visible;
        ThemePicker.SelectedIndex = _preferences.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        LanguagePicker.SelectedItem = LanguagePicker.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == _preferences.Language) ?? LanguagePicker.Items[0];
        BackdropSwitch.OnContent = L.Get("开");
        BackdropSwitch.OffContent = L.Get("关");
        AboutVersion.Text = $"SoraEXT {typeof(App).Assembly.GetName().Version?.ToString(3)}" +
            (typeof(App).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
                .Cast<System.Reflection.AssemblyMetadataAttribute>().Any(a => a.Key == "ReleaseChannel" && a.Value == "release") ? "" : " Dev");
        BackdropSwitch.IsOn = _preferences.Backdrop;
        ApplyAppearance();
        ApplyMotionPreferences();
        _uiSettings.AnimationsEnabledChanged += OnAnimationsEnabledChanged;
        Root.ActualThemeChanged += (_, _) => UpdateCaptionColors();
        Activated += OnActivated;
        Closed += (_, _) =>
        {
            _closed = true;
            CompositionTarget.Rendering -= OnFirstFrame;
            _uiSettings.AnimationsEnabledChanged -= OnAnimationsEnabledChanged;
            _lifetime.Cancel();
        };
        Navigation.SelectedItem = VolumesNav;
        _initialized = true;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering += OnFirstFrame;
        await RefreshAsync();
    }
    private void OnFirstFrame(object? sender, object e)
    {
        CompositionTarget.Rendering -= OnFirstFrame;
        AppLog.Write(L.Get("界面就绪"), L.Get("首个渲染回调 · {0:N0} ms", App.StartupTimer.Elapsed.TotalMilliseconds));
        if (Navigation.SettingsItem is NavigationViewItem settingsItem) settingsItem.Content = L.Get("设置");
    }
    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialized && !_dialogOpen &&
            args.WindowActivationState != WindowActivationState.Deactivated &&
            DateTime.UtcNow - _lastScan > TimeSpan.FromSeconds(15))
            await RefreshAsync();
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void OnRefreshKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_busy || _closed) return;
        SetBusy(true, L.Get("正在读取分区"));
        var elapsed = Stopwatch.StartNew();
        try
        {
            ScanResult result = _demo ? DemoData() : await _driver.ScanAsync(_lifetime.Token);
            if (_closed) return;
            var selectedId = _selected?.Id;
            Reconcile(result.Volumes);
            VolumeList.SelectedItem = Volumes.FirstOrDefault(v => v.Model.Id == selectedId) ?? Volumes.FirstOrDefault();
            ShowDetails((VolumeList.SelectedItem as VolumeRow)?.Model);
            _driverRunning = result.DriverRunning;
            SummaryTitle.Text = Volumes.Count == 1 ? L.Get("共 1 个分区") : L.Get("共 {0} 个分区", Volumes.Count);
            MountCount.Text = L.Get("{0} 个已挂载", Volumes.Count(v => v.Model.Mounted));
            EmptyState.Visibility = Volumes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyDetail.Text = L.Get("连接磁盘后刷新");
            if (Volumes.Count == 0) ShowDetails(null);
            if (result.Errors.Count > 0)
            {
                ShowNotice(L.Get("扫描结果"), string.Join("\n", result.Errors), InfoBarSeverity.Warning);
                AppLog.Write(L.Get("扫描提示"), string.Join("\n", result.Errors));
            }
            else if (!result.DriverRunning && Volumes.Any(v => !v.Model.IsWindowsVolume))
                ShowNotice(L.Get("驱动状态：{0}", result.DriverStatus), L.Get("请在 Ext2 Volume Manager 中检查驱动服务。"), InfoBarSeverity.Warning);
            _lastScan = DateTime.UtcNow;
            StatusLine.Text = L.Get("更新于 {0:HH:mm:ss} · {1:N0} ms", DateTime.Now, elapsed.Elapsed.TotalMilliseconds);
            AppLog.Write(L.Get("刷新"), L.Get("{0} 个分区 · {1:N0} ms", Volumes.Count, elapsed.Elapsed.TotalMilliseconds));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_closed) ShowFailure(L.Get("读取失败"), ex);
        }
        finally { if (!_closed) SetBusy(false); }
    }

    // Preserve item containers and selection when data is unchanged.
    private void Reconcile(IReadOnlyList<Volume> incoming)
    {
        var ids = incoming.Select(v => v.Id).ToHashSet();
        for (int i = Volumes.Count - 1; i >= 0; i--)
            if (!ids.Contains(Volumes[i].Model.Id)) Volumes.RemoveAt(i);
        foreach (var volume in incoming)
        {
            int index = -1;
            for (int i = 0; i < Volumes.Count; i++)
                if (Volumes[i].Model.Id == volume.Id) { index = i; break; }
            if (index < 0) Volumes.Add(new(volume));
            else Volumes[index].Update(volume);
        }
        for (int i = 0; i < incoming.Count; i++)
        {
            int index = Volumes.IndexOf(Volumes.First(v => v.Model.Id == incoming[i].Id));
            if (index != i) Volumes.Move(index, i);
        }
    }

    private void OnVolumeSelected(object sender, SelectionChangedEventArgs e) =>
        ShowDetails((VolumeList.SelectedItem as VolumeRow)?.Model);

    private void ShowDetails(Volume? volume)
    {
        bool changed = _selected?.Id != volume?.Id;
        _selected = volume;
        DetailFields.Visibility = volume is null ? Visibility.Collapsed : Visibility.Visible;
        DetailTitle.Text = volume?.Label ?? L.Get("选择分区");
        if (volume is null) return;
        DetailLetter.Text = volume.DisplayLetter;
        DetailMode.Text = volume.State;
        DetailSize.Text = volume.Capacity + " · " + volume.FreeSpaceText;
        DetailAuto.Text = volume.AutoMountText;
        DetailFileSystem.Text = volume.FileSystem;
        DetailIdentityLabel.Text = volume.IsWindowsVolume ? L.Get("卷标识") : "UUID";
        DetailUuid.Text = volume.Id.StartsWith("mounted:") ? L.Get("管理员模式中读取") : volume.Uuid;
        DetailFeatures.Text = volume.IsWindowsVolume
            ? ""
            : volume.FeaturesKnown
            ? L.Get("ext 特性 · 0x{0:X8} / 0x{1:X8}", volume.IncompatFeatures, volume.ReadOnlyFeatures)
            : L.Get("ext 特性 · 管理员模式中读取");
        DetailFeatures.Visibility = volume.IsWindowsVolume ? Visibility.Collapsed : Visibility.Visible;
        OpenButton.IsEnabled = volume.Mounted;
        ManagementActions.Visibility = volume.FileSystem == "NTFS" || volume.IsProtected
            ? Visibility.Collapsed : Visibility.Visible;
        UnmountButton.Content = L.Get(volume.IsWindowsVolume ? "移除盘符" : "卸载");
        UpdateManagementButtons();
        if (changed && _initialized) AnimateEntrance(DetailFields, 12);
    }

    private void UpdateManagementButtons()
    {
        bool available = !_busy && _selected is { IsProtected: false } volume &&
            volume.FileSystem != "NTFS" && (volume.IsWindowsVolume || _driverRunning);
        ConfigureButton.IsEnabled = available;
        UnmountButton.IsEnabled = available && _selected?.Mounted == true;
    }

    private void OnNavigationChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (VolumesPanel is null) return;
        string page = args.IsSettingsSelected ? "settings" :
            (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "volumes";
        VolumesPanel.Visibility = page == "volumes" ? Visibility.Visible : Visibility.Collapsed;
        ActivityPanel.Visibility = page == "activity" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = page == "settings" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = L.Get(page switch { "settings" => "设置", "activity" => "运行记录", _ => "分区" });
        PageSubtitle.Text = page switch { "settings" => L.Get("外观与行为"), "activity" => L.Get("本次会话"), _ => "" };
        PageSubtitle.Visibility = page == "volumes" ? Visibility.Collapsed : Visibility.Visible;
        if (_initialized) AnimateEntrance(page switch
        {
            "settings" => SettingsPanel, "activity" => ActivityPanel, _ => VolumesPanel
        });
    }

    private void OnAnimationsEnabledChanged(UISettings sender, object args) =>
        DispatcherQueue.TryEnqueue(() => { if (!_closed) ApplyMotionPreferences(); });

    private void ApplyMotionPreferences()
    {
        TransitionCollection ListTransitions() => _uiSettings.AnimationsEnabled
            ? new TransitionCollection { new AddDeleteThemeTransition(), new ReorderThemeTransition() }
            : new TransitionCollection();
        VolumeList.ItemContainerTransitions = ListTransitions();
        ActivityList.ItemContainerTransitions = ListTransitions();
        if (!_uiSettings.AnimationsEnabled)
            foreach (var element in new UIElement[] { VolumesPanel, ActivityPanel, SettingsPanel, DetailFields })
            {
                var visual = ElementCompositionPreview.GetElementVisual(element);
                visual.StopAnimation("Opacity");
                visual.StopAnimation("Translation");
                visual.Opacity = 1;
                element.Translation = Vector3.Zero;
            }
    }

    private void AnimateEntrance(UIElement element, float distance = 24)
    {
        // Fluent entrance on the compositor; no per-frame layout or UI-thread timer.
        if (!_uiSettings.AnimationsEnabled) return;
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var easing = visual.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1));
        ScalarKeyFrameAnimation fade = visual.Compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, easing);
        fade.Duration = TimeSpan.FromMilliseconds(200);
        var slide = visual.Compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0, new Vector3(0, distance, 0));
        slide.InsertKeyFrame(1, Vector3.Zero, easing);
        slide.Duration = TimeSpan.FromMilliseconds(250);
        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Translation", slide);
    }

    private void OnVolumesSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 760;
        if (narrow == _narrow) return;
        _narrow = narrow;
        DetailColumn.Width = narrow ? new GridLength(0) : new GridLength(360);
        ListRow.Height = narrow ? new GridLength(0.9, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
        DetailRow.Height = narrow ? new GridLength(1.1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(DetailCard, narrow ? 0 : 1);
        Grid.SetRow(DetailCard, narrow ? 1 : 0);
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        if (_selected is not { Mounted: true } volume) return;
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"{volume.Letter}:\\") { UseShellExecute = true }); }
        catch (Exception ex) { ShowFailure(L.Get("打开失败"), ex); }
    }

    private void OnElevate(object sender, RoutedEventArgs e)
    {
        try
        {
            var executable = Environment.ProcessPath ?? throw new IOException(L.Get("应用路径不可用。"));
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas" });
            Close();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        { StatusLine.Text = L.Get("已取消授权"); }
        catch (Exception ex) { ShowFailure(L.Get("启动失败"), ex); }
    }

    private bool EnsureManagement()
    {
        if (_demo)
        {
            ShowNotice(L.Get("界面预览"), L.Get("通过常规启动管理实际分区。"), InfoBarSeverity.Informational);
            return false;
        }
        if (DriverService.IsAdministrator) return true;
        ShowNotice(L.Get("管理员权限"), L.Get("请以管理员身份管理分区。"), InfoBarSeverity.Informational);
        return false;
    }

    private async void OnConfigure(object sender, RoutedEventArgs e)
    {
        if (_selected is not { IsProtected: false } volume || volume.FileSystem == "NTFS" || _busy || _dialogOpen) return;
        // Read-only preview of the dialog is useful in an ordinary session.
        var letters = DriverService.AvailableLetters(volume.Letter);
        var picker = new ComboBox { Header = L.Get("盘符"), ItemsSource = letters, HorizontalAlignment = HorizontalAlignment.Stretch };
        picker.SelectedItem = volume.Mounted ? volume.Letter : letters.FirstOrDefault();
        picker.IsEnabled = !volume.Mounted;
        var readOnly = new ToggleSwitch { Header = L.Get("只读"), IsOn = volume.ReadOnly, OnContent = L.Get("开"), OffContent = L.Get("关") };
        var auto = new ToggleSwitch { Header = L.Get("开机挂载"), IsOn = volume.AutoMount, OnContent = L.Get("开"), OffContent = L.Get("关") };
        var panel = new StackPanel { Spacing = 20, MinWidth = 300 };
        panel.Children.Add(picker);
        if (!volume.IsWindowsVolume)
        {
            panel.Children.Add(readOnly);
            panel.Children.Add(auto);
        }
        else
            panel.Children.Add(new TextBlock { Text = L.Get("盘符由 Windows 保存并恢复。"), TextWrapping = TextWrapping.Wrap });
        if (volume.Mounted)
            panel.Children.Add(new TextBlock { Text = L.Get(volume.IsWindowsVolume
                ? "更改盘符前请先移除现有盘符。" : "更改盘符前请先卸载。"), TextWrapping = TextWrapping.Wrap });
        if (!DriverService.IsAdministrator)
            panel.Children.Add(new TextBlock { Text = L.Get("保存设置需要管理员权限。"), TextWrapping = TextWrapping.Wrap });
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
            Title = volume.Label, Content = panel, PrimaryButtonText = L.Get("保存"),
            CloseButtonText = L.Get("取消"), DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = DriverService.IsAdministrator && !_demo && letters.Count > 0
        };
        _dialogOpen = true;
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            if (picker.SelectedItem is not string selected) return;
            SetBusy(true, L.Get("正在保存"));
            await _driver.ApplyAsync(volume, selected[0], readOnly.IsOn, auto.IsOn);
            AppLog.Write(L.Get("已保存"), $"{volume.Label} · {selected}: · {L.Get(readOnly.IsOn ? "只读" : "读写")}");
            Notice.IsOpen = false;
        }
        catch (Exception ex) { ShowFailure(L.Get("保存失败"), ex); }
        finally
        {
            _dialogOpen = false;
            SetBusy(false);
            await RefreshAsync();
        }
    }

    private async void OnUnmount(object sender, RoutedEventArgs e)
    {
        if (_selected is not { Mounted: true, IsProtected: false } volume || volume.FileSystem == "NTFS" ||
            _busy || _dialogOpen || !EnsureManagement()) return;
        _dialogOpen = true;
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
                Title = $"{L.Get(volume.IsWindowsVolume ? "移除盘符" : "卸载")} {volume.Letter}:",
                Content = L.Get(volume.IsWindowsVolume ? "移除盘符后，可重新挂载访问此分区。请先关闭使用此盘符的文件。"
                    : "请先关闭此分区中的文件。"),
                PrimaryButtonText = L.Get(volume.IsWindowsVolume ? "移除盘符" : "卸载"),
                CloseButtonText = L.Get("取消"), DefaultButton = ContentDialogButton.Close
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            SetBusy(true, L.Get("正在卸载"));
            string stage = L.Get("准备卸载");
            var progress = new Progress<string>(message =>
            {
                stage = message;
                AppLog.Write(L.Get("卸载步骤"), $"{volume.Letter}: · {message}", persist: true);
                if (!_closed) StatusLine.Text = $"{L.Get("正在卸载")} · {message}";
            });
            var operation = _driver.UnmountAsync(volume, progress);
            using var waitNotice = new CancellationTokenSource();
            var delay = Task.Delay(TimeSpan.FromSeconds(15), waitNotice.Token);
            if (await Task.WhenAny(operation, delay) != operation && !_closed)
            {
                ShowNotice(L.Get("卸载仍在进行"), L.Get("当前步骤：{0}。正在等待系统返回。", stage), InfoBarSeverity.Warning);
                AppLog.Write(L.Get("卸载等待"), stage, persist: true);
            }
            waitNotice.Cancel();
            await operation;
            AppLog.Write(L.Get("已卸载"), $"{volume.Label} · {volume.Letter}:", persist: true);
            Notice.IsOpen = false;
        }
        catch (Exception ex) { ShowFailure(L.Get("卸载失败"), ex); }
        finally { _dialogOpen = false; SetBusy(false); await RefreshAsync(); }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        BusyRing.IsActive = busy;
        BusyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        RefreshButton.IsEnabled = !busy;
        UpdateManagementButtons();
        AdminButton.IsEnabled = !busy;
        if (message is not null) StatusLine.Text = message;
    }

    private void ShowNotice(string title, string message, InfoBarSeverity severity)
    {
        Notice.Title = title;
        Notice.Message = message;
        Notice.Severity = severity;
        Notice.IsOpen = true;
    }

    private void ShowFailure(string title, Exception ex)
    {
        ShowNotice(title, ex.Message, InfoBarSeverity.Error);
        AppLog.Write(title, $"{ex.Message}\n0x{ex.HResult:X8}", persist: true);
        StatusLine.Text = title;
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        _preferences.Language = (LanguagePicker.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "system";
        SavePreferences();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        _preferences.Theme = (ThemePicker.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Default";
        ApplyAppearance();
        SavePreferences();
    }
    private void OnBackdropChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _preferences.Backdrop = BackdropSwitch.IsOn;
        ApplyAppearance();
        SavePreferences();
    }
    private void SavePreferences()
    {
        try { _preferences.Save(); }
        catch (Exception ex) { ShowFailure(L.Get("设置保存失败"), ex); }
    }
    private void ApplyAppearance()
    {
        Root.RequestedTheme = Enum.TryParse<ElementTheme>(_preferences.Theme, out var theme) ? theme : ElementTheme.Default;
        SystemBackdrop = _preferences.Backdrop ? new MicaBackdrop() : null;
        Root.Background = _preferences.Backdrop ? new SolidColorBrush(Colors.Transparent)
            : (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
        UpdateCaptionColors();
    }
    private void UpdateCaptionColors()
    {
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonForegroundColor = Root.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
    }

    private static ScanResult DemoData() => new(
    [
        new Volume
        {
            Id = "{88-44-E7-1D-4C-93-62-47-86-49-43-1D-E0-C5-C7-EC}", Label = "Spare",
            Device = @"\Device\Harddisk1\Partition1", DiskName = "ST500DM002-1BD142",
            DiskNumber = 1, PartitionNumber = 1, Size = 500105740288, FreeSpace = 491105435648,
            Letter = "F", AutoMount = true, FeaturesKnown = false
        },
        new Volume
        {
            Id = "{00-11-22-33-44-55-66-77-88-99-AA-BB-CC-DD-EE-FF}", Label = "Archive",
            Device = @"\Device\Harddisk4\Partition1", DiskName = "USB Storage",
            DiskNumber = 4, PartitionNumber = 1, Size = 1000204886016, ReadOnly = true
        }
    ], L.Get("运行中"), true, true, []);
}
