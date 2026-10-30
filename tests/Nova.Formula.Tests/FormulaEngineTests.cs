using Nova.Formula;
using Xunit;

namespace Nova.Formula.Tests;

public class FormulaEngineTests
{
    private readonly FormulaEngine _engine;

    public FormulaEngineTests()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<FormulaEngine>();
        _engine = new FormulaEngine(logger);
    }

    [Theory]
    [InlineData("=1+1", 2)]
    [InlineData("=2*3", 6)]
    [InlineData("=10/2", 5)]
    [InlineData("=2^3", 8)]
    [InlineData("=5-3", 2)]
    [InlineData("=\"Hello\"&\"World\"", "HelloWorld")]
    public void BasicArithmetic_ReturnsExpectedResult(string formula, object expected)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("=SUM(1,2,3)", 6)]
    [InlineData("=AVERAGE(1,2,3)", 2)]
    [InlineData("=MIN(1,2,3)", 1)]
    [InlineData("=MAX(1,2,3)", 3)]
    [InlineData("=COUNT(1,2,3)", 3)]
    [InlineData("=COUNTA(1,2,3)", 3)]
    [InlineData("=ROUND(1.567,2)", 1.57)]
    [InlineData("=ROUNDUP(1.1,0)", 2)]
    [InlineData("=ROUNDDOWN(1.9,0)", 1)]
    [InlineData("=ABS(-5)", 5)]
    [InlineData("=SQRT(16)", 4)]
    [InlineData("=POWER(2,3)", 8)]
    [InlineData("=MOD(10,3)", 1)]
    public void MathFunctions_ReturnExpectedResults(string formula, object expected)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("=IF(TRUE,\"yes\",\"no\")", "yes")]
    [InlineData("=IF(FALSE,\"yes\",\"no\")", "no")]
    [InlineData("=IF(1>2,\"yes\",\"no\")", "no")]
    [InlineData("=AND(TRUE,TRUE)", true)]
    [InlineData("=AND(TRUE,FALSE)", false)]
    [InlineData("=OR(TRUE,FALSE)", true)]
    [InlineData("=OR(FALSE,FALSE)", false)]
    [InlineData("=NOT(TRUE)", false)]
    [InlineData("=NOT(FALSE)", true)]
    [InlineData("=IFERROR(1/0,\"error\")", "error")]
    [InlineData("=IFERROR(1,\"error\")", 1)]
    public void LogicalFunctions_ReturnExpectedResults(string formula, object expected)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("=CONCAT(\"a\",\"b\",\"c\")", "abc")]
    [InlineData("=TEXTJOIN(\",\",TRUE,\"a\",\"b\",\"c\")", "a,b,c")]
    [InlineData("=LEFT(\"hello\",2)", "he")]
    [InlineData("=RIGHT(\"hello\",2)", "lo")]
    [InlineData("=MID(\"hello\",2,2)", "el")]
    [InlineData("=LEN(\"hello\")", 5)]
    [InlineData("=TRIM(\"  hello  \")", "hello")]
    [InlineData("=UPPER(\"hello\")", "HELLO")]
    [InlineData("=LOWER(\"HELLO\")", "hello")]
    public void TextFunctions_ReturnExpectedResults(string formula, object expected)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("=DATE(2024,1,15)", 45308.0)] // Excel date serial for 2024-01-15
    [InlineData("=YEAR(45308)", 2024)]
    [InlineData("=MONTH(45308)", 1)]
    [InlineData("=DAY(45308)", 15)]
    public void DateFunctions_ReturnExpectedResults(string formula, object expected)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void TODAY_ReturnsCurrentDate()
    {
        var result = _engine.Evaluate("=TODAY()", new TestContext());
        Assert.False(result.IsError);
        Assert.IsType<double>(result.Value);
        var date = DateTime.FromOADate((double)result.Value!);
        Assert.Equal(DateTime.Today, date.Date);
    }

    [Fact]
    public void NOW_ReturnsCurrentDateTime()
    {
        var result = _engine.Evaluate("=NOW()", new TestContext());
        Assert.False(result.IsError);
        Assert.IsType<double>(result.Value);
    }

    [Theory]
    [InlineData("=PMT(0.05/12,360,200000)", -1073.64, 0.01)] // Mortgage payment
    public void FinancialFunctions_ReturnExpectedResults(string formula, double expected, double tolerance)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.False(result.IsError);
        Assert.Equal(expected, (double)result.Value!, tolerance);
    }

    [Fact]
    public void RAND_ReturnsBetween0And1()
    {
        var result = _engine.Evaluate("=RAND()", new TestContext());
        Assert.False(result.IsError);
        var val = (double)result.Value!;
        Assert.InRange(val, 0.0, 1.0);
    }

    [Fact]
    public void RANDBETWEEN_ReturnsInRange()
    {
        var result = _engine.Evaluate("=RANDBETWEEN(1,10)", new TestContext());
        Assert.False(result.IsError);
        var val = (double)result.Value!;
        Assert.InRange(val, 1, 10);
    }

    [Theory]
    [InlineData("=1/0", "#DIV/0!")]
    [InlineData("=UNKNOWN()", "#NAME?")]
    public void ErrorHandling_ReturnsCorrectError(string formula, string expectedError)
    {
        var result = _engine.Evaluate(formula, new TestContext());
        Assert.True(result.IsError);
        Assert.Equal(expectedError, result.Error?.ToString());
    }

    [Fact]
    public void Parse_CachesResults()
    {
        var parsed1 = _engine.Parse("=1+1");
        var parsed2 = _engine.Parse("=1+1");
        Assert.Same(parsed1, parsed2);
    }

    [Fact]
    public void GetFunctions_ReturnsAllFunctions()
    {
        var functions = _engine.GetFunctions().ToList();
        Assert.Contains(functions, f => f.Name == "SUM");
        Assert.Contains(functions, f => f.Name == "VLOOKUP");
        Assert.Contains(functions, f => f.Name == "XLOOKUP");
        Assert.Contains(functions, f => f.Name == "IF");
        Assert.True(functions.Count >= 45); // At least the required functions
    }

    [Fact]
    public void TryGetFunction_ReturnsSignature()
    {
        Assert.True(_engine.TryGetFunction("SUM", out var sig));
        Assert.Equal("SUM", sig.Name);
        Assert.Equal("Math", sig.Category);
        Assert.Equal(2, sig.Parameters.Count); // number1, number2...
    }

    [Fact]
    public void DependencyGraph_TracksDependencies()
    {
        _engine.RegisterFormula("A1", "=B1+C1");
        _engine.RegisterFormula("B1", "=D1*2");
        _engine.RegisterFormula("C1", "=5");
        _engine.RegisterFormula("D1", "=10");

        var order = _engine.GetRecalcOrder(new[] { "D1" });
        Assert.Contains("D1", order);
        Assert.Contains("B1", order);
        Assert.Contains("A1", order);
        // D1 should come before B1, B1 before A1
        Assert.True(order.IndexOf("D1") < order.IndexOf("B1"));
        Assert.True(order.IndexOf("B1") < order.IndexOf("A1"));
    }

    [Fact]
    public void CircularReference_Detected()
    {
        _engine.RegisterFormula("A1", "=B1+1");
        _engine.RegisterFormula("B1", "=A1+1");

        Assert.True(_engine.HasCircularReference("A1", out var cycle));
        Assert.Contains("A1", cycle);
        Assert.Contains("B1", cycle);
    }
}

public class TestContext : IEvaluationContext
{
    private readonly Dictionary<string, FormulaResult> _cells = new();

    public FormulaResult GetCellValue(string sheetName, string cellRef)
    {
        var key = $"{sheetName}!{cellRef}";
        return _cells.TryGetValue(key, out var value) ? value : new FormulaResult(0.0);
    }

    public IReadOnlyList<FormulaResult> GetRangeValues(string sheetName, string startRef, string endRef)
    {
        return Array.Empty<FormulaResult>();
    }

    public FormulaResult GetNamedRange(string name)
    {
        return ErrorValue.Name;
    }

    public string CurrentSheet => "Sheet1";
    public string CurrentCell => "";
}