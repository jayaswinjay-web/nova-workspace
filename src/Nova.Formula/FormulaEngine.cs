using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Represents a parsed formula with its AST and dependencies.
/// </summary>
public sealed class ParsedFormula
{
    public string FormulaText { get; }
    public AstNode Ast { get; }
    public IReadOnlyList<CellReference> Dependencies { get; }
    public bool HasError { get; }
    public ErrorValue? ParseError { get; }

    public ParsedFormula(string formulaText, AstNode ast, IEnumerable<CellReference> dependencies)
    {
        FormulaText = formulaText;
        Ast = ast;
        Dependencies = dependencies.ToList();
        HasError = false;
    }

    public ParsedFormula(string formulaText, ErrorValue error)
    {
        FormulaText = formulaText;
        Ast = new LiteralNode(new Token(TokenType.Error, "", error, 0, 0), error);
        Dependencies = Array.Empty<CellReference>();
        HasError = true;
        ParseError = error;
    }
}

/// <summary>
/// Main formula engine for spreadsheet calculations.
/// </summary>
public sealed class FormulaEngine
{
    private readonly ILogger<FormulaEngine> _logger;
    private readonly FormulaTokenizer _tokenizer;
    private readonly FormulaParser _parser;
    private readonly FunctionRegistry _functions;
    private readonly FormulaEvaluator _evaluator;
    private readonly Dictionary<string, ParsedFormula> _parsedCache = new();
    private readonly DependencyGraph _dependencyGraph = new();

    public FormulaEngine(ILogger<FormulaEngine> logger)
    {
        _logger = logger;
        _tokenizer = new FormulaTokenizer(logger);
        _parser = new FormulaParser(logger, _tokenizer);
        _functions = new FunctionRegistry();
        _evaluator = new FormulaEvaluator(logger, new EmptyEvaluationContext(), _functions);
    }

    /// <summary>
    /// Parse a formula string.
    /// </summary>
    public ParsedFormula Parse(string formula)
    {
        if (_parsedCache.TryGetValue(formula, out var cached))
            return cached;

        try
        {
            var ast = _parser.Parse(formula);
            var dependencies = ExtractDependencies(ast);
            var parsed = new ParsedFormula(formula, ast, dependencies);
            _parsedCache[formula] = parsed;
            return parsed;
        }
        catch (FormulaParseException ex)
        {
            _logger.LogWarning(ex, "Parse error in formula: {Formula}", formula);
            var errorFormula = new ParsedFormula(formula, ErrorValue.Name);
            _parsedCache[formula] = errorFormula;
            return errorFormula;
        }
    }

    /// <summary>
    /// Evaluate a formula with the given context.
    /// </summary>
    public FormulaResult Evaluate(string formula, IEvaluationContext context)
    {
        var parsed = Parse(formula);
        if (parsed.HasError)
            return parsed.ParseError!.Value;

        var evaluator = new FormulaEvaluator(_logger, context, _functions);
        return evaluator.Evaluate(parsed.Ast);
    }

    /// <summary>
    /// Evaluate a parsed formula with the given context.
    /// </summary>
    public FormulaResult Evaluate(ParsedFormula parsed, IEvaluationContext context)
    {
        if (parsed.HasError)
            return parsed.ParseError!.Value;

        var evaluator = new FormulaEvaluator(_logger, context, _functions);
        return evaluator.Evaluate(parsed.Ast);
    }

    /// <summary>
    /// Get function signatures for autocomplete.
    /// </summary>
    public IEnumerable<FunctionSignature> GetFunctions() => _functions.GetSignatures();

    /// <summary>
    /// Get function signature by name.
    /// </summary>
    public bool TryGetFunction(string name, out FunctionSignature signature)
    {
        return _functions.TryGetSignature(name, out signature!);
    }

    /// <summary>
    /// Clear the parse cache.
    /// </summary>
    public void ClearCache()
    {
        _parsedCache.Clear();
    }

    /// <summary>
    /// Register a formula in the dependency graph.
    /// </summary>
    public void RegisterFormula(string cellAddress, string formula)
    {
        var parsed = Parse(formula);
        _dependencyGraph.AddNode(cellAddress, parsed.Dependencies.Select(d => d.Reference).ToList());
    }

    /// <summary>
    /// Unregister a formula from the dependency graph.
    /// </summary>
    public void UnregisterFormula(string cellAddress)
    {
        _dependencyGraph.RemoveNode(cellAddress);
    }

    /// <summary>
    /// Get recalculation order for changed cells.
    /// </summary>
    public IReadOnlyList<string> GetRecalcOrder(IEnumerable<string> changedCells)
    {
        return _dependencyGraph.GetTopologicalOrder(changedCells);
    }

    /// <summary>
    /// Check for circular references.
    /// </summary>
    public bool HasCircularReference(string cellAddress, out IReadOnlyList<string> cycle)
    {
        return _dependencyGraph.HasCycle(cellAddress, out cycle!);
    }

    private IEnumerable<CellReference> ExtractDependencies(AstNode node)
    {
        var refs = new List<CellReference>();
        ExtractDependenciesRecursive(node, refs);
        return refs.DistinctBy(r => r.Reference);
    }

    private void ExtractDependenciesRecursive(AstNode node, List<CellReference> refs)
    {
        switch (node)
        {
            case ReferenceNode refNode:
                refs.Add(refNode.Reference);
                break;
            case RangeNode rangeNode:
                refs.Add(new CellReference(rangeNode.Range.SheetName, rangeNode.Range.StartRef, false, false));
                if (rangeNode.Range.EndRef != null)
                    refs.Add(new CellReference(rangeNode.Range.SheetName, rangeNode.Range.EndRef, false, false));
                break;
            case BinaryOpNode binNode:
                ExtractDependenciesRecursive(binNode.Left, refs);
                ExtractDependenciesRecursive(binNode.Right, refs);
                break;
            case UnaryOpNode unNode:
                ExtractDependenciesRecursive(unNode.Operand, refs);
                break;
            case FunctionNode funcNode:
                foreach (var arg in funcNode.Arguments)
                    ExtractDependenciesRecursive(arg, refs);
                break;
            case ParenNode parenNode:
                ExtractDependenciesRecursive(parenNode.Expression, refs);
                break;
        }
    }
}

/// <summary>
/// Empty evaluation context for parsing without evaluation.
/// </summary>
internal sealed class EmptyEvaluationContext : IEvaluationContext
{
    public FormulaResult GetCellValue(string sheetName, string cellRef) => ErrorValue.Ref;
    public IReadOnlyList<FormulaResult> GetRangeValues(string sheetName, string startRef, string endRef) => Array.Empty<FormulaResult>();
    public FormulaResult GetNamedRange(string name) => ErrorValue.Name;
    public string CurrentSheet => "Sheet1";
    public string CurrentCell => "";
}

/// <summary>
/// Dependency graph for formula recalculation.
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _adjacency = new();
    private readonly Dictionary<string, HashSet<string>> _reverseAdjacency = new();

    /// <summary>
    /// Add a node with its dependencies.
    /// </summary>
    public void AddNode(string node, IEnumerable<string> dependencies)
    {
        _adjacency[node] = dependencies.ToHashSet();
        foreach (var dep in dependencies)
        {
            if (!_reverseAdjacency.ContainsKey(dep))
                _reverseAdjacency[dep] = new HashSet<string>();
            _reverseAdjacency[dep].Add(node);
        }
    }

    /// <summary>
    /// Remove a node and its edges.
    /// </summary>
    public void RemoveNode(string node)
    {
        if (_adjacency.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                _reverseAdjacency[dep].Remove(node);
            }
        }
        if (_reverseAdjacency.TryGetValue(node, out var revDeps))
        {
            foreach (var revDep in revDeps)
            {
                _adjacency[revDep].Remove(node);
            }
        }
        _adjacency.Remove(node);
        _reverseAdjacency.Remove(node);
    }

    /// <summary>
    /// Get topological order for recalculation starting from changed cells.
    /// </summary>
    public IReadOnlyList<string> GetTopologicalOrder(IEnumerable<string> changedCells)
    {
        var visited = new HashSet<string>();
        var temp = new HashSet<string>();
        var order = new List<string>();

        foreach (var cell in changedCells)
        {
            Visit(cell, visited, temp, order);
        }

        // Also include all dependents of changed cells
        var allAffected = new HashSet<string>(changedCells);
        foreach (var cell in changedCells)
        {
            CollectDependents(cell, allAffected);
        }

        // Sort all affected topologically
        visited.Clear();
        temp.Clear();
        order.Clear();
        foreach (var cell in allAffected)
        {
            if (!visited.Contains(cell))
                Visit(cell, visited, temp, order);
        }

        return order;
    }

    private void Visit(string node, HashSet<string> visited, HashSet<string> temp, List<string> order)
    {
        if (temp.Contains(node))
        {
            // Cycle detected - skip to avoid infinite loop
            return;
        }
        if (visited.Contains(node)) return;

        temp.Add(node);
        if (_adjacency.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                Visit(dep, visited, temp, order);
            }
        }
        temp.Remove(node);
        visited.Add(node);
        order.Add(node);
    }

    private void CollectDependents(string node, HashSet<string> collected)
    {
        if (_reverseAdjacency.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (collected.Add(dep))
                    CollectDependents(dep, collected);
            }
        }
    }

    /// <summary>
    /// Check if adding a dependency would create a cycle.
    /// </summary>
    public bool HasCycle(string newNode, out IReadOnlyList<string> cycle)
    {
        cycle = Array.Empty<string>();
        // Simplified cycle detection
        var visited = new HashSet<string>();
        var path = new List<string>();
        return DetectCycle(newNode, visited, path, out cycle);
    }

    private bool DetectCycle(string node, HashSet<string> visited, List<string> path, out IReadOnlyList<string> cycle)
    {
        cycle = Array.Empty<string>();
        if (path.Contains(node))
        {
            var idx = path.IndexOf(node);
            cycle = path.Skip(idx).Concat(new[] { node }).ToList();
            return true;
        }
        if (visited.Contains(node)) return false;

        visited.Add(node);
        path.Add(node);

        if (_adjacency.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (DetectCycle(dep, visited, path, out cycle))
                    return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }
}