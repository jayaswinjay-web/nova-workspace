using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Nova.Core;

/// <summary>
/// Represents a command that can be executed and undone.
/// </summary>
public interface ICommand : IDisposable
{
    /// <summary>Unique identifier for this command.</summary>
    Guid Id { get; }

    /// <summary>Human-readable description of the command.</summary>
    string Description { get; }

    /// <summary>Whether this command can be undone.</summary>
    bool CanUndo { get; }

    /// <summary>Whether this command can be redone.</summary>
    bool CanRedo { get; }

    /// <summary>Execute the command.</summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>Undo the command.</summary>
    Task UndoAsync(CancellationToken cancellationToken = default);

    /// <summary>Redo the command.</summary>
    Task RedoAsync(CancellationToken cancellationToken = default);

    /// <summary>Try to merge with another command of the same type.</summary>
    bool TryMerge(ICommand other);
}

/// <summary>
/// Service for managing undo/redo stacks.
/// </summary>
public interface IUndoRedoService
{
    /// <summary>Execute a command and add it to the undo stack.</summary>
    Task ExecuteAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>Undo the last command.</summary>
    Task<bool> UndoAsync(CancellationToken cancellationToken = default);

    /// <summary>Redo the last undone command.</summary>
    Task<bool> RedoAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether an undo is available.</summary>
    bool CanUndo { get; }

    /// <summary>Whether a redo is available.</summary>
    bool CanRedo { get; }

    /// <summary>Description of the next undo action.</summary>
    string? UndoDescription { get; }

    /// <summary>Description of the next redo action.</summary>
    string? RedoDescription { get; }

    /// <summary>Get the undo stack (for UI display).</summary>
    IReadOnlyList<ICommand> UndoStack { get; }

    /// <summary>Get the redo stack (for UI display).</summary>
    IReadOnlyList<ICommand> RedoStack { get; }

    /// <summary>Clear both stacks.</summary>
    void Clear();

    /// <summary>Event raised when the undo/redo state changes.</summary>
    event EventHandler? StateChanged;

    /// <summary>Begin a transaction group (multiple commands treated as one).</summary>
    IDisposable BeginTransaction(string description);

    /// <summary>Get the current transaction depth.</summary>
    int TransactionDepth { get; }
}

/// <summary>
/// Implementation of undo/redo service.
/// </summary>
public sealed class UndoRedoService : IUndoRedoService, IDisposable
{
    private readonly ILogger<UndoRedoService> _logger;
    private readonly List<ICommand> _undoStack = new();
    private readonly List<ICommand> _redoStack = new();
    private readonly Stack<Transaction> _transactions = new();
    private int _maxStackSize = 1000;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int TransactionDepth => _transactions.Count;

    public string? UndoDescription => CanUndo ? _undoStack[^1].Description : null;
    public string? RedoDescription => CanRedo ? _redoStack[^1].Description : null;

    public IReadOnlyList<ICommand> UndoStack => _undoStack.AsReadOnly();
    public IReadOnlyList<ICommand> RedoStack => _redoStack.AsReadOnly();

    public event EventHandler? StateChanged;

    public UndoRedoService(ILogger<UndoRedoService> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            await command.ExecuteAsync(cancellationToken);

            if (_transactions.Count > 0)
            {
                _transactions.Peek().Commands.Add(command);
            }
            else
            {
                _undoStack.Add(command);
                TrimStack(_undoStack);
                _redoStack.Clear();
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Executed command: {Description}", command.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Description}", command.Description);
            command.Dispose();
            throw;
        }
    }

    public async Task<bool> UndoAsync(CancellationToken cancellationToken = default)
    {
        if (!CanUndo) return false;

        var command = _undoStack[^1];
        if (!command.CanUndo) return false;

        try
        {
            await command.UndoAsync(cancellationToken);
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(command);
            TrimStack(_redoStack);
            StateChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Undid command: {Description}", command.Description);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo command: {Description}", command.Description);
            return false;
        }
    }

    public async Task<bool> RedoAsync(CancellationToken cancellationToken = default)
    {
        if (!CanRedo) return false;

        var command = _redoStack[^1];
        if (!command.CanRedo) return false;

        try
        {
            await command.RedoAsync(cancellationToken);
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(command);
            TrimStack(_undoStack);
            StateChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogDebug("Redid command: {Description}", command.Description);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to redo command: {Description}", command.Description);
            return false;
        }
    }

    public void Clear()
    {
        foreach (var cmd in _undoStack)
            cmd.Dispose();
        foreach (var cmd in _redoStack)
            cmd.Dispose();

        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public IDisposable BeginTransaction(string description)
    {
        var transaction = new Transaction(description);
        _transactions.Push(transaction);
        return transaction;
    }

    private void TrimStack(List<ICommand> stack)
    {
        while (stack.Count > _maxStackSize)
        {
            var removed = stack[0];
            stack.RemoveAt(0);
            removed.Dispose();
        }
    }

    private sealed class Transaction : IDisposable
    {
        public string Description { get; }
        public List<ICommand> Commands { get; } = new();
        private readonly UndoRedoService _service;

        public Transaction(string description)
        {
            Description = description;
        }

        public void Dispose()
        {
            if (_transactions.Count > 0 && _transactions.Peek() == this)
            {
                _transactions.Pop();
                if (Commands.Count > 0)
                {
                    var composite = new CompositeCommand(Description, Commands);
                    _service._undoStack.Add(composite);
                    _service.TrimStack(_service._undoStack);
                    _service._redoStack.Clear();
                    _service.StateChanged?.Invoke(_service, EventArgs.Empty);
                }
            }
        }
    }

    private sealed class CompositeCommand : ICommand
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Description { get; }
        public bool CanUndo => Commands.All(c => c.CanUndo);
        public bool CanRedo => Commands.All(c => c.CanRedo);
        private readonly List<ICommand> _commands;

        public CompositeCommand(string description, List<ICommand> commands)
        {
            Description = description;
            _commands = commands;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var cmd in _commands)
            {
                await cmd.ExecuteAsync(cancellationToken);
            }
        }

        public async Task UndoAsync(CancellationToken cancellationToken = default)
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                await _commands[i].UndoAsync(cancellationToken);
            }
        }

        public async Task RedoAsync(CancellationToken cancellationToken = default)
        {
            foreach (var cmd in _commands)
            {
                await cmd.RedoAsync(cancellationToken);
            }
        }

        public bool TryMerge(ICommand other)
        {
            return false; // Composite commands don't merge
        }

        public void Dispose()
        {
            foreach (var cmd in _commands)
                cmd.Dispose();
        }
    }

    public void Dispose()
    {
        Clear();
    }
}

/// <summary>
/// Base class for commands.
/// </summary>
public abstract class CommandBase : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract string Description { get; }
    public virtual bool CanUndo => true;
    public virtual bool CanRedo => true;

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
    public abstract Task UndoAsync(CancellationToken cancellationToken = default);
    public abstract Task RedoAsync(CancellationToken cancellationToken = default);

    public virtual bool TryMerge(ICommand other) => false;

    public virtual void Dispose() { }
}