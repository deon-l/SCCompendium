using System.Text;

namespace SCCompendium.Application.Parser;

public interface ILatexParser
{
    public string ParseLatexSegment(ReadOnlySpan<char> segment)
    {
        StringBuilder sb = new();
        ParseLatexSegment(segment, sb);
        return sb.ToString();
    }
    public void ParseLatexSegment(ReadOnlySpan<char> segment, StringBuilder builder);
}
