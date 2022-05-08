using System.Collections.Immutable;

namespace Broccolini.Parsing;

internal static class Parser
{
    public static IniDocument Parse(IParserInput input)
    {
        var nodes = ImmutableArray.CreateBuilder<IniNode>();

        while (input.Peek() is not Token.Epsilon)
        {
            nodes.Add(ParseNode(input));
        }

        return new IniDocument(nodes.ToImmutable());
    }

    private static IniNode ParseNode(IParserInput input)
    {
        var node = input switch
        {
            _ when IsSection(input) => ParseSection(input),
            _ when IsKeyValue(input) => ParseKeyValue(input),
            _ => new TriviaNode(new TriviaList(input.ReadWhile(t => t is not Token.LineBreak))),
        };

        return node with { LineBreak = input.ReadOrNone(t => t is Token.LineBreak) };
    }

    private static bool IsSection(IParserInput input)
        => input.PeekIgnoreWhitespace() is Token.OpeningBracket;

    private static bool IsKeyValue(IParserInput input)
    {
        for (var i = 0; input.Peek(i) is not Token.LineBreak and not Token.Epsilon; i++)
        {
            if (input.Peek(i) is Token.EqualsSign)
            {
                return true;
            }
        }

        return false;
    }

    private static IniNode ParseKeyValue(IParserInput input)
    {
        var leadingTrivia = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var key = input.ReadWhileExcludeTrailingWhitespace(static t => t is not Token.EqualsSign).ConcatToString();
        var triviaBeforeEqualsSign = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var equalsSign = input.Read();
        var triviaAfterEqualsSign = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var (openingQuote, value, closingQuote) = ParseQuotedValue(input);
        var trailingTrivia = input.ReadWhile(static t => t is not Token.LineBreak);
        return new KeyValueNode(key, value)
        {
            LeadingTrivia = new TriviaList(leadingTrivia),
            TriviaBeforeEqualsSign = new TriviaList(triviaBeforeEqualsSign),
            EqualsSign = equalsSign,
            TriviaAfterEqualsSign = new TriviaList(triviaAfterEqualsSign),
            OpeningQuote = openingQuote,
            ClosingQuote = closingQuote,
            TrailingTrivia = new TriviaList(trailingTrivia),
        };
    }

    private static (Option<Token>, string, Option<Token>) ParseQuotedValue(IParserInput input)
    {
        var openingQuote = input.ReadOrNone(static t => t is Token.SingleQuotes or Token.DoubleQuotes);
        var value = ParseValue(input).ConcatToString();
        var closingQuote = input.ReadOrNone(static t => t is Token.SingleQuotes or Token.DoubleQuotes);

        static string ToString(Option<Token> token) => token.Match(none: string.Empty, some: static t => t.ToString());

        return openingQuote == closingQuote
            ? (openingQuote, value, closingQuote)
            : (Option<Token>.None(), ToString(openingQuote) + value + ToString(closingQuote), Option<Token>.None());
    }

    private static IImmutableList<Token> ParseValue(IParserInput input)
    {
        var tokens = ImmutableArray.CreateBuilder<Token>();

        while (true)
        {
            if (input.PeekIgnoreWhitespace() is Token.LineBreak or Token.Epsilon
                || (input.Peek() is Token.DoubleQuotes or Token.SingleQuotes
                    && input.PeekIgnoreWhitespace(1) is Token.LineBreak or Token.Epsilon))
            {
                break;
            }

            tokens.Add(input.Read());
        }

        return tokens.ToImmutable();
    }

    private static IniNode ParseSection(IParserInput input)
    {
        var leadingTrivia = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var openingBracketToken = input.Read();
        var triviaAfterOpeningBracket = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var name = input.ReadWhileExcludeTrailingWhitespace(static t => t is not Token.ClosingBracket and not Token.LineBreak).ConcatToString();
        var triviaBeforeClosingBracket = input.ReadOrEmpty(static t => t is Token.WhiteSpace);
        var closingBracket = input.ReadOrNone(static t => t is Token.ClosingBracket);
        var trailingTrivia = input.ReadWhile(static t => t is not Token.LineBreak);
        var children = ParseSectionChildren(input);
        return new SectionNode(name, children)
        {
            LeadingTrivia = new TriviaList(leadingTrivia),
            OpeningBracket = openingBracketToken,
            TriviaAfterOpeningBracket = new TriviaList(triviaAfterOpeningBracket),
            TriviaBeforeClosingBracket = new TriviaList(triviaBeforeClosingBracket),
            ClosingBracket = closingBracket,
            TrailingTrivia = new TriviaList(trailingTrivia),
        };
    }

    private static IImmutableList<IniNode> ParseSectionChildren(IParserInput input)
    {
        var nodes = ImmutableArray.CreateBuilder<IniNode>();

        while (input.Peek() is not Token.Epsilon && !IsSection(input))
        {
            nodes.Add(ParseNode(input));
        }

        return nodes.ToImmutable();
    }
}
