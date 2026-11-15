using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SoraEXT.Core;

namespace SoraEXT.App;

// Static XAML labels are resolved during window creation. Language selection
// takes effect on restart, including built-in WinUI control resources.
public static class Translate
{
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(Translate), new PropertyMetadata("", OnKeyChanged));
    public static string GetKey(DependencyObject item) => (string)item.GetValue(KeyProperty);
    public static void SetKey(DependencyObject item, string value) => item.SetValue(KeyProperty, value);
    private static void OnKeyChanged(DependencyObject item, DependencyPropertyChangedEventArgs e)
    {
        string value = L.Get((string)e.NewValue);
        if (item is TextBlock text) text.Text = value;
        else if (item is ContentControl control) control.Content = value;
    }

    public static readonly DependencyProperty NameProperty = DependencyProperty.RegisterAttached(
        "Name", typeof(string), typeof(Translate), new PropertyMetadata("", (item, e) =>
            AutomationProperties.SetName(item, L.Get((string)e.NewValue))));
    public static string GetName(DependencyObject item) => (string)item.GetValue(NameProperty);
    public static void SetName(DependencyObject item, string value) => item.SetValue(NameProperty, value);

    public static readonly DependencyProperty TipProperty = DependencyProperty.RegisterAttached(
        "Tip", typeof(string), typeof(Translate), new PropertyMetadata("", (item, e) =>
            ToolTipService.SetToolTip(item, L.Get((string)e.NewValue))));
    public static string GetTip(DependencyObject item) => (string)item.GetValue(TipProperty);
    public static void SetTip(DependencyObject item, string value) => item.SetValue(TipProperty, value);
}
