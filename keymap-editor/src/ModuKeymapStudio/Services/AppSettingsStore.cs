using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModuKeymapStudio.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public sealed record AppSettings(string? ZmkAppPath = null, ThemePreference ThemePreference = ThemePreference.System);

public static class AppSettingsStore
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModuKeymapStudio");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? Deserialize(File.ReadAllText(SettingsPath))
                : new AppSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, Serialize(settings));
    }

    public static void SaveZmkAppPath(string zmkAppPath) =>
        Save(Load() with { ZmkAppPath = zmkAppPath });

    public static void SaveThemePreference(ThemePreference preference) =>
        Save(Load() with { ThemePreference = preference });

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, JsonOptions);

    public static AppSettings Deserialize(string json) =>
        JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
