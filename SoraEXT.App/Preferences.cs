using System.Text.Json;

namespace SoraEXT.App;

public sealed class Preferences
{
    public string Theme { get; set; } = "Default";
    public string Language { get; set; } = "system";
    public bool Backdrop { get; set; } = true;
    private static string FilePath => Path.Combine(AppLog.DirectoryPath, "settings.json");
    public static Preferences Load()
    {
        try
        {
            var source = File.Exists(FilePath) ? FilePath : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "app.minamotodev.soraext", "settings.json");
            var preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(source)) ?? new();
            if (source != FilePath) preferences.Save();
            return preferences;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(AppLog.DirectoryPath);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, FilePath, true);
    }
}
