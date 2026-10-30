using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nova.Core;

/// <summary>
/// Base class for all document models.
/// </summary>
public abstract partial class DocumentModel : ObservableObject, IDisposable
{
    private string _filePath = string.Empty;
    private bool _isDirty;
    private bool _isReadOnly;
    private string _title = "Untitled";
    private DocumentState _state = DocumentState.New;

    /// <summary>Full path to the document file.</summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    /// <summary>Whether the document has unsaved changes.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    /// <summary>Whether the document is read-only.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>Display title for the document.</summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>Current document state.</summary>
    public DocumentState State
    {
        get => _state;
        protected set => SetProperty(ref _state, value);
    }

    /// <summary>Unique identifier for this document instance.</summary>
    public Guid InstanceId { get; } = Guid.NewGuid();

    /// <summary>When the document was created.</summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>When the document was last modified.</summary>
    public DateTime LastModifiedTime { get; protected set; } = DateTime.UtcNow;

    /// <summary>Event raised when the document is saved.</summary>
    public event EventHandler<DocumentSavedEventArgs>? Saved;

    /// <summary>Event raised when the document state changes.</summary>
    public event EventHandler<DocumentStateChangedEventArgs>? StateChanged;

    /// <summary>Save the document to its current path.</summary>
    public abstract Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Save the document to a new path.</summary>
    public abstract Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Close the document.</summary>
    public virtual Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected void OnSaved(bool success, string? errorMessage = null)
    {
        if (success)
        {
            IsDirty = false;
            LastModifiedTime = DateTime.UtcNow;
            State = DocumentState.Saved;
        }
        Saved?.Invoke(this, new DocumentSavedEventArgs(success, errorMessage));
    }

    protected void OnStateChanged(DocumentState oldState, DocumentState newState)
    {
        StateChanged?.Invoke(this, new DocumentStateChangedEventArgs(oldState, newState));
    }

    public virtual void Dispose()
    {
        // Override in derived classes
    }
}

/// <summary>
/// Document state enumeration.
/// </summary>
public enum DocumentState
{
    New,
    Loading,
    Loaded,
    Editing,
    Saving,
    Saved,
    Error,
    Closed
}

/// <summary>
/// Event arguments for document saved.
/// </summary>
public sealed class DocumentSavedEventArgs : EventArgs
{
    public bool Success { get; }
    public string? ErrorMessage { get; }

    public DocumentSavedEventArgs(bool success, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Event arguments for document state changed.
/// </summary>
public sealed class DocumentStateChangedEventArgs : EventArgs
{
    public DocumentState OldState { get; }
    public DocumentState NewState { get; }

    public DocumentStateChangedEventArgs(DocumentState oldState, DocumentState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// Interface for document factories.
/// </summary>
public interface IDocumentFactory
{
    string AppId { get; }
    string[] SupportedExtensions { get; }
    Task<DocumentModel> CreateNewAsync(CancellationToken cancellationToken = default);
    Task<DocumentModel> OpenAsync(string filePath, CancellationToken cancellationToken = default);
    bool CanOpen(string filePath);
}