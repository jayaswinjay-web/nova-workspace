using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Nova.Core;

/// <summary>
/// Service for managing recent files across all apps.
/// </summary>
public interface IRecentFilesService
{
    /// <summary>Get recent files for a specific app.</summary>
    IReadOnlyList<RecentFile> GetRecentFiles(string appId, int maxCount = 20);

    /// <summary>Get all recent files across all apps.</summary>
    IReadOnlyList<RecentFile> GetAllRecentFiles(int maxCount = 50);

    /// <summary>Get pinned files for a specific app.</summary>
    IReadOnlyList<RecentFile> GetPinnedFiles(string appId);

    /// <summary>Add or update a recent file.</summary>
    Task AddOrUpdateAsync(RecentFile file, CancellationToken cancellationToken = default);

    /// <summary>Remove a recent file.</summary>
    Task RemoveAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Pin or unpin a file.</summary>
    Task SetPinnedAsync(string filePath, bool pinned, CancellationToken cancellationToken = default);

    /// <summary>Clear all recent files for an app.</summary>
    Task ClearAsync(string appId, CancellationToken cancellationToken = default);

    /// <summary>Event raised when recent files change.</summary>
    event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;
}

/// <summary>
/// Event arguments for recent files changes.
/// </summary>
public sealed class RecentFilesChangedEventArgs : EventArgs
{
    public string AppId { get; }
    public RecentFilesChangeType ChangeType { get; }
    public RecentFile? File { get; }

    public RecentFilesChangedEventArgs(string appId, RecentFilesChangeType changeType, RecentFile? file = null)
    {
        AppId = appId;
        ChangeType = changeType;
        File = file;
    }
}

/// <summary>
/// Type of recent files change.
/// </summary>
public enum RecentFilesChangeType
{
    Added,
    Updated,
    Removed,
    Pinned,
    Unpinned,
    Cleared
}

/// <summary>
/// Implementation of recent files service.
/// </summary>
public sealed class RecentFilesService : IRecentFilesService
{
    private readonly string _recentFilesPath;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<RecentFilesService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;

    public RecentFilesService(ISettingsService settingsService, ILogger<RecentFilesService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice");
        Directory.CreateDirectory(appDataPath);
        _recentFilesPath = Path.Combine(appDataPath, "RecentFiles.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Sync with settings service
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingName == "RecentFiles" || e.SettingName == "All")
        {
            RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs("All", RecentFilesChangeType.Updated));
        }
    }

    public IReadOnlyList<RecentFile> GetRecentFiles(string appId, int maxCount = 20)
    {
        return _settingsService.Current.RecentFiles
            .Where(f => f.AppId == appId && !f.IsPinned)
            .OrderByDescending(f => f.LastAccessed)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<RecentFile> GetAllRecentFiles(int maxCount = 50)
    {
        return _settingsService.Current.RecentFiles
            .Where(f => !f.IsPinned)
            .OrderByDescending(f => f.LastAccessed)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<RecentFile> GetPinnedFiles(string appId)
    {
        return _settingsService.Current.PinnedFiles
            .Where(f => f.AppId == appId)
            .OrderByDescending(f => f.LastAccessed)
            .ToList();
    }

    public async Task AddOrUpdateAsync(RecentFile file, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = _settingsService.Current;
            var existingIndex = settings.RecentFiles.FindIndex(f => f.Path == file.Path);
            var wasPinned = false;

            if (existingIndex >= 0)
            {
                wasPinned = settings.RecentFiles[existingIndex].IsPinned;
                settings.RecentFiles[existingIndex] = file with { IsPinned = wasPinned };
            }
            else
            {
                settings.RecentFiles.Insert(0, file);
            }

            // Keep only last 100 recent files
            if (settings.RecentFiles.Count > 100)
            {
                settings.RecentFiles.RemoveRange(100, settings.RecentFiles.Count - 100);
            }

            _settingsService.NotifySettingChanged("RecentFiles", settings.RecentFiles, null);
            await _settingsService.SaveAsync(cancellationToken);

            RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(
                file.AppId, existingIndex >= 0 ? RecentFilesChangeType.Updated : RecentFilesChangeType.Added, file));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = _settingsService.Current;
            var file = settings.RecentFiles.FirstOrDefault(f => f.Path == filePath);
            if (file != null)
            {
                settings.RecentFiles.Remove(file);
                _settingsService.NotifySettingChanged("RecentFiles", settings.RecentFiles, null);
                await _settingsService.SaveAsync(cancellationToken);
                RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(file.AppId, RecentFilesChangeType.Removed, file));
            }

            // Also check pinned
            var pinnedFile = settings.PinnedFiles.FirstOrDefault(f => f.Path == filePath);
            if (pinnedFile != null)
            {
                settings.PinnedFiles.Remove(pinnedFile);
                _settingsService.NotifySettingChanged("PinnedFiles", settings.PinnedFiles, null);
                await _settingsService.SaveAsync(cancellationToken);
                RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(pinnedFile.AppId, RecentFilesChangeType.Unpinned, pinnedFile));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetPinnedAsync(string filePath, bool pinned, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = _settingsService.Current;
            var recentFile = settings.RecentFiles.FirstOrDefault(f => f.Path == filePath);
            var pinnedFile = settings.PinnedFiles.FirstOrDefault(f => f.Path == filePath);

            if (pinned)
            {
                if (recentFile != null)
                {
                    settings.RecentFiles.Remove(recentFile);
                    var pinnedCopy = recentFile with { IsPinned = true };
                    settings.PinnedFiles.Insert(0, pinnedCopy);
                    _settingsService.NotifySettingChanged("RecentFiles", settings.RecentFiles, null);
                    _settingsService.NotifySettingChanged("PinnedFiles", settings.PinnedFiles, null);
                    await _settingsService.SaveAsync(cancellationToken);
                    RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(pinnedCopy.AppId, RecentFilesChangeType.Pinned, pinnedCopy));
                }
                else if (pinnedFile != null)
                {
                    // Already pinned, just update position
                    settings.PinnedFiles.Remove(pinnedFile);
                    settings.PinnedFiles.Insert(0, pinnedFile);
                    _settingsService.NotifySettingChanged("PinnedFiles", settings.PinnedFiles, null);
                    await _settingsService.SaveAsync(cancellationToken);
                }
            }
            else
            {
                if (pinnedFile != null)
                {
                    settings.PinnedFiles.Remove(pinnedFile);
                    var unpinnedCopy = pinnedFile with { IsPinned = false };
                    settings.RecentFiles.Insert(0, unpinnedCopy);
                    _settingsService.NotifySettingChanged("RecentFiles", settings.RecentFiles, null);
                    _settingsService.NotifySettingChanged("PinnedFiles", settings.PinnedFiles, null);
                    await _settingsService.SaveAsync(cancellationToken);
                    RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(unpinnedCopy.AppId, RecentFilesChangeType.Unpinned, unpinnedCopy));
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(string appId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = _settingsService.Current;
            var toRemove = settings.RecentFiles.Where(f => f.AppId == appId).ToList();
            foreach (var file in toRemove)
            {
                settings.RecentFiles.Remove(file);
            }
            _settingsService.NotifySettingChanged("RecentFiles", settings.RecentFiles, null);
            await _settingsService.SaveAsync(cancellationToken);
            RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs(appId, RecentFilesChangeType.Cleared));
        }
        finally
        {
            _lock.Release();
        }
    }
}