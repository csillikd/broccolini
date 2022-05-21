using Broccolini.Syntax;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using static Broccolini.IniParser;
using static Broccolini.Test.TestData;

namespace Broccolini.Test;

public sealed class ParserTest
{
    [Theory]
    [MemberData(nameof(GetCommentsData))]
    public void ParsesCommentNode(string input, string leadingNode)
    {
        var document = Parse(leadingNode + input);
        var node = Assert.IsType<CommentNode>(GetLastNode(document));
        Assert.Equal(input, node.ToString());
    }

    [Property]
    public Property ParsesArbitraryComment(string commentValue)
    {
        var input = $"; {commentValue}";
        var document = Parse(input);
        return (document.NodesOutsideSection.Count == 1
                && document.NodesOutsideSection.First() is CommentNode triviaNode
                && triviaNode.ToString() == input)
            .ToProperty()
            .When(!input.Contains('\r') && !input.Contains('\n'));
    }

    public static TheoryData<string, string> GetCommentsData()
        => CommentNodes.Select(c => (c, string.Empty))
            .Concat(CommentNodes.SelectMany(_ => LeadingNodes, ValueTuple.Create))
            .ToTheoryData();

    [Theory]
    [MemberData(nameof(GetGarbageData))]
    public void ParsesGarbageAsUnrecognized(string input, string leadingNode)
    {
        var document = Parse(leadingNode + input);
        var node = Assert.IsType<UnrecognizedNode>(GetLastNode(document));
        Assert.Equal(input, node.ToString());
    }

    public static TheoryData<string, string> GetGarbageData()
        => GarbageNodes.Select(c => (c, string.Empty))
            .Concat(GarbageNodes.SelectMany(_ => LeadingNodes, ValueTuple.Create))
            .ToTheoryData();

    [Theory]
    [MemberData(nameof(GetSectionNameData))]
    public void ParsesSectionNames(string name, string input)
    {
        var document = Parse(input);
        var node = Assert.IsType<SectionNode>(document.Sections.Last());
        Assert.Equal(name, node.Name);
    }

    public static TheoryData<string, string> GetSectionNameData()
        => SectionsWithNames.Select(s => (s.Name, s.Input)).ToTheoryData();

    [Theory]
    [MemberData(nameof(GetKeyValuePairData))]
    public void ParsesKeyValuePair(string key, string value, string input)
    {
        var document = Parse(input);
        var node = Assert.IsType<KeyValueNode>(document.NodesOutsideSection.Last());
        Assert.Equal(key, node.Key);
        Assert.Equal(value, node.Value);
    }

    public static TheoryData<string, string, string> GetKeyValuePairData()
        => KeyValuePairsWithKeyAndValue.Select(s => (s.Key, s.Value, s.Input)).ToTheoryData();

    private static IniNode GetLastNode(IniDocument document)
        => GetLastNode(document.GetNodes());

    private static IniNode GetLastNode(IEnumerable<IniNode> nodes)
        => nodes.Last() switch
        {
            SectionNode sectionNode => GetLastNode(sectionNode),
            var node => node,
        };

    private static IniNode GetLastNode(SectionNode sectionNode)
        => sectionNode.Children.Count > 0
            ? GetLastNode(sectionNode.Children)
            : sectionNode;
}
