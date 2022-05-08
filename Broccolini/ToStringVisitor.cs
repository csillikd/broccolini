using System.Text;

namespace Broccolini;

internal sealed class ToStringVisitor : IIniNodeVisitor
{
    private readonly StringBuilder _stringBuilder = new();

    public override string ToString() => _stringBuilder.ToString();

    public void Visit(IEnumerable<IniNode> nodes) => nodes.ForEach(n => n.Accept(this));

    public void Visit(KeyValueNode keyValueNode)
    {
        VisitTrivia(keyValueNode.LeadingTrivia);
        _stringBuilder.Append(keyValueNode.Key);
        VisitTrivia(keyValueNode.TriviaBeforeEqualsSign);
        _stringBuilder.Append(keyValueNode.EqualsSign);
        VisitTrivia(keyValueNode.TriviaAfterEqualsSign);
        VisitToken(keyValueNode.OpeningQuote);
        _stringBuilder.Append(keyValueNode.Value);
        VisitToken(keyValueNode.ClosingQuote);
        VisitTrivia(keyValueNode.TrailingTrivia);
        VisitToken(keyValueNode.LineBreak);
    }

    public void Visit(TriviaNode triviaNode)
    {
        VisitTrivia(triviaNode.Value);
        VisitToken(triviaNode.LineBreak);
    }

    public void Visit(SectionNode sectionNode)
    {
        VisitTrivia(sectionNode.LeadingTrivia);
        _stringBuilder.Append(sectionNode.OpeningBracket);
        VisitTrivia(sectionNode.TriviaAfterOpeningBracket);
        _stringBuilder.Append(sectionNode.Name);
        VisitTrivia(sectionNode.TriviaBeforeClosingBracket);
        VisitToken(sectionNode.ClosingBracket);
        VisitTrivia(sectionNode.TrailingTrivia);
        VisitToken(sectionNode.LineBreak);
        Visit(sectionNode.Children);
    }

    private void VisitTrivia(TriviaList trivia)
    {
        foreach (var token in trivia.Tokens)
        {
            _stringBuilder.Append(token);
        }
    }

    private void VisitToken(Option<Token> token)
    {
        token.AndThen(t => { _stringBuilder.Append(t); });
    }
}
