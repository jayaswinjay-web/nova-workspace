using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Nova.Core;

/// <summary>
/// Application theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>Follow system theme.</summary>
    System = 0,
    /// <summary>Light theme.</summary>
    Light = 1,
    /// <summary>Dark theme.</summary>
    Dark = 2,
    /// <summary>High contrast theme.</summary>
    HighContrast = 3
}

/// <summary>
/// Application settings model.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Current theme.</summary>
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>Default font family for new documents.</summary>
    public string DefaultFontFamily { get; set; } = "Segoe UI Variable";

    /// <summary>Default font size for new documents.</summary>
    public double DefaultFontSize { get; set; } = 11;

    /// <summary>Autosave interval in seconds.</summary>
    public int AutosaveIntervalSeconds { get; set; } = 60;

    /// <summary>Default save folder.</summary>
    public string DefaultSaveFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    /// <summary>Application language/culture.</summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Whether animations are enabled.</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>Whether to show the ribbon in compact mode.</summary>
    public bool RibbonCompactMode { get; set; } = false;

    /// <summary>Recent files list (max 50).</summary>
    [JsonIgnore]
    public List<RecentFile> RecentFiles { get; set; } = new();

    /// <summary>Pinned files.</summary>
    [JsonIgnore]
    public List<RecentFile> PinnedFiles { get; set; } = new();
}

/// <summary>
/// Represents a recent file entry.
/// </summary>
public sealed class RecentFile
{
    /// <summary>Full path to the file.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Application that owns this file type.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Last accessed time.</summary>
    public DateTime LastAccessed { get; set; } = DateTime.Now;

    /// <summary>Whether this file is pinned.</summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>Thumbnail path (if generated).</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }
}

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>Current settings.</summary>
    AppSettings Current { get; }

    /// <summary>Event raised when settings change.</summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>Save settings to disk.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Reset settings to defaults.</summary>
    void ResetToDefaults();
}

/// <summary>
/// Event arguments for settings changes.
/// </summary>
public sealed class SettingsChangedEventArgs : EventArgs
{
    /// <summary>Name of the setting that changed.</summary>
    public string SettingName { get; }

    /// <summary>New value.</summary>
    public object? NewValue { get; }

    /// <summary>Old value.</summary>
    public object? OldValue { get; }

    public SettingsChangedEventArgs(string settingName, object? newValue, object? oldValue)
    {
        SettingName = settingName;
        NewValue = newValue;
        OldValue = oldValue;
    }
}

/// <summary>
/// Implementation of settings service using JSON file storage.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _settings = new();
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public AppSettings Current => _settings;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "Settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (loaded != null)
                {
                    _settings = loaded;
                    _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
                }
            }
            else
            {
                _logger.LogInformation("No settings file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _settings = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            var tempPath = _settingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, _settingsPath, true);
            _logger.LogDebug("Settings saved to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public void ResetToDefaults()
    {
        var oldSettings = _settings;
        _settings = new AppSettings();
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("All", _settings, oldSettings));
        _ = SaveAsync();
    }

    internal void NotifySettingChanged(string name, object? newValue, object? oldValue)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(name, newValue, oldValue));
    }
}