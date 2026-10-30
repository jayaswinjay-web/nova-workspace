using System.Text;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Token types for formula parsing.
/// </summary>
public enum TokenType
{
    // Literals
    Number,
    String,
    Boolean,
    Error,
    
    // References
    CellRef,        // A1, $A$1
    RangeRef,       // A1:B10
    NamedRange,
    
    // Operators
    Plus,
    Minus,
    Multiply,
    Divide,
    Power,
    Concatenate,
    Equal,
    NotEqual,
    LessThan,
    LessEqual,
    GreaterThan,
    GreaterEqual,
    
    // Parentheses
    LeftParen,
    RightParen,
    
    // Function/Structure
    Comma,
    Semicolon,
    Colon,
    AtSign,         // @ for implicit intersection
    
    // Special
    Function,
    SheetRef,       // Sheet1!
    EndOfInput,
    Unknown
}

/// <summary>
/// Represents a token in the formula.
/// </summary>
public readonly struct Token
{
    public TokenType Type { get; }
    public string Text { get; }
    public object? Value { get; }
    public int Position { get; }
    public int Length { get; }

    public Token(TokenType type, string text, object? value, int position, int length)
    {
        Type = type;
        Text = text;
        Value = value;
        Position = position;
        Length = length;
    }

    public override string ToString() => $"{Type}: '{Text}' (pos {Position})";
}

/// <summary>
/// Tokenizes formula strings into tokens.
/// </summary>
public sealed class FormulaTokenizer
{
    private readonly ILogger<FormulaTokenizer> _logger;
    private readonly string _input;
    private int _position;
    private readonly List<Token> _tokens = new();

    public FormulaTokenizer(ILogger<FormulaTokenizer> logger)
    {
        _logger = logger;
        _input = string.Empty;
    }

    /// <summary>
    /// Tokenize a formula string.
    /// </summary>
    public IReadOnlyList<Token> Tokenize(string formula)
    {
        _input = formula ?? string.Empty;
        _position = 0;
        _tokens.Clear();

        if (string.IsNullOrWhiteSpace(_input))
        {
            _tokens.Add(new Token(TokenType.EndOfInput, "", null, 0, 0));
            return _tokens;
        }

        // Skip leading equals sign
        if (_input[0] == '=')
        {
            _position = 1;
        }

        while (_position < _input.Length)
        {
            var ch = _input[_position];

            if (char.IsWhiteSpace(ch))
            {
                _position++;
                continue;
            }

            if (TryReadNumber(out var numberToken))
            {
                _tokens.Add(numberToken);
            }
            else if (TryReadString(out var stringToken))
            {
                _tokens.Add(stringToken);
            }
            else if (TryReadBoolean(out var boolToken))
            {
                _tokens.Add(boolToken);
            }
            else if (TryReadError(out var errorToken))
            {
                _tokens.Add(errorToken);
            }
            else if (TryReadReference(out var refToken))
            {
                _tokens.Add(refToken);
            }
            else if (TryReadOperator(out var opToken))
            {
                _tokens.Add(opToken);
            }
            else if (TryReadPunctuation(out var punctToken))
            {
                _tokens.Add(punctToken);
            }
            else if (TryReadFunctionName(out var funcToken))
            {
                _tokens.Add(funcToken);
            }
            else
            {
                // Unknown character
                _tokens.Add(new Token(TokenType.Unknown, ch.ToString(), null, _position, 1));
                _position++;
            }
        }

        _tokens.Add(new Token(TokenType.EndOfInput, "", null, _position, 0));
        return _tokens;
    }

    private bool TryReadNumber(out Token token)
    {
        token = default;
        var start = _position;
        var hasDecimal = false;
        var hasExponent = false;

        while (_position < _input.Length)
        {
            var ch = _input[_position];
            if (char.IsDigit(ch))
            {
                _position++;
            }
            else if (ch == '.' && !hasDecimal && !hasExponent)
            {
                hasDecimal = true;
                _position++;
            }
            else if ((ch == 'e' || ch == 'E') && !hasExponent)
            {
                hasExponent = true;
                _position++;
                if (_position < _input.Length && (_input[_position] == '+' || _input[_position] == '-'))
                {
                    _position++;
                }
            }
            else
            {
                break;
            }
        }

        if (_position > start)
        {
            var text = _input[start.._position];
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                token = new Token(TokenType.Number, text, value, start, _position - start);
                return true;
            }
        }

        _position = start;
        return false;
    }

    private bool TryReadString(out Token token)
    {
        token = default;
        if (_position >= _input.Length || _input[_position] != '"')
            return false;

        var start = _position;
        _position++; // Skip opening quote
        var sb = new StringBuilder();

        while (_position < _input.Length)
        {
            var ch = _input[_position];
            if (ch == '"')
            {
                _position++;
                if (_position < _input.Length && _input[_position] == '"')
                {
                    // Escaped quote
                    sb.Append('"');
                    _position++;
                }
                else
                {
                    // End of string
                    break;
                }
            }
            else
            {
                sb.Append(ch);
                _position++;
            }
        }

        var text = _input[start.._position];
        token = new Token(TokenType.String, text, sb.ToString(), start, _position - start);
        return true;
    }

    private bool TryReadBoolean(out Token token)
    {
        token = default;
        if (_position + 4 <= _input.Length && _input[_position..(_position + 4)].Equals("TRUE", StringComparison.OrdinalIgnoreCase))
        {
            token = new Token(TokenType.Boolean, "TRUE", true, _position, 4);
            _position += 4;
            return true;
        }
        if (_position + 5 <= _input.Length && _input[_position..(_position + 5)].Equals("FALSE", StringComparison.OrdinalIgnoreCase))
        {
            token = new Token(TokenType.Boolean, "FALSE", false, _position, 5);
            _position += 5;
            return true;
        }
        return false;
    }

    private bool TryReadError(out Token token)
    {
        token = default;
        var errors = new[] { "#DIV/0!", "#REF!", "#NAME?", "#VALUE!", "#N/A", "#NUM!", "#NULL!", "#CIRC!", "#SPILL!", "#CALC!" };
        foreach (var error in errors)
        {
            if (_position + error.Length <= _input.Length && _input[_position..(_position + error.Length)].Equals(error, StringComparison.OrdinalIgnoreCase))
            {
                token = new Token(TokenType.Error, error, error, _position, error.Length);
                _position += error.Length;
                return true;
            }
        }
        return false;
    }

    private bool TryReadReference(out Token token)
    {
        token = default;
        var start = _position;

        // Check for sheet reference: 'Sheet Name'! or SheetName!
        string? sheetName = null;
        if (_input[_position] == '\'')
        {
            _position++;
            var sheetStart = _position;
            while (_position < _input.Length && _input[_position] != '\'')
                _position++;
            if (_position < _input.Length && _input[_position] == '\'')
            {
                sheetName = _input[sheetStart.._position];
                _position++;
            }
            if (_position < _input.Length && _input[_position] == '!')
            {
                _position++;
            }
        }
        else
        {
            var sheetStart = _position;
            while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_' || _input[_position] == ' '))
                _position++;
            if (_position > sheetStart && _position < _input.Length && _input[_position] == '!')
            {
                sheetName = _input[sheetStart.._position];
                _position++;
            }
            else
            {
                _position = sheetStart;
            }
        }

        // Check for column (letters)
        var colStart = _position;
        var colAbsolute = false;
        if (_position < _input.Length && _input[_position] == '$')
        {
            colAbsolute = true;
            _position++;
        }
        while (_position < _input.Length && char.IsLetter(_input[_position]))
        {
            _position++;
        }
        var colLength = _position - colStart;
        if (colLength == 0 && sheetName == null)
        {
            _position = start;
            return false;
        }

        // Check for row (digits)
        var rowStart = _position;
        var rowAbsolute = false;
        if (_position < _input.Length && _input[_position] == '$')
        {
            rowAbsolute = true;
            _position++;
        }
        while (_position < _input.Length && char.IsDigit(_input[_position]))
        {
            _position++;
        }
        var rowLength = _position - rowStart;

        // If we have a column but no row, it might be a column range reference
        // Check for range operator :
        var isRange = false;
        string? endCellRef = null;
        if (_position < _input.Length && _input[_position] == ':')
        {
            isRange = true;
            _position++;
            // Parse end reference (simplified)
            var endStart = _position;
            var endColAbs = false;
            if (_position < _input.Length && _input[_position] == '$')
            {
                endColAbs = true;
                _position++;
            }
            while (_position < _input.Length && char.IsLetter(_input[_position]))
                _position++;
            var endRowAbs = false;
            if (_position < _input.Length && _input[_position] == '$')
            {
                endRowAbs = true;
                _position++;
            }
            while (_position < _input.Length && char.IsDigit(_input[_position]))
                _position++;
            endCellRef = _input[endStart.._position];
        }

        var text = _input[start.._position];
        if (isRange)
        {
            token = new Token(TokenType.RangeRef, text, new RangeReference(sheetName, text, endCellRef), start, _position - start);
        }
        else if (colLength > 0 || rowLength > 0)
        {
            token = new Token(TokenType.CellRef, text, new CellReference(sheetName, text, colAbsolute, rowAbsolute), start, _position - start);
        }
        else if (sheetName != null)
        {
            token = new Token(TokenType.SheetRef, text, sheetName, start, _position - start);
        }
        else
        {
            _position = start;
            return false;
        }
        return true;
    }

    private bool TryReadOperator(out Token token)
    {
        token = default;
        var ch = _input[_position];
        return ch switch
        {
            '+' => SingleChar(TokenType.Plus),
            '-' => SingleChar(TokenType.Minus),
            '*' => SingleChar(TokenType.Multiply),
            '/' => SingleChar(TokenType.Divide),
            '^' => SingleChar(TokenType.Power),
            '&' => SingleChar(TokenType.Concatenate),
            '=' => SingleChar(TokenType.Equal),
            '<' => TryTwoChar('<', '=', TokenType.LessEqual, TokenType.LessThan),
            '>' => TryTwoChar('>', '=', TokenType.GreaterEqual, TokenType.GreaterThan),
            _ when _position + 1 < _input.Length && ch == '<' && _input[_position + 1] == '>' => TwoChar(TokenType.NotEqual),
            _ => false
        };

        bool SingleChar(TokenType type)
        {
            token = new Token(type, ch.ToString(), null, _position, 1);
            _position++;
            return true;
        }

        bool TryTwoChar(char first, char second, TokenType twoCharType, TokenType oneCharType)
        {
            if (_position + 1 < _input.Length && _input[_position + 1] == second)
            {
                token = new Token(twoCharType, $"{first}{second}", null, _position, 2);
                _position += 2;
                return true;
            }
            token = new Token(oneCharType, first.ToString(), null, _position, 1);
            _position++;
            return true;
        }

        bool TwoChar(TokenType type)
        {
            token = new Token(type, _input[_position..(_position + 2)], null, _position, 2);
            _position += 2;
            return true;
        }
    }

    private bool TryReadPunctuation(out Token token)
    {
        token = default;
        var ch = _input[_position];
        return ch switch
        {
            '(' => SingleChar(TokenType.LeftParen),
            ')' => SingleChar(TokenType.RightParen),
            ',' => SingleChar(TokenType.Comma),
            ';' => SingleChar(TokenType.Semicolon),
            ':' => SingleChar(TokenType.Colon),
            '@' => SingleChar(TokenType.AtSign),
            _ => false
        };

        bool SingleChar(TokenType type)
        {
            token = new Token(type, ch.ToString(), null, _position, 1);
            _position++;
            return true;
        }
    }

    private bool TryReadFunctionName(out Token token)
    {
        token = default;
        var start = _position;
        while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_' || _input[_position] == '.'))
        {
            _position++;
        }

        if (_position > start)
        {
            var text = _input[start.._position];
            // Check if followed by ( (function call)
            var lookahead = _position;
            while (lookahead < _input.Length && char.IsWhiteSpace(_input[lookahead]))
                lookahead++;
            if (lookahead < _input.Length && _input[lookahead] == '(')
            {
                token = new Token(TokenType.Function, text, text.ToUpperInvariant(), start, _position - start);
                return true;
            }
        }

        _position = start;
        return false;
    }
}

/// <summary>
/// Cell reference information.
/// </summary>
public readonly struct CellReference
{
    public string? SheetName { get; }
    public string Reference { get; }
    public bool ColAbsolute { get; }
    public bool RowAbsolute { get; }

    public CellReference(string? sheetName, string reference, bool colAbsolute, bool rowAbsolute)
    {
        SheetName = sheetName;
        Reference = reference;
        ColAbsolute = colAbsolute;
        RowAbsolute = rowAbsolute;
    }
}

/// <summary>
/// Range reference information.
/// </summary>
public readonly struct RangeReference
{
    public string? SheetName { get; }
    public string StartRef { get; }
    public string? EndRef { get; }

    public RangeReference(string? sheetName, string startRef, string? endRef)
    {
        SheetName = sheetName;
        StartRef = startRef;
        EndRef = endRef;
    }
}