using SoraEXT.Core;
using System.ComponentModel;

namespace SoraEXT.App;

// XAML metadata generation expects mutable properties for init-only records.
// Expose a read-only presentation adapter and keep the driver snapshot immutable.
public sealed class VolumeRow : INotifyPropertyChanged
{
    internal Volume Model { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public VolumeRow(Volume model) => Model = model;
    internal void Update(Volume model)
    {
        if (Model == model) return;
        Model = model;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    public string Label => Model.Label;
    public string Subtitle => Model.Subtitle;
    public string DisplayLetter => Model.DisplayLetter;
    public string State => Model.State;
    public string FreeSpaceText => Model.FreeSpaceText;
    public double UsedPercent => Model.UsedPercent;
    public string AccessibleName => $"{Label}，{DisplayLetter}，{State}，{Model.Capacity}";
    public override string ToString() => AccessibleName;
}
