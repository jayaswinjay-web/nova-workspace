using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Nova.Core;

/// <summary>
/// Service for crash recovery.
/// </summary>
public interface ICrashRecoveryService
{
    /// <summary>Register the current session as active.</summary>
    void RegisterSession(string sessionId, IEnumerable<string> openDocuments);

    /// <summary>Unregister the session (clean shutdown).</summary>
    void UnregisterSession();

    /// <summary>Check if there's a previous session that crashed.</summary>
    bool HasPreviousCrash { get; }

    /// <summary>Get recovery information for the previous session.</summary>
    CrashRecoveryInfo? GetPreviousSessionRecovery();

    /// <summary>Mark recovery as completed.</summary>
    void MarkRecoveryCompleted();

    /// <summary>Event raised when recovery data is available.</summary>
    event EventHandler<CrashRecoveryEventArgs>? RecoveryAvailable;
}

/// <summary>
/// Information about a crashed session.
/// </summary>
public sealed class CrashRecoveryInfo
{
    public string SessionId { get; }
    public DateTime CrashTime { get; }
    public IReadOnlyList<string> OpenDocuments { get; }
    public IReadOnlyList<DocumentRecoveryInfo> DocumentRecoveries { get; }

    public CrashRecoveryInfo(string sessionId, DateTime crashTime, IEnumerable<string> openDocuments, IEnumerable<DocumentRecoveryInfo> documentRecoveries)
    {
        SessionId = sessionId;
        CrashTime = crashTime;
        OpenDocuments = openDocuments.ToList();
        DocumentRecoveries = documentRecoveries.ToList();
    }
}

/// <summary>
/// Recovery information for a single document.
/// </summary>
public sealed class DocumentRecoveryInfo
{
    public string DocumentId { get; }
    public string OriginalPath { get; }
    public string RecoveryPath { get; }
    public DateTime LastAutosave { get; }
    public long Size { get; }

    public DocumentRecoveryInfo(string documentId, string originalPath, string recoveryPath, DateTime lastAutosave, long size)
    {
        DocumentId = documentId;
        OriginalPath = originalPath;
        RecoveryPath = recoveryPath;
        LastAutosave = lastAutosave;
        Size = size;
    }
}

/// <summary>
/// Event arguments for crash recovery.
/// </summary>
public sealed class CrashRecoveryEventArgs : EventArgs
{
    public CrashRecoveryInfo RecoveryInfo { get; }

    public CrashRecoveryEventArgs(CrashRecoveryInfo recoveryInfo)
    {
        RecoveryInfo = recoveryInfo;
    }
}

/// <summary>
/// Implementation of crash recovery service.
/// </summary>
public sealed class CrashRecoveryService : ICrashRecoveryService
{
    private readonly IAutosaveService _autosaveService;
    private readonly ILogger<CrashRecoveryService> _logger;
    private readonly string _sessionPath;
    private string? _currentSessionId;
    private bool _hasPreviousCrash;
    private CrashRecoveryInfo? _previousRecovery;

    public bool HasPreviousCrash => _hasPreviousCrash;

    public event EventHandler<CrashRecoveryEventArgs>? RecoveryAvailable;

    public CrashRecoveryService(IAutosaveService autosaveService, ILogger<CrashRecoveryService> logger)
    {
        _autosaveService = autosaveService;
        _logger = logger;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NovaOffice");
        Directory.CreateDirectory(appDataPath);
        _sessionPath = Path.Combine(appDataPath, "session.json");

        CheckForPreviousCrash();
    }

    private void CheckForPreviousCrash()
    {
        try
        {
            if (File.Exists(_sessionPath))
            {
                var json = File.ReadAllText(_sessionPath);
                var sessionData = JsonSerializer.Deserialize<SessionData>(json);
                if (sessionData != null && sessionData.OpenDocuments.Count > 0)
                {
                    _hasPreviousCrash = true;
                    var recoveries = new List<DocumentRecoveryInfo>();

                    foreach (var doc in sessionData.OpenDocuments)
                    {
                        var autoRecoverFiles = _autosaveService.GetRecoveryFiles()
                            .Where(r => r.DocumentId == doc.DocumentId)
                            .ToList();

                        if (autoRecoverFiles.Any())
                        {
                            var latest = autoRecoverFiles.OrderByDescending(f => f.LastModified).First();
                            recoveries.Add(new DocumentRecoveryInfo(
                                doc.DocumentId,
                                doc.OriginalPath,
                                latest.Path,
                                latest.LastModified,
                                latest.Size));
                        }
                    }

                    _previousRecovery = new CrashRecoveryInfo(
                        sessionData.SessionId,
                        sessionData.LastUpdate,
                        sessionData.OpenDocuments.Select(d => d.OriginalPath),
                        recoveries);

                    _logger.LogWarning("Previous crash detected: {SessionId} at {CrashTime}, {Count} documents",
                        sessionData.SessionId, sessionData.LastUpdate, sessionData.OpenDocuments.Count);

                    RecoveryAvailable?.Invoke(this, new CrashRecoveryEventArgs(_previousRecovery));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for previous crash");
        }
    }

    public void RegisterSession(string sessionId, IEnumerable<string> openDocuments)
    {
        _currentSessionId = sessionId;
        var sessionData = new SessionData
        {
            SessionId = sessionId,
            LastUpdate = DateTime.UtcNow,
            OpenDocuments = openDocuments.Select(p => new SessionDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                OriginalPath = p
            }).ToList()
        };
        SaveSession(sessionData);
    }

    public void UnregisterSession()
    {
        if (_currentSessionId != null)
        {
            try
            {
                if (File.Exists(_sessionPath))
                    File.Delete(_sessionPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete session file");
            }
            _currentSessionId = null;
        }
    }

    public CrashRecoveryInfo? GetPreviousSessionRecovery()
    {
        return _previousRecovery;
    }

    public void MarkRecoveryCompleted()
    {
        _hasPreviousCrash = false;
        _previousRecovery = null;
    }

    internal void UpdateSessionDocuments(IEnumerable<string> openDocuments)
    {
        if (_currentSessionId == null) return;

        try
        {
            var sessionData = new SessionData
            {
                SessionId = _currentSessionId,
                LastUpdate = DateTime.UtcNow,
                OpenDocuments = openDocuments.Select(p => new SessionDocument
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    OriginalPath = p
                }).ToList()
            };
            SaveSession(sessionData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update session");
        }
    }

    private void SaveSession(SessionData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _sessionPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _sessionPath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session");
        }
    }

    private sealed class SessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
        public List<SessionDocument> OpenDocuments { get; set; } = new();
    }

    private sealed class SessionDocument
    {
        public string DocumentId { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
    }
}