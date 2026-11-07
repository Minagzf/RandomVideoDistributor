
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using RandomVideoDistributor.Models;

namespace RandomVideoDistributor.Services;

public class VideoExtensionSettingsService
{
    private readonly string _settingsPath; private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };


    public VideoExtensionSettingsService()
    {
        _settingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "video-extensions.json");

        MigrateOldSettings();
    }

    private void MigrateOldSettings()
    {
        if (File.Exists(_settingsPath))
            return;

        var appDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        string oldSettingsPath = Path.Combine(
            appDataPath,
            "RandomVideoDistributor",
            "video-extensions.json");

        if (File.Exists(oldSettingsPath))
        {
            File.Copy(
                oldSettingsPath,
                _settingsPath);
        }
    }

    public VideoExtensionSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaultSettings = new VideoExtensionSettings();
            Save(defaultSettings);
            return defaultSettings;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);

            var settings =
                JsonSerializer.Deserialize<VideoExtensionSettings>(json);

            if (settings?.Extensions is null)
                return new VideoExtensionSettings();
            settings.Extensions =
            [
                .. settings.Extensions
                .Where(IsValidExtension)
                .Select(NormalizeExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
            ];

            return settings;
        }
        catch (JsonException)
        {
            return new VideoExtensionSettings();
        }
    }

    public void Save(VideoExtensionSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.Trim().ToLowerInvariant().TrimStart('.');
    }

    private static bool IsValidExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var normalized = NormalizeExtension(extension);

        return normalized.Length > 0
            && normalized.All(c =>
                char.IsAsciiLetterOrDigit(c) || c == '_');
    }
}
