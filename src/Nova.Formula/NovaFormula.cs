using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Extension methods for configuring Nova.Formula services.
/// </summary>
public static class NovaFormulaExtensions
{
    /// <summary>
    /// Adds Nova.Formula services to the service collection.
    /// </summary>
    public static IServiceCollection AddNovaFormula(this IServiceCollection services)
    {
        services.AddSingleton<FormulaTokenizer>();
        services.AddSingleton<FormulaParser>(sp =>
            new FormulaParser(sp.GetRequiredService<ILogger<FormulaParser>>(), sp.GetRequiredService<FormulaTokenizer>()));
        services.AddSingleton<FunctionRegistry>();
        services.AddSingleton<FormulaEngine>(sp =>
            new FormulaEngine(sp.GetRequiredService<ILogger<FormulaEngine>>()));
        services.AddSingleton<DependencyGraph>();

        return services;
    }
}

/// <summary>
/// Spreadsheet calculation context implementation.
/// </summary>
public interface ISpreadsheetContext : IEvaluationContext
{
    /// <summary>
    /// Set a cell value.
    /// </summary>
    void SetCellValue(string sheetName, string cellRef, FormulaResult value);

    /// <summary>
    /// Set a cell formula.
    /// </summary>
    void SetCellFormula(string sheetName, string cellRef, string formula);

    /// <summary>
    /// Get cell formula.
    /// </summary>
    string? GetCellFormula(string sheetName, string cellRef);

    /// <summary>
    /// Recalculate all dirty cells.
    /// </summary>
    void Recalculate();

    /// <summary>
    /// Event raised when cell values change.
    /// </summary>
    event EventHandler<CellChangedEventArgs>? CellChanged;
}

/// <summary>
/// Event arguments for cell changes.
/// </summary>
public sealed class CellChangedEventArgs : EventArgs
{
    public string SheetName { get; }
    public string CellRef { get; }
    public FormulaResult OldValue { get; }
    public FormulaResult NewValue { get; }

    public CellChangedEventArgs(string sheetName, string cellRef, FormulaResult oldValue, FormulaResult newValue)
    {
        SheetName = sheetName;
        CellRef = cellRef;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Default spreadsheet context implementation.
/// </summary>
public sealed class SpreadsheetContext : ISpreadsheetContext
{
    private readonly FormulaEngine _engine;
    private readonly Dictionary<string, Dictionary<string, FormulaResult>> _values = new();
    private readonly Dictionary<string, Dictionary<string, string>> _formulas = new();
    private readonly Dictionary<string, FormulaResult> _namedRanges = new();
    private string _currentSheet = "Sheet1";
    private string _currentCell = "";

    public string CurrentSheet => _currentSheet;
    public string CurrentCell => _currentCell;

    public event EventHandler<CellChangedEventArgs>? CellChanged;

    public SpreadsheetContext(FormulaEngine engine)
    {
        _engine = engine;
    }

    public FormulaResult GetCellValue(string sheetName, string cellRef)
    {
        if (_values.TryGetValue(sheetName, out var sheet) && sheet.TryGetValue(cellRef, out var value))
            return value;
        return new FormulaResult(0.0); // Empty cells are 0
    }

    public IReadOnlyList<FormulaResult> GetRangeValues(string sheetName, string startRef, string endRef)
    {
        // Simplified - would parse range and return array of values
        var results = new List<FormulaResult>();
        if (_values.TryGetValue(sheetName, out var sheet))
        {
            // In a real implementation, parse startRef/endRef and iterate cells
            foreach (var kvp in sheet)
            {
                results.Add(kvp.Value);
            }
        }
        return results;
    }

    public FormulaResult GetNamedRange(string name)
    {
        return _namedRanges.TryGetValue(name, out var value) ? value : ErrorValue.Name;
    }

    public void SetCellValue(string sheetName, string cellRef, FormulaResult value)
    {
        var sheet = _values.GetOrAdd(sheetName, _ => new Dictionary<string, FormulaResult>());
        var oldValue = sheet.TryGetValue(cellRef, out var old) ? old : new FormulaResult(0.0);
        sheet[cellRef] = value;
        CellChanged?.Invoke(this, new CellChangedEventArgs(sheetName, cellRef, oldValue, value));
    }

    public void SetCellFormula(string sheetName, string cellRef, string formula)
    {
        var sheet = _formulas.GetOrAdd(sheetName, _ => new Dictionary<string, string>());
        sheet[cellRef] = formula;
        _engine.RegisterFormula($"{sheetName}!{cellRef}", formula);
    }

    public string? GetCellFormula(string sheetName, string cellRef)
    {
        return _formulas.TryGetValue(sheetName, out var sheet) && sheet.TryGetValue(cellRef, out var formula) ? formula : null;
    }

    public void Recalculate()
    {
        // Get all cells with formulas
        var formulaCells = new List<(string sheet, string cell, string formula)>();
        foreach (var (sheetName, sheet) in _formulas)
        {
            foreach (var (cellRef, formula) in sheet)
            {
                formulaCells.Add((sheetName, cellRef, formula));
            }
        }

        // Get recalc order
        var changedCells = formulaCells.Select(fc => $"{fc.sheet}!{fc.cell}").ToList();
        var order = _engine.GetRecalcOrder(changedCells);

        // Evaluate in order
        foreach (var cellAddr in order)
        {
            var parts = cellAddr.Split('!');
            if (parts.Length == 2)
            {
                var sheetName = parts[0];
                var cellRef = parts[1];
                if (_formulas.TryGetValue(sheetName, out var sheet) && sheet.TryGetValue(cellRef, out var formula))
                {
                    _currentSheet = sheetName;
                    _currentCell = cellAddr;
                    var result = _engine.Evaluate(formula, this);
                    _currentCell = "";
                    SetCellValue(sheetName, cellRef, result);
                }
            }
        }
    }

    public void SetNamedRange(string name, FormulaResult value)
    {
        _namedRanges[name] = value;
    }

    public void SetCurrentSheet(string sheetName)
    {
        _currentSheet = sheetName;
    }
}

/// <summary>
/// Extension for GetOrAdd on Dictionary.
/// </summary>
internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        value = factory(key);
        dict[key] = value;
        return value;
    }
}