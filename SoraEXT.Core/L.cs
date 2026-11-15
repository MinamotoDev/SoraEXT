using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SoraEXT.Core;

// Shared application-owned strings. Merged array order: zh-Hant, ja, en, ko.
// Simplified Chinese is the source key; technical identifiers and user labels
// are never translated.
public static class L
{
    private static readonly Dictionary<string, string[]> Strings = Load();
    public static string LanguageTag { get; private set; } = ResolveLanguage(CultureInfo.CurrentUICulture.Name);
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(LanguageTag);

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    private static Dictionary<string, string[]> Load()
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream("SoraEXT.Core.Strings.json")
            ?? throw new InvalidOperationException("Missing localization resources.");
        var strings = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)!;
        using var koreanStream = typeof(L).Assembly.GetManifestResourceStream("SoraEXT.Core.Strings.ko.json")
            ?? throw new InvalidOperationException("Missing Korean localization resources.");
        var korean = JsonSerializer.Deserialize<Dictionary<string, string>>(koreanStream)!;
        if (korean.Count != strings.Count || strings.Keys.Any(key => !korean.ContainsKey(key)))
            throw new InvalidOperationException("Korean localization keys do not match the base catalog.");
        return strings.ToDictionary(pair => pair.Key, pair => pair.Value.Append(korean[pair.Key]).ToArray());
    }

    public static string ResolveLanguage(string? language)
    {
        string tag = language?.ToLowerInvariant() ?? "";
        if (tag.StartsWith("zh"))
            return tag.Contains("hant") || tag is "zh-tw" or "zh-hk" or "zh-mo" ? "zh-Hant" : "zh-Hans";
        if (tag == "ko" || tag.StartsWith("ko-")) return "ko";
        return tag.StartsWith("ja") ? "ja" : "en";
    }

    public static void Configure(string? preference)
    {
        string language = string.IsNullOrWhiteSpace(preference) || preference == "system"
            ? CultureInfo.GetCultureInfo(GetUserDefaultUILanguage()).Name : preference;
        LanguageTag = ResolveLanguage(language);
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        // Keep the user's regional number/date formatting independently of UI language.
    }

    public static string Get(string key, params object?[] args)
    {
        int index = LanguageTag switch { "zh-Hant" => 0, "ja" => 1, "en" => 2, "ko" => 3, _ => -1 };
        string text = index >= 0 && Strings.TryGetValue(key, out var values) ? values[index] : key;
        return args.Length > 0 ? string.Format(CultureInfo.CurrentCulture, text, args) : text;
    }

    internal static IReadOnlyDictionary<string, string[]> Resources => Strings;
}
