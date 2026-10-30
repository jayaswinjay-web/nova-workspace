using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Nova.Formula;

/// <summary>
/// Abstract syntax tree nodes.
/// </summary>
public abstract class AstNode
{
    public Token Token { get; }
    public int StartPosition => Token.Position;
    public int Length => Token.Length;

    protected AstNode(Token token)
    {
        Token = token;
    }
}

/// <summary>
/// Literal value node.
/// </summary>
public sealed class LiteralNode : AstNode
{
    public object? Value { get; }

    public LiteralNode(Token token, object? value) : base(token)
    {
        Value = value;
    }
}

/// <summary>
/// Cell reference node.
/// </summary>
public sealed class ReferenceNode : AstNode
{
    public CellReference Reference { get; }

    public ReferenceNode(Token token, CellReference reference) : base(token)
    {
        Reference = reference;
    }
}

/// <summary>
/// Range reference node.
/// </summary>
public sealed class RangeNode : AstNode
{
    public RangeReference Range { get; }

    public RangeNode(Token token, RangeReference range) : base(token)
    {
        Range = range;
    }
}

/// <summary>
/// Named range node.
/// </summary>
public sealed class NamedRangeNode : AstNode
{
    public string Name { get; }

    public NamedRangeNode(Token token, string name) : base(token)
    {
        Name = name;
    }
}

/// <summary>
/// Binary operation node.
/// </summary>
public sealed class BinaryOpNode : AstNode
{
    public AstNode Left { get; }
    public AstNode Right { get; }
    public BinaryOperator Operator { get; }

    public BinaryOpNode(Token token, AstNode left, AstNode right, BinaryOperator op) : base(token)
    {
        Left = left;
        Right = right;
        Operator = op;
    }
}

/// <summary>
/// Unary operation node.
/// </summary>
public sealed class UnaryOpNode : AstNode
{
    public AstNode Operand { get; }
    public UnaryOperator Operator { get; }

    public UnaryOpNode(Token token, AstNode operand, UnaryOperator op) : base(token)
    {
        Operand = operand;
        Operator = op;
    }
}

/// <summary>
/// Function call node.
/// </summary>
public sealed class FunctionNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<AstNode> Arguments { get; }

    public FunctionNode(Token token, string name, IReadOnlyList<AstNode> arguments) : base(token)
    {
        Name = name;
        Arguments = arguments;
    }
}

/// <summary>
/// Parenthesized expression node.
/// </summary>
public sealed class ParenNode : AstNode
{
    public AstNode Expression { get; }

    public ParenNode(Token token, AstNode expression) : base(token)
    {
        Expression = expression;
    }
}

/// <summary>
/// Binary operators.
/// </summary>
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Power,
    Concatenate,
    Equal,
    NotEqual,
    LessThan,
    LessEqual,
    GreaterThan,
    GreaterEqual
}

/// <summary>
/// Unary operators.
/// </summary>
public enum UnaryOperator
{
    Plus,
    Minus,
    Percent
}

/// <summary>
/// Parses tokens into an AST.
/// </summary>
public sealed class FormulaParser
{
    private readonly ILogger<FormulaParser> _logger;
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;
    private readonly FormulaTokenizer _tokenizer;

    public FormulaParser(ILogger<FormulaParser> logger, FormulaTokenizer tokenizer)
    {
        _logger = logger;
        _tokenizer = tokenizer;
        _tokens = Array.Empty<Token>();
    }

    /// <summary>
    /// Parse a formula string into an AST.
    /// </summary>
    public AstNode Parse(string formula)
    {
        _tokens = _tokenizer.Tokenize(formula);
        _position = 0;
        return ParseExpression();
    }

    private AstNode ParseExpression(int precedence = 0)
    {
        var left = ParseUnary();

        while (true)
        {
            var token = CurrentToken;
            var op = GetBinaryOperator(token.Type);
            if (op == null) break;

            var opPrecedence = GetPrecedence(op.Value);
            if (opPrecedence < precedence) break;

            _position++;
            var right = ParseExpression(opPrecedence + 1);
            left = new BinaryOpNode(token, left, right, op.Value);
        }

        return left;
    }

    private AstNode ParseUnary()
    {
        var token = CurrentToken;
        var op = GetUnaryOperator(token.Type);
        if (op != null)
        {
            _position++;
            var operand = ParseUnary();
            return new UnaryOpNode(token, operand, op.Value);
        }

        return ParsePrimary();
    }

    private AstNode ParsePrimary()
    {
        var token = CurrentToken;

        return token.Type switch
        {
            TokenType.Number => ParseLiteral(token, token.Value),
            TokenType.String => ParseLiteral(token, token.Value),
            TokenType.Boolean => ParseLiteral(token, token.Value),
            TokenType.Error => ParseLiteral(token, token.Value),
            TokenType.CellRef => ParseReference(token),
            TokenType.RangeRef => ParseRange(token),
            TokenType.NamedRange => ParseNamedRange(token),
            TokenType.Function => ParseFunction(token),
            TokenType.LeftParen => ParseParenthesized(),
            TokenType.EndOfInput => new LiteralNode(token, ErrorValue.Null),
            _ => ParseError(token)
        };
    }

    private LiteralNode ParseLiteral(Token token, object? value)
    {
        _position++;
        return new LiteralNode(token, value);
    }

    private ReferenceNode ParseReference(Token token)
    {
        _position++;
        return new ReferenceNode(token, (CellReference)token.Value!);
    }

    private RangeNode ParseRange(Token token)
    {
        _position++;
        return new RangeNode(token, (RangeReference)token.Value!);
    }

    private NamedRangeNode ParseNamedRange(Token token)
    {
        _position++;
        return new NamedRangeNode(token, (string)token.Value!);
    }

    private FunctionNode ParseFunction(Token token)
    {
        var name = (string)token.Value!;
        _position++; // Skip function name

        // Expect left paren
        Expect(TokenType.LeftParen);
        _position++; // Skip (

        var args = new List<AstNode>();
        if (CurrentToken.Type != TokenType.RightParen)
        {
            while (true)
            {
                args.Add(ParseExpression());
                if (CurrentToken.Type == TokenType.Comma || CurrentToken.Type == TokenType.Semicolon)
                {
                    _position++;
                    continue;
                }
                break;
            }
        }

        Expect(TokenType.RightParen);
        _position++; // Skip )

        return new FunctionNode(token, name, args);
    }

    private ParenNode ParseParenthesized()
    {
        _position++; // Skip (
        var expr = ParseExpression();
        Expect(TokenType.RightParen);
        _position++; // Skip )
        return new ParenNode(CurrentToken, expr);
    }

    private LiteralNode ParseError(Token token)
    {
        _position++;
        return new LiteralNode(token, ErrorValue.Unknown(token.Text));
    }

    private Token CurrentToken => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfInput, "", null, 0, 0);

    private void Expect(TokenType type)
    {
        if (CurrentToken.Type != type)
        {
            throw new FormulaParseException($"Expected {type}, got {CurrentToken.Type} at position {CurrentToken.Position}");
        }
    }

    private BinaryOperator? GetBinaryOperator(TokenType type)
    {
        return type switch
        {
            TokenType.Plus => BinaryOperator.Add,
            TokenType.Minus => BinaryOperator.Subtract,
            TokenType.Multiply => BinaryOperator.Multiply,
            TokenType.Divide => BinaryOperator.Divide,
            TokenType.Power => BinaryOperator.Power,
            TokenType.Concatenate => BinaryOperator.Concatenate,
            TokenType.Equal => BinaryOperator.Equal,
            TokenType.NotEqual => BinaryOperator.NotEqual,
            TokenType.LessThan => BinaryOperator.LessThan,
            TokenType.LessEqual => BinaryOperator.LessEqual,
            TokenType.GreaterThan => BinaryOperator.GreaterThan,
            TokenType.GreaterEqual => BinaryOperator.GreaterEqual,
            _ => null
        };
    }

    private UnaryOperator? GetUnaryOperator(TokenType type)
    {
        return type switch
        {
            TokenType.Plus => UnaryOperator.Plus,
            TokenType.Minus => UnaryOperator.Minus,
            TokenType.Percent => UnaryOperator.Percent,
            _ => null
        };
    }

    private int GetPrecedence(BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Power => 4,
            BinaryOperator.Multiply or BinaryOperator.Divide => 3,
            BinaryOperator.Add or BinaryOperator.Subtract => 2,
            BinaryOperator.Concatenate => 1,
            BinaryOperator.Equal or BinaryOperator.NotEqual or
            BinaryOperator.LessThan or BinaryOperator.LessEqual or
            BinaryOperator.GreaterThan or BinaryOperator.GreaterEqual => 0,
            _ => -1
        };
    }
}

/// <summary>
/// Exception for formula parsing errors.
/// </summary>
public sealed class FormulaParseException : Exception
{
    public FormulaParseException(string message) : base(message) { }
}