using Broccolini.Syntax;
using System.Collections.Immutable;

namespace Broccolini.Editing;

internal static class NewLineExtensions
{
    public static IniDocument EnsureTrailingNewLine(this IniDocument document, Token.NewLine newLine)
        => document switch
        {
            { Sections: { Count: >=1 } sections } => document with { Sections = document.Sections.ReplaceLast(n => EnsureTrailingNewLine(n, newLine)) },
            { NodesOutsideSection: { Count: >= 1 } sections } => document with { NodesOutsideSection = document.NodesOutsideSection.ReplaceLast(n => EnsureTrailingNewLine(n, newLine)) },
            _ => document,
        };

    public static IniNode EnsureTrailingNewLine(this IniNode node, Token.NewLine newLine)
        => node switch
        {
            SectionNode sectionNode => EnsureTrailingNewLine(sectionNode, newLine),
            SectionChildNode sectionChildNode => EnsureTrailingNewLine(sectionChildNode, newLine),
            _ => throw new InvalidOperationException("Unreachable"),
        };

    public static SectionChildNode EnsureTrailingNewLine(this SectionChildNode node, Token.NewLine newLine)
        => node.NewLine is null
            ? node with { NewLine = newLine }
            : node;

    public static SectionNode EnsureTrailingNewLine(this SectionNode node, Token.NewLine newLine)
        => node switch
        {
            { Children: { Count: >=1 } children } => node with { Children = children.ReplaceLast(n => EnsureTrailingNewLine(n, newLine)) },
            { Children.Count: 0, NewLine: null } => node with { NewLine = newLine },
            _ => node,
        };

    private static IImmutableList<T> ReplaceLast<T>(this IImmutableList<T> list, Func<T, T> update)
        => list.SetItem(list.Count - 1, update(list[list.Count - 1]));
}
