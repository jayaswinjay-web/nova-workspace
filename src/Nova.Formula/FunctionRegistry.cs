using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Function signature for metadata.
/// </summary>
public sealed class FunctionSignature
{
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public IReadOnlyList<ParameterInfo> Parameters { get; }
    public bool IsVolatile { get; }

    public FunctionSignature(string name, string description, string category, IEnumerable<ParameterInfo> parameters, bool isVolatile = false)
    {
        Name = name;
        Description = description;
        Category = category;
        Parameters = parameters.ToList();
        IsVolatile = isVolatile;
    }
}

/// <summary>
/// Parameter information for functions.
/// </summary>
public sealed class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public string Description { get; }
    public bool IsOptional { get; }
    public object? DefaultValue { get; }
    public bool IsRepeatable { get; } // For varargs

    public ParameterInfo(string name, string type, string description, bool isOptional = false, object? defaultValue = null, bool isRepeatable = false)
    {
        Name = name;
        Type = type;
        Description = description;
        IsOptional = isOptional;
        DefaultValue = defaultValue;
        IsRepeatable = isRepeatable;
    }
}

/// <summary>
/// Delegate for function implementations.
/// </summary>
public delegate FormulaResult FunctionImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context);

/// <summary>
/// Registry of all available functions.
/// </summary>
public sealed class FunctionRegistry
{
    private readonly Dictionary<string, FunctionImpl> _functions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FunctionSignature> _signatures = new(StringComparer.OrdinalIgnoreCase);

    public FunctionRegistry()
    {
        RegisterAllFunctions();
    }

    /// <summary>
    /// Register a function.
    /// </summary>
    public void Register(string name, FunctionImpl impl, FunctionSignature signature)
    {
        _functions[name.ToUpperInvariant()] = impl;
        _signatures[name.ToUpperInvariant()] = signature;
    }

    /// <summary>
    /// Try to get a function implementation.
    /// </summary>
    public bool TryGet(string name, out FunctionImpl impl)
    {
        return _functions.TryGetValue(name.ToUpperInvariant(), out impl!);
    }

    /// <summary>
    /// Get function signature.
    /// </summary>
    public bool TryGetSignature(string name, out FunctionSignature signature)
    {
        return _signatures.TryGetValue(name.ToUpperInvariant(), out signature!);
    }

    /// <summary>
    /// Get all function names.
    /// </summary>
    public IEnumerable<string> GetFunctionNames() => _functions.Keys;

    /// <summary>
    /// Get all function signatures.
    /// </summary>
    public IEnumerable<FunctionSignature> GetSignatures() => _signatures.Values;

    private void RegisterAllFunctions()
    {
        // Math & Trig
        Register("SUM", SumImpl, new FunctionSignature("SUM", "Adds all numbers in a range", "Math",
            new[] { new ParameterInfo("number1", "number", "First number or range", false), new ParameterInfo("number2", "number", "Additional numbers", true, null, true) }));

        Register("AVERAGE", AverageImpl, new FunctionSignature("AVERAGE", "Returns the average of arguments", "Math",
            new[] { new ParameterInfo("number1", "number", "First number or range", false), new ParameterInfo("number2", "number", "Additional numbers", true, null, true) }));

        Register("MIN", MinImpl, new FunctionSignature("MIN", "Returns the minimum value", "Math",
            new[] { new ParameterInfo("number1", "number", "First number or range", false), new ParameterInfo("number2", "number", "Additional numbers", true, null, true) }));

        Register("MAX", MaxImpl, new FunctionSignature("MAX", "Returns the maximum value", "Math",
            new[] { new ParameterInfo("number1", "number", "First number or range", false), new ParameterInfo("number2", "number", "Additional numbers", true, null, true) }));

        Register("COUNT", CountImpl, new FunctionSignature("COUNT", "Counts numbers in arguments", "Math",
            new[] { new ParameterInfo("value1", "any", "First value or range", false), new ParameterInfo("value2", "any", "Additional values", true, null, true) }));

        Register("COUNTA", CountAImpl, new FunctionSignature("COUNTA", "Counts non-empty cells", "Math",
            new[] { new ParameterInfo("value1", "any", "First value or range", false), new ParameterInfo("value2", "any", "Additional values", true, null, true) }));

        Register("COUNTIF", CountIfImpl, new FunctionSignature("COUNTIF", "Counts cells matching criteria", "Math",
            new[] { new ParameterInfo("range", "range", "Range to evaluate", false), new ParameterInfo("criteria", "any", "Criteria to match", false) }));

        Register("SUMIF", SumIfImpl, new FunctionSignature("SUMIF", "Sums cells matching criteria", "Math",
            new[] { new ParameterInfo("range", "range", "Range to evaluate", false), new ParameterInfo("criteria", "any", "Criteria to match", false), new ParameterInfo("sum_range", "range", "Range to sum", true) }));

        Register("AVERAGEIF", AverageIfImpl, new FunctionSignature("AVERAGEIF", "Averages cells matching criteria", "Math",
            new[] { new ParameterInfo("range", "range", "Range to evaluate", false), new ParameterInfo("criteria", "any", "Criteria to match", false), new ParameterInfo("average_range", "range", "Range to average", true) }));

        Register("ROUND", RoundImpl, new FunctionSignature("ROUND", "Rounds a number to specified digits", "Math",
            new[] { new ParameterInfo("number", "number", "Number to round", false), new ParameterInfo("num_digits", "number", "Number of digits", false) }));

        Register("ROUNDUP", RoundUpImpl, new FunctionSignature("ROUNDUP", "Rounds a number up", "Math",
            new[] { new ParameterInfo("number", "number", "Number to round", false), new ParameterInfo("num_digits", "number", "Number of digits", false) }));

        Register("ROUNDDOWN", RoundDownImpl, new FunctionSignature("ROUNDDOWN", "Rounds a number down", "Math",
            new[] { new ParameterInfo("number", "number", "Number to round", false), new ParameterInfo("num_digits", "number", "Number of digits", false) }));

        Register("ABS", AbsImpl, new FunctionSignature("ABS", "Returns absolute value", "Math",
            new[] { new ParameterInfo("number", "number", "Number", false) }));

        Register("SQRT", SqrtImpl, new FunctionSignature("SQRT", "Returns square root", "Math",
            new[] { new ParameterInfo("number", "number", "Number", false) }));

        Register("POWER", PowerImpl, new FunctionSignature("POWER", "Returns number raised to power", "Math",
            new[] { new ParameterInfo("number", "number", "Base", false), new ParameterInfo("power", "number", "Exponent", false) }));

        Register("MOD", ModImpl, new FunctionSignature("MOD", "Returns remainder from division", "Math",
            new[] { new ParameterInfo("number", "number", "Dividend", false), new ParameterInfo("divisor", "number", "Divisor", false) }));

        // Logical
        Register("IF", IfImpl, new FunctionSignature("IF", "Returns value based on condition", "Logical",
            new[] { new ParameterInfo("logical_test", "boolean", "Condition", false), new ParameterInfo("value_if_true", "any", "True value", false), new ParameterInfo("value_if_false", "any", "False value", true) }));

        Register("IFS", IfsImpl, new FunctionSignature("IFS", "Checks multiple conditions", "Logical",
            new[] { new ParameterInfo("logical_test1", "boolean", "First condition", false), new ParameterInfo("value_if_true1", "any", "First true value", false), new ParameterInfo("logical_test2", "boolean", "Additional conditions", true, null, true) }));

        Register("AND", AndImpl, new FunctionSignature("AND", "Returns TRUE if all conditions true", "Logical",
            new[] { new ParameterInfo("logical1", "boolean", "First condition", false), new ParameterInfo("logical2", "boolean", "Additional conditions", true, null, true) }));

        Register("OR", OrImpl, new FunctionSignature("OR", "Returns TRUE if any condition true", "Logical",
            new[] { new ParameterInfo("logical1", "boolean", "First condition", false), new ParameterInfo("logical2", "boolean", "Additional conditions", true, null, true) }));

        Register("NOT", NotImpl, new FunctionSignature("NOT", "Reverses logical value", "Logical",
            new[] { new ParameterInfo("logical", "boolean", "Condition", false) }));

        Register("IFERROR", IfErrorImpl, new FunctionSignature("IFERROR", "Returns value if error", "Logical",
            new[] { new ParameterInfo("value", "any", "Value to check", false), new ParameterInfo("value_if_error", "any", "Error replacement", false) }));

        // Lookup & Reference
        Register("VLOOKUP", VLookupImpl, new FunctionSignature("VLOOKUP", "Vertical lookup", "Lookup",
            new[] { new ParameterInfo("lookup_value", "any", "Value to find", false), new ParameterInfo("table_array", "range", "Table to search", false), new ParameterInfo("col_index_num", "number", "Column index", false), new ParameterInfo("range_lookup", "boolean", "Exact match", true, true) }));

        Register("HLOOKUP", HLookupImpl, new FunctionSignature("HLOOKUP", "Horizontal lookup", "Lookup",
            new[] { new ParameterInfo("lookup_value", "any", "Value to find", false), new ParameterInfo("table_array", "range", "Table to search", false), new ParameterInfo("row_index_num", "number", "Row index", false), new ParameterInfo("range_lookup", "boolean", "Exact match", true, true) }));

        Register("XLOOKUP", XLookupImpl, new FunctionSignature("XLOOKUP", "Modern lookup", "Lookup",
            new[] { new ParameterInfo("lookup_value", "any", "Value to find", false), new ParameterInfo("lookup_array", "range", "Array to search", false), new ParameterInfo("return_array", "range", "Array to return", false), new ParameterInfo("if_not_found", "any", "Not found value", true), new ParameterInfo("match_mode", "number", "Match mode", true, 0), new ParameterInfo("search_mode", "number", "Search mode", true, 1) }));

        Register("INDEX", IndexImpl, new FunctionSignature("INDEX", "Returns value from range", "Lookup",
            new[] { new ParameterInfo("array", "range", "Range", false), new ParameterInfo("row_num", "number", "Row", false), new ParameterInfo("col_num", "number", "Column", true) }));

        Register("MATCH", MatchImpl, new FunctionSignature("MATCH", "Returns position of value", "Lookup",
            new[] { new ParameterInfo("lookup_value", "any", "Value to find", false), new ParameterInfo("lookup_array", "range", "Array to search", false), new ParameterInfo("match_type", "number", "Match type", true, 1) }));

        // Text
        Register("CONCAT", ConcatImpl, new FunctionSignature("CONCAT", "Joins text strings", "Text",
            new[] { new ParameterInfo("text1", "string", "First text", false), new ParameterInfo("text2", "string", "Additional text", true, null, true) }));

        Register("TEXTJOIN", TextJoinImpl, new FunctionSignature("TEXTJOIN", "Joins text with delimiter", "Text",
            new[] { new ParameterInfo("delimiter", "string", "Delimiter", false), new ParameterInfo("ignore_empty", "boolean", "Ignore empty", false), new ParameterInfo("text1", "string", "First text", false), new ParameterInfo("text2", "string", "Additional text", true, null, true) }));

        Register("LEFT", LeftImpl, new FunctionSignature("LEFT", "Returns left characters", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false), new ParameterInfo("num_chars", "number", "Characters", true, 1) }));

        Register("RIGHT", RightImpl, new FunctionSignature("RIGHT", "Returns right characters", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false), new ParameterInfo("num_chars", "number", "Characters", true, 1) }));

        Register("MID", MidImpl, new FunctionSignature("MID", "Returns middle characters", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false), new ParameterInfo("start_num", "number", "Start position", false), new ParameterInfo("num_chars", "number", "Characters", false) }));

        Register("LEN", LenImpl, new FunctionSignature("LEN", "Returns text length", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false) }));

        Register("TRIM", TrimImpl, new FunctionSignature("TRIM", "Removes extra spaces", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false) }));

        Register("UPPER", UpperImpl, new FunctionSignature("UPPER", "Converts to uppercase", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false) }));

        Register("LOWER", LowerImpl, new FunctionSignature("LOWER", "Converts to lowercase", "Text",
            new[] { new ParameterInfo("text", "string", "Text", false) }));

        // Date & Time
        Register("TODAY", TodayImpl, new FunctionSignature("TODAY", "Returns current date", "Date",
            new ParameterInfo[0], true));

        Register("NOW", NowImpl, new FunctionSignature("NOW", "Returns current date and time", "Date",
            new ParameterInfo[0], true));

        Register("DATE", DateImpl, new FunctionSignature("DATE", "Creates a date", "Date",
            new[] { new ParameterInfo("year", "number", "Year", false), new ParameterInfo("month", "number", "Month", false), new ParameterInfo("day", "number", "Day", false) }));

        Register("YEAR", YearImpl, new FunctionSignature("YEAR", "Returns year from date", "Date",
            new[] { new ParameterInfo("serial_number", "number", "Date", false) }));

        Register("MONTH", MonthImpl, new FunctionSignature("MONTH", "Returns month from date", "Date",
            new[] { new ParameterInfo("serial_number", "number", "Date", false) }));

        Register("DAY", DayImpl, new FunctionSignature("DAY", "Returns day from date", "Date",
            new[] { new ParameterInfo("serial_number", "number", "Date", false) }));

        // Financial
        Register("PMT", PmtImpl, new FunctionSignature("PMT", "Calculates loan payment", "Financial",
            new[] { new ParameterInfo("rate", "number", "Interest rate", false), new ParameterInfo("nper", "number", "Number of periods", false), new ParameterInfo("pv", "number", "Present value", false), new ParameterInfo("fv", "number", "Future value", true, 0), new ParameterInfo("type", "number", "Payment timing", true, 0) }));

        // Random
        Register("RAND", RandImpl, new FunctionSignature("RAND", "Returns random number 0-1", "Random",
            new ParameterInfo[0], true));

        Register("RANDBETWEEN", RandBetweenImpl, new FunctionSignature("RANDBETWEEN", "Returns random integer", "Random",
            new[] { new ParameterInfo("bottom", "number", "Minimum", false), new ParameterInfo("top", "number", "Maximum", false) }, true));
    }

    // Implementation methods
    private FormulaResult SumImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        double sum = 0;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range)
                {
                    if (!v.IsError && ToNumber(v.Value, out var n)) sum += n;
                }
            }
            else if (ToNumber(arg.Value, out var n))
            {
                sum += n;
            }
        }
        return sum;
    }

    private FormulaResult AverageImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        double sum = 0;
        int count = 0;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range)
                {
                    if (!v.IsError && ToNumber(v.Value, out var n)) { sum += n; count++; }
                }
            }
            else if (ToNumber(arg.Value, out var n))
            {
                sum += n; count++;
            }
        }
        return count > 0 ? sum / count : ErrorValue.DivByZero;
    }

    private FormulaResult MinImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        double? min = null;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range)
                {
                    if (!v.IsError && ToNumber(v.Value, out var n))
                    {
                        if (!min.HasValue || n < min) min = n;
                    }
                }
            }
            else if (ToNumber(arg.Value, out var n))
            {
                if (!min.HasValue || n < min) min = n;
            }
        }
        return min ?? ErrorValue.Value;
    }

    private FormulaResult MaxImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        double? max = null;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range)
                {
                    if (!v.IsError && ToNumber(v.Value, out var n))
                    {
                        if (!max.HasValue || n > max) max = n;
                    }
                }
            }
            else if (ToNumber(arg.Value, out var n))
            {
                if (!max.HasValue || n > max) max = n;
            }
        }
        return max ?? ErrorValue.Value;
    }

    private FormulaResult CountImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        int count = 0;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range) if (!v.IsError && ToNumber(v.Value, out _)) count++;
            }
            else if (ToNumber(arg.Value, out _)) count++;
        }
        return count;
    }

    private FormulaResult CountAImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        int count = 0;
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range) if (!v.IsError && v.Value != null) count++;
            }
            else if (arg.Value != null) count++;
        }
        return count;
    }

    private FormulaResult CountIfImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        var range = args[0];
        var criteria = args[1];
        if (range.IsError) return range.Error.Value;

        int count = 0;
        if (range.Value is IEnumerable<FormulaResult> rangeValues)
        {
            foreach (var v in rangeValues)
            {
                if (!v.IsError && MatchesCriteria(v.Value, criteria.Value)) count++;
            }
        }
        return count;
    }

    private FormulaResult SumIfImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        var range = args[0];
        var criteria = args[1];
        var sumRange = args.Count > 2 ? args[2] : range;
        if (range.IsError) return range.Error.Value;

        double sum = 0;
        int idx = 0;
        if (range.Value is IEnumerable<FormulaResult> rangeValues)
        {
            IEnumerable<FormulaResult>? sumValues = null;
            if (sumRange.Value is IEnumerable<FormulaResult> sr) sumValues = sr;

            foreach (var v in rangeValues)
            {
                var sv = sumValues?.ElementAtOrDefault(idx);
                if (!v.IsError && MatchesCriteria(v.Value, criteria.Value))
                {
                    if (sv.HasValue && !sv.Value.IsError && ToNumber(sv.Value.Value, out var n)) sum += n;
                    else if (!sumValues.HasValue && ToNumber(v.Value, out var n2)) sum += n2;
                }
                idx++;
            }
        }
        return sum;
    }

    private FormulaResult AverageIfImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        var range = args[0];
        var criteria = args[1];
        var avgRange = args.Count > 2 ? args[2] : range;
        if (range.IsError) return range.Error.Value;

        double sum = 0;
        int count = 0;
        int idx = 0;
        if (range.Value is IEnumerable<FormulaResult> rangeValues)
        {
            IEnumerable<FormulaResult>? avgValues = null;
            if (avgRange.Value is IEnumerable<FormulaResult> ar) avgValues = ar;

            foreach (var v in rangeValues)
            {
                var av = avgValues?.ElementAtOrDefault(idx);
                if (!v.IsError && MatchesCriteria(v.Value, criteria.Value))
                {
                    if (av.HasValue && !av.Value.IsError && ToNumber(av.Value.Value, out var n)) { sum += n; count++; }
                    else if (!avgValues.HasValue && ToNumber(v.Value, out var n2)) { sum += n2; count++; }
                }
                idx++;
            }
        }
        return count > 0 ? sum / count : ErrorValue.DivByZero;
    }

    private FormulaResult RoundImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var d)) return ErrorValue.Value;
        return Math.Round(n, (int)d);
    }

    private FormulaResult RoundUpImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var d)) return ErrorValue.Value;
        var mult = Math.Pow(10, (int)d);
        return Math.Ceiling(n * mult) / mult;
    }

    private FormulaResult RoundDownImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var d)) return ErrorValue.Value;
        var mult = Math.Pow(10, (int)d);
        return Math.Floor(n * mult) / mult;
    }

    private FormulaResult AbsImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        return Math.Abs(n);
    }

    private FormulaResult SqrtImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (n < 0) return ErrorValue.Num;
        return Math.Sqrt(n);
    }

    private FormulaResult PowerImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var p)) return ErrorValue.Value;
        return Math.Pow(n, p);
    }

    private FormulaResult ModImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var n)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var d)) return ErrorValue.Value;
        if (d == 0) return ErrorValue.DivByZero;
        return n % d;
    }

    private FormulaResult IfImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        var condition = ToBoolean(args[0].Value);
        if (condition) return args.Count > 2 ? args[1] : new FormulaResult(true);
        return args.Count > 3 ? args[2] : new FormulaResult(false);
    }

    private FormulaResult IfsImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        for (int i = 0; i < args.Count; i += 2)
        {
            if (i + 1 >= args.Count) break;
            if (args[i].IsError) return args[i].Error.Value;
            if (ToBoolean(args[i].Value)) return args[i + 1];
        }
        return ErrorValue.NA;
    }

    private FormulaResult AndImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (!ToBoolean(arg.Value)) return false;
        }
        return true;
    }

    private FormulaResult OrImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (ToBoolean(arg.Value)) return true;
        }
        return false;
    }

    private FormulaResult NotImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        return !ToBoolean(args[0].Value);
    }

    private FormulaResult IfErrorImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (!args[0].IsError) return args[0];
        return args[1];
    }

    private FormulaResult VLookupImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (args[2].IsError) return args[2].Error.Value;

        var lookupVal = args[0].Value;
        var tableArray = args[1].Value as IEnumerable<FormulaResult>;
        if (!ToNumber(args[2].Value, out var colIndex)) return ErrorValue.Value;
        if (colIndex < 1) return ErrorValue.Value;
        var exactMatch = args.Count < 4 || ToBoolean(args[3].Value);

        if (tableArray == null) return ErrorValue.Ref;

        int row = 0;
        foreach (var tableRow in tableArray)
        {
            if (tableRow.Value is IEnumerable<FormulaResult> rowValues)
            {
                var rowList = rowValues.ToList();
                if (rowList.Count >= colIndex)
                {
                    var key = rowList[0].Value;
                    if (MatchesCriteria(key, lookupVal))
                    {
                        return rowList[(int)colIndex - 1];
                    }
                }
            }
            row++;
        }
        return ErrorValue.NA;
    }

    private FormulaResult HLookupImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (args[2].IsError) return args[2].Error.Value;

        var lookupVal = args[0].Value;
        var tableArray = args[1].Value as IEnumerable<FormulaResult>;
        if (!ToNumber(args[2].Value, out var rowIndex)) return ErrorValue.Value;
        if (rowIndex < 1) return ErrorValue.Value;
        var exactMatch = args.Count < 4 || ToBoolean(args[3].Value);

        if (tableArray == null) return ErrorValue.Ref;

        var tableList = tableArray.ToList();
        if (tableList.Count < rowIndex) return ErrorValue.Ref;

        var headerRow = tableList[0].Value as IEnumerable<FormulaResult>;
        var dataRow = tableList[(int)rowIndex - 1].Value as IEnumerable<FormulaResult>;
        if (headerRow == null || dataRow == null) return ErrorValue.Ref;

        var headerList = headerRow.ToList();
        var dataList = dataRow.ToList();
        for (int i = 0; i < headerList.Count; i++)
        {
            if (MatchesCriteria(headerList[i].Value, lookupVal))
            {
                return i < dataList.Count ? dataList[i] : ErrorValue.Ref;
            }
        }
        return ErrorValue.NA;
    }

    private FormulaResult XLookupImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        var lookupVal = args[0].Value;
        var lookupArray = args[1].Value as IEnumerable<FormulaResult>;
        var returnArray = args[2].Value as IEnumerable<FormulaResult>;
        var ifNotFound = args.Count > 3 ? args[3].Value : ErrorValue.NA;
        var matchMode = args.Count > 4 ? GetInt(args[4].Value, 0) : 0;
        var searchMode = args.Count > 5 ? GetInt(args[5].Value, 1) : 1;

        if (lookupArray == null || returnArray == null) return ErrorValue.Ref;

        var lookupList = lookupArray.ToList();
        var returnList = returnArray.ToList();

        for (int i = 0; i < lookupList.Count; i++)
        {
            if (MatchesCriteria(lookupList[i].Value, lookupVal))
            {
                if (i < returnList.Count) return returnList[i];
                break;
            }
        }
        return ifNotFound is ErrorValue ? (FormulaResult)ifNotFound : new FormulaResult(ifNotFound);
    }

    private FormulaResult IndexImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;

        var array = args[0].Value as IEnumerable<FormulaResult>;
        if (!ToNumber(args[1].Value, out var row)) return ErrorValue.Value;
        if (row < 1) return ErrorValue.Value;

        var arrayList = array?.ToList();
        if (arrayList == null || arrayList.Count < row) return ErrorValue.Ref;

        var rowValue = arrayList[(int)row - 1];
        if (args.Count > 2)
        {
            if (args[2].IsError) return args[2].Error.Value;
            if (!ToNumber(args[2].Value, out var col)) return ErrorValue.Value;
            if (col < 1) return ErrorValue.Value;
            if (rowValue.Value is IEnumerable<FormulaResult> rowArray)
            {
                var rowList = rowArray.ToList();
                if (rowList.Count >= col) return rowList[(int)col - 1];
                return ErrorValue.Ref;
            }
        }
        return rowValue;
    }

    private FormulaResult MatchImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;

        var lookupVal = args[0].Value;
        var lookupArray = args[1].Value as IEnumerable<FormulaResult>;
        var matchType = args.Count > 2 ? GetInt(args[2].Value, 1) : 1;

        if (lookupArray == null) return ErrorValue.Ref;

        var arrayList = lookupArray.ToList();
        for (int i = 0; i < arrayList.Count; i++)
        {
            if (MatchesCriteria(arrayList[i].Value, lookupVal))
            {
                return i + 1;
            }
        }
        return ErrorValue.NA;
    }

    private FormulaResult ConcatImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var arg in args)
        {
            if (arg.IsError) return arg.Error.Value;
            if (arg.Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range) if (!v.IsError) sb.Append(v.Value?.ToString() ?? "");
            }
            else sb.Append(arg.Value?.ToString() ?? "");
        }
        return sb.ToString();
    }

    private FormulaResult TextJoinImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;

        var delimiter = args[0].Value?.ToString() ?? "";
        var ignoreEmpty = ToBoolean(args[1].Value);
        var sb = new System.Text.StringBuilder();
        bool first = true;

        for (int i = 2; i < args.Count; i++)
        {
            if (args[i].IsError) return args[i].Error.Value;
            if (args[i].Value is IEnumerable<FormulaResult> range)
            {
                foreach (var v in range)
                {
                    if (v.IsError) continue;
                    var str = v.Value?.ToString() ?? "";
                    if (ignoreEmpty && string.IsNullOrEmpty(str)) continue;
                    if (!first) sb.Append(delimiter);
                    sb.Append(str);
                    first = false;
                }
            }
            else
            {
                var str = args[i].Value?.ToString() ?? "";
                if (ignoreEmpty && string.IsNullOrEmpty(str)) continue;
                if (!first) sb.Append(delimiter);
                sb.Append(str);
                first = false;
            }
        }
        return sb.ToString();
    }

    private FormulaResult LeftImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        var text = args[0].Value?.ToString() ?? "";
        var len = args.Count > 1 ? GetInt(args[1].Value, 1) : 1;
        return text.Substring(0, Math.Min(len, text.Length));
    }

    private FormulaResult RightImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        var text = args[0].Value?.ToString() ?? "";
        var len = args.Count > 1 ? GetInt(args[1].Value, 1) : 1;
        return text.Length <= len ? text : text.Substring(text.Length - len);
    }

    private FormulaResult MidImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        var text = args[0].Value?.ToString() ?? "";
        var start = GetInt(args[1].Value, 1) - 1;
        var len = GetInt(args[2].Value, 0);
        if (start < 0 || start >= text.Length) return "";
        return text.Substring(start, Math.Min(len, text.Length - start));
    }

    private FormulaResult LenImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        return (args[0].Value?.ToString() ?? "").Length;
    }

    private FormulaResult TrimImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        return System.Text.RegularExpressions.Regex.Replace(args[0].Value?.ToString() ?? "", @"\s+", " ").Trim();
    }

    private FormulaResult UpperImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        return (args[0].Value?.ToString() ?? "").ToUpperInvariant();
    }

    private FormulaResult LowerImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        return (args[0].Value?.ToString() ?? "").ToLowerInvariant();
    }

    private FormulaResult TodayImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        return DateTime.Today.ToOADate();
    }

    private FormulaResult NowImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        return DateTime.Now.ToOADate();
    }

    private FormulaResult DateImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (args[2].IsError) return args[2].Error.Value;
        if (!ToNumber(args[0].Value, out var y)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var m)) return ErrorValue.Value;
        if (!ToNumber(args[2].Value, out var d)) return ErrorValue.Value;
        try { return new DateTime((int)y, (int)m, (int)d).ToOADate(); } catch { return ErrorValue.Value; }
    }

    private FormulaResult YearImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (!ToNumber(args[0].Value, out var d)) return ErrorValue.Value;
        return DateTime.FromOADate(d).Year;
    }

    private FormulaResult MonthImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (!ToNumber(args[0].Value, out var d)) return ErrorValue.Value;
        return DateTime.FromOADate(d).Month;
    }

    private FormulaResult DayImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 1) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (!ToNumber(args[0].Value, out var d)) return ErrorValue.Value;
        return DateTime.FromOADate(d).Day;
    }

    private FormulaResult PmtImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 3) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (args[2].IsError) return args[2].Error.Value;
        if (!ToNumber(args[0].Value, out var rate)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var nper)) return ErrorValue.Value;
        if (!ToNumber(args[2].Value, out var pv)) return ErrorValue.Value;
        var fv = args.Count > 3 ? GetDouble(args[3].Value, 0) : 0;
        var type = args.Count > 4 ? GetInt(args[4].Value, 0) : 0;

        if (rate == 0) return - (pv + fv) / nper;
        var pvif = Math.Pow(1 + rate, nper);
        var pmt = (rate * (pv * pvif + fv)) / (pvif - 1);
        if (type == 1) pmt /= (1 + rate);
        return -pmt;
    }

    private FormulaResult RandImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        return Random.Shared.NextDouble();
    }

    private FormulaResult RandBetweenImpl(IReadOnlyList<FormulaResult> args, IEvaluationContext context)
    {
        if (args.Count < 2) return ErrorValue.Value;
        if (args[0].IsError) return args[0].Error.Value;
        if (args[1].IsError) return args[1].Error.Value;
        if (!ToNumber(args[0].Value, out var b)) return ErrorValue.Value;
        if (!ToNumber(args[1].Value, out var t)) return ErrorValue.Value;
        return Random.Shared.Next((int)b, (int)t + 1);
    }

    // Helper methods
    private bool ToNumber(object? value, out double result)
    {
        result = 0;
        if (value == null) return false;
        if (value is double d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is decimal m) { result = (double)m; return true; }
        if (value is float f) { result = f; return true; }
        if (value is bool b) { result = b ? 1 : 0; return true; }
        if (value is string s)
        {
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;
            if (DateTime.TryParse(s, out var dt)) { result = dt.ToOADate(); return true; }
            return false;
        }
        return false;
    }

    private double GetDouble(object? value, double defaultValue)
    {
        return ToNumber(value, out var d) ? d : defaultValue;
    }

    private int GetInt(object? value, int defaultValue)
    {
        return ToNumber(value, out var d) ? (int)d : defaultValue;
    }

    private bool ToBoolean(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is double d) return d != 0;
        if (value is int i) return i != 0;
        if (value is string s) return bool.TryParse(s, out var b) && b;
        return false;
    }

    private bool MatchesCriteria(object? cellValue, object? criteria)
    {
        if (criteria == null) return cellValue == null;
        var critStr = criteria.ToString() ?? "";
        var cellStr = cellValue?.ToString() ?? "";

        // Check for comparison operators
        if (critStr.StartsWith(">=")) return CompareValues(cellValue, critStr[2..], (a, b) => a >= b);
        if (critStr.StartsWith("<=")) return CompareValues(cellValue, critStr[2..], (a, b) => a <= b);
        if (critStr.StartsWith(">")) return CompareValues(cellValue, critStr[1..], (a, b) => a > b);
        if (critStr.StartsWith("<")) return CompareValues(cellValue, critStr[1..], (a, b) => a < b);
        if (critStr.StartsWith("<>")) return CompareValues(cellValue, critStr[2..], (a, b) => a != b);
        if (critStr.StartsWith("=")) return CompareValues(cellValue, critStr[1..], (a, b) => a == b);

        // Wildcard matching
        if (critStr.Contains('*') || critStr.Contains('?'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(critStr).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(cellStr, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Exact match
        return CompareValues(cellValue, critStr, (a, b) => a == b);
    }

    private bool CompareValues(object? cellValue, string criteria, Func<double, double, bool> comparer)
    {
        if (ToNumber(cellValue, out var cn) && ToNumber(criteria, out var ccn))
            return comparer(cn, ccn);
        return string.Equals(cellValue?.ToString(), criteria, StringComparison.OrdinalIgnoreCase);
    }
}