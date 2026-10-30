using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Represents a formula evaluation result.
/// </summary>
public readonly struct FormulaResult
{
    public object? Value { get; }
    public ErrorValue? Error { get; }
    public bool IsError => Error.HasValue;

    public FormulaResult(object? value)
    {
        Value = value;
        Error = null;
    }

    public FormulaResult(ErrorValue error)
    {
        Value = null;
        Error = error;
    }

    public static implicit operator FormulaResult(double value) => new(value);
    public static implicit operator FormulaResult(string value) => new(value);
    public static implicit operator FormulaResult(bool value) => new(value);
    public static implicit operator FormulaResult(ErrorValue error) => new(error);

    public override string ToString()
    {
        if (IsError) return Error.Value.ToString();
        return Value?.ToString() ?? "NULL";
    }
}

/// <summary>
/// Error values for formulas.
/// </summary>
public readonly struct ErrorValue
{
    public ErrorType Type { get; }
    public string Message { get; }

    private ErrorValue(ErrorType type, string message)
    {
        Type = type;
        Message = message;
    }

    public static ErrorValue DivByZero => new(ErrorType.DivByZero, "#DIV/0!");
    public static ErrorValue Ref => new(ErrorType.Ref, "#REF!");
    public static ErrorValue Name => new(ErrorType.Name, "#NAME?");
    public static ErrorValue Value => new(ErrorType.Value, "#VALUE!");
    public static ErrorValue NA => new(ErrorType.NA, "#N/A");
    public static ErrorValue Num => new(ErrorType.Num, "#NUM!");
    public static ErrorValue Null => new(ErrorType.Null, "#NULL!");
    public static ErrorValue Circ => new(ErrorType.Circ, "#CIRC!");
    public static ErrorValue Spill => new(ErrorType.Spill, "#SPILL!");
    public static ErrorValue Calc => new(ErrorType.Calc, "#CALC!");

    public static ErrorValue Unknown(string name) => new(ErrorType.Name, $"#NAME? ({name})");

    public override string ToString() => Message;

    public static implicit operator string(ErrorValue e) => e.Message;
}

public enum ErrorType
{
    DivByZero,
    Ref,
    Name,
    Value,
    NA,
    Num,
    Null,
    Circ,
    Spill,
    Calc
}

/// <summary>
/// Evaluation context providing cell values and functions.
/// </summary>
public interface IEvaluationContext
{
    /// <summary>
    /// Get the value of a cell.
    /// </summary>
    FormulaResult GetCellValue(string sheetName, string cellRef);

    /// <summary>
    /// Get values of a range.
    /// </summary>
    IReadOnlyList<FormulaResult> GetRangeValues(string sheetName, string startRef, string endRef);

    /// <summary>
    /// Get a named range value.
    /// </summary>
    FormulaResult GetNamedRange(string name);

    /// <summary>
    /// Current sheet name.
    /// </summary>
    string CurrentSheet { get; }

    /// <summary>
    /// Current cell reference being evaluated (for circular detection).
    /// </summary>
    string CurrentCell { get; }
}

/// <summary>
/// Evaluates AST nodes to produce results.
/// </summary>
public sealed class FormulaEvaluator
{
    private readonly ILogger<FormulaEvaluator> _logger;
    private readonly IEvaluationContext _context;
    private readonly FunctionRegistry _functions;
    private readonly HashSet<string> _evaluationStack = new();

    public FormulaEvaluator(ILogger<FormulaEvaluator> logger, IEvaluationContext context, FunctionRegistry functions)
    {
        _logger = logger;
        _context = context;
        _functions = functions;
    }

    /// <summary>
    /// Evaluate an AST node.
    /// </summary>
    public FormulaResult Evaluate(AstNode node)
    {
        return node switch
        {
            LiteralNode n => EvaluateLiteral(n),
            ReferenceNode n => EvaluateReference(n),
            RangeNode n => EvaluateRange(n),
            NamedRangeNode n => EvaluateNamedRange(n),
            BinaryOpNode n => EvaluateBinaryOp(n),
            UnaryOpNode n => EvaluateUnaryOp(n),
            FunctionNode n => EvaluateFunction(n),
            ParenNode n => Evaluate(n.Expression),
            _ => ErrorValue.Value
        };
    }

    private FormulaResult EvaluateLiteral(LiteralNode node)
    {
        if (node.Value is ErrorValue err) return err;
        return new FormulaResult(node.Value);
    }

    private FormulaResult EvaluateReference(ReferenceNode node)
    {
        var refInfo = node.Reference;
        var sheet = refInfo.SheetName ?? _context.CurrentSheet;
        var cellRef = refInfo.Reference;

        // Check for circular reference
        var fullRef = $"{sheet}!{cellRef}";
        if (_evaluationStack.Contains(fullRef))
        {
            return ErrorValue.Circ;
        }

        _evaluationStack.Add(fullRef);
        try
        {
            return _context.GetCellValue(sheet, cellRef);
        }
        finally
        {
            _evaluationStack.Remove(fullRef);
        }
    }

    private FormulaResult EvaluateRange(RangeNode node)
    {
        var range = node.Range;
        var sheet = range.SheetName ?? _context.CurrentSheet;
        return new FormulaResult(_context.GetRangeValues(sheet, range.StartRef, range.EndRef ?? range.StartRef));
    }

    private FormulaResult EvaluateNamedRange(NamedRangeNode node)
    {
        return _context.GetNamedRange(node.Name);
    }

    private FormulaResult EvaluateBinaryOp(BinaryOpNode node)
    {
        var left = Evaluate(node.Left);
        var right = Evaluate(node.Right);

        if (left.IsError) return left.Error.Value;
        if (right.IsError) return right.Error.Value;

        return node.Operator switch
        {
            BinaryOperator.Add => Add(left.Value, right.Value),
            BinaryOperator.Subtract => Subtract(left.Value, right.Value),
            BinaryOperator.Multiply => Multiply(left.Value, right.Value),
            BinaryOperator.Divide => Divide(left.Value, right.Value),
            BinaryOperator.Power => Power(left.Value, right.Value),
            BinaryOperator.Concatenate => Concatenate(left.Value, right.Value),
            BinaryOperator.Equal => Compare(left.Value, right.Value, (a, b) => a == b),
            BinaryOperator.NotEqual => Compare(left.Value, right.Value, (a, b) => a != b),
            BinaryOperator.LessThan => Compare(left.Value, right.Value, (a, b) => a < b),
            BinaryOperator.LessEqual => Compare(left.Value, right.Value, (a, b) => a <= b),
            BinaryOperator.GreaterThan => Compare(left.Value, right.Value, (a, b) => a > b),
            BinaryOperator.GreaterEqual => Compare(left.Value, right.Value, (a, b) => a >= b),
            _ => ErrorValue.Value
        };
    }

    private FormulaResult EvaluateUnaryOp(UnaryOpNode node)
    {
        var operand = Evaluate(node.Operand);
        if (operand.IsError) return operand.Error.Value;

        return node.Operator switch
        {
            UnaryOperator.Plus => ToNumber(operand.Value),
            UnaryOperator.Minus => Negate(operand.Value),
            UnaryOperator.Percent => Percent(operand.Value),
            _ => ErrorValue.Value
        };
    }

    private FormulaResult EvaluateFunction(FunctionNode node)
    {
        if (!_functions.TryGet(node.Name, out var func))
        {
            return ErrorValue.Name;
        }

        // Evaluate arguments
        var args = new List<FormulaResult>();
        foreach (var argNode in node.Arguments)
        {
            args.Add(Evaluate(argNode));
        }

        try
        {
            return func.Invoke(args, _context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Function {Name} threw exception", node.Name);
            return ErrorValue.Value;
        }
    }

    // Arithmetic operations
    private FormulaResult Add(object? a, object? b)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError) return na.Error.Value;
        if (nb.IsError) return nb.Error.Value;
        return (double)na.Value! + (double)nb.Value!;
    }

    private FormulaResult Subtract(object? a, object? b)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError) return na.Error.Value;
        if (nb.IsError) return nb.Error.Value;
        return (double)na.Value! - (double)nb.Value!;
    }

    private FormulaResult Multiply(object? a, object? b)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError) return na.Error.Value;
        if (nb.IsError) return nb.Error.Value;
        return (double)na.Value! * (double)nb.Value!;
    }

    private FormulaResult Divide(object? a, object? b)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError) return na.Error.Value;
        if (nb.IsError) return nb.Error.Value;
        var denom = (double)nb.Value!;
        if (denom == 0) return ErrorValue.DivByZero;
        return (double)na.Value! / denom;
    }

    private FormulaResult Power(object? a, object? b)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError) return na.Error.Value;
        if (nb.IsError) return nb.Error.Value;
        return Math.Pow((double)na.Value!, (double)nb.Value!);
    }

    private FormulaResult Concatenate(object? a, object? b)
    {
        return a?.ToString() ?? "" + b?.ToString() ?? "";
    }

    private FormulaResult Compare(object? a, object? b, Func<double, double, bool> comparer)
    {
        var na = ToNumber(a);
        var nb = ToNumber(b);
        if (na.IsError || nb.IsError)
        {
            // String comparison fallback
            return comparer(string.CompareOrdinal(a?.ToString() ?? "", b?.ToString() ?? ""), 0);
        }
        return comparer((double)na.Value!, (double)nb.Value!);
    }

    private FormulaResult ToNumber(object? value)
    {
        if (value == null) return 0.0;
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is decimal m) return (double)m;
        if (value is float f) return f;
        if (value is bool b) return b ? 1.0 : 0.0;
        if (value is string s)
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                return result;
            // Try date parsing
            if (DateTime.TryParse(s, out var dt))
                return dt.ToOADate();
            return ErrorValue.Value;
        }
        return ErrorValue.Value;
    }

    private FormulaResult Negate(object? value)
    {
        var n = ToNumber(value);
        if (n.IsError) return n.Error.Value;
        return -(double)n.Value!;
    }

    private FormulaResult Percent(object? value)
    {
        var n = ToNumber(value);
        if (n.IsError) return n.Error.Value;
        return (double)n.Value! / 100.0;
    }
}