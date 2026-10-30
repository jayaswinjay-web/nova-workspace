using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Nova.Core;

/// <summary>
/// Service for autosaving documents.
/// </summary>
public interface IAutosaveService
{
    /// <summary>Register a document for autosave.</summary>
    void RegisterDocument(string documentId, string filePath, Func<CancellationToken, Task<byte[]>> saveCallback);

    /// <summary>Unregister a document from autosave.</summary>
    void UnregisterDocument(string documentId);

    /// <summary>Force save a specific document.</summary>
    Task ForceSaveAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Force save all registered documents.</summary>
    Task ForceSaveAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Event raised when autosave occurs.</summary>
    event EventHandler<AutosaveEventArgs>? AutosaveCompleted;
}

/// <summary>
/// Event arguments for autosave completion.
/// </summary>
public sealed class AutosaveEventArgs : EventArgs
{
    public string DocumentId { get; }
    public string FilePath { get; }
    public bool Success { get; }
    public Exception? Error { get; }

    public AutosaveEventArgs(string documentId, string filePath, bool success, Exception? error = null)
    {
        DocumentId = documentId;
        FilePath = filePath;
        Success = success;
        Error = error;
    }
}

/// <summary>
/// Implementation of autosave service.
/// </summary>
public sealed class AutosaveService : IAutosaveService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AutosaveService> _logger;
    private readonly Dictionary<string, DocumentRegistration> _documents = new();
    private readonly Timer _timer;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly string _autoRecoverPath;
    private bool _disposed;

    public event EventHandler<AutosaveEventArgs>? AutosaveCompleted;

    public AutosaveService(ISettingsService settingsService, ILogger<AutosaveService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        _autoRecoverPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice",
            "AutoRecover");
        Directory.CreateDirectory(_autoRecoverPath);

        var interval = TimeSpan.FromSeconds(_settingsService.Current.AutosaveIntervalSeconds);
        _timer = new Timer(OnTimerElapsed, null, interval, interval);

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingName == "AutosaveIntervalSeconds" && e.NewValue is int seconds)
        {
            var interval = TimeSpan.FromSeconds(seconds);
            _timer.Change(interval, interval);
        }
    }

    private async void OnTimerElapsed(object? state)
    {
        await SaveAllAsync(CancellationToken.None);
    }

    public void RegisterDocument(string documentId, string filePath, Func<CancellationToken, Task<byte[]>> saveCallback)
    {
        var recoveryPath = Path.Combine(_autoRecoverPath, $"{documentId}.autosave");
        _documents[documentId] = new DocumentRegistration(documentId, filePath, recoveryPath, saveCallback);
        _logger.LogDebug("Registered document for autosave: {DocumentId}", documentId);
    }

    public void UnregisterDocument(string documentId)
    {
        if (_documents.TryGetValue(documentId, out var reg))
        {
            // Clean up recovery file on clean close
            try
            {
                if (File.Exists(reg.RecoveryPath))
                    File.Delete(reg.RecoveryPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete recovery file for {DocumentId}", documentId);
            }
            _documents.Remove(documentId);
            _logger.LogDebug("Unregistered document from autosave: {DocumentId}", documentId);
        }
    }

    public async Task ForceSaveAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(documentId, out var reg))
        {
            await SaveDocumentAsync(reg, cancellationToken);
        }
    }

    public async Task ForceSaveAllAsync(CancellationToken cancellationToken = default)
    {
        await SaveAllAsync(cancellationToken);
    }

    private async Task SaveAllAsync(CancellationToken cancellationToken)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            var tasks = _documents.Values.Select(reg => SaveDocumentAsync(reg, cancellationToken));
            await Task.WhenAll(tasks);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task SaveDocumentAsync(DocumentRegistration reg, CancellationToken cancellationToken)
    {
        try
        {
            var data = await reg.SaveCallback(cancellationToken);
            var tempPath = reg.RecoveryPath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, data, cancellationToken);
            File.Move(tempPath, reg.RecoveryPath, true);

            AutosaveCompleted?.Invoke(this, new AutosaveEventArgs(reg.DocumentId, reg.FilePath, true));
            _logger.LogDebug("Autosaved document: {DocumentId}", reg.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autosave failed for document: {DocumentId}", reg.DocumentId);
            AutosaveCompleted?.Invoke(this, new AutosaveEventArgs(reg.DocumentId, reg.FilePath, false, ex));
        }
    }

    /// <summary>
    /// Get all available recovery files.
    /// </summary>
    public IEnumerable<RecoveryInfo> GetRecoveryFiles()
    {
        foreach (var file in Directory.GetFiles(_autoRecoverPath, "*.autosave"))
        {
            try
            {
                var info = new FileInfo(file);
                var docId = Path.GetFileNameWithoutExtension(file);
                yield return new RecoveryInfo(docId, file, info.LastWriteTime, info.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read recovery file info: {File}", file);
            }
        }
    }

    /// <summary>
    /// Load recovery data for a document.
    /// </summary>
    public async Task<byte[]?> LoadRecoveryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_autoRecoverPath, $"{documentId}.autosave");
        if (!File.Exists(path)) return null;

        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recovery data for {DocumentId}", documentId);
            return null;
        }
    }

    /// <summary>
    /// Delete recovery file after successful recovery.
    /// </summary>
    public void DeleteRecovery(string documentId)
    {
        var path = Path.Combine(_autoRecoverPath, $"{documentId}.autosave");
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete recovery file: {DocumentId}", documentId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Dispose();
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _saveLock.Dispose();
    }

    private sealed class DocumentRegistration
    {
        public string DocumentId { get; }
        public string FilePath { get; }
        public string RecoveryPath { get; }
        public Func<CancellationToken, Task<byte[]>> SaveCallback { get; }

        public DocumentRegistration(string documentId, string filePath, string recoveryPath, Func<CancellationToken, Task<byte[]>> saveCallback)
        {
            DocumentId = documentId;
            FilePath = filePath;
            RecoveryPath = recoveryPath;
            SaveCallback = saveCallback;
        }
    }

    /// <summary>
    /// Information about a recovery file.
    /// </summary>
    public sealed class RecoveryInfo
    {
        public string DocumentId { get; }
        public string Path { get; }
        public DateTime LastModified { get; }
        public long Size { get; }

        public RecoveryInfo(string documentId, string path, DateTime lastModified, long size)
        {
            DocumentId = documentId;
            Path = path;
            LastModified = lastModified;
            Size = size;
        }
    }
}