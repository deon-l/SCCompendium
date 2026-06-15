using System.Text;
using SCCompendium.Application.Parser;

namespace SCCompendium.Tests.Infrastructure.Parser;

/// <summary>
/// Abstract class to make mocking of <see cref="ILatexParser"/> easier
/// by providing a mockable version of <see cref="ILatexParser.ParseLatexSegment(ReadOnlySpan{char},StringBuilder)"/>
/// take takes a <see cref="string"/> instead, so callback can be done.
/// </summary>
public abstract class MockableILatexParser : ILatexParser
{
    public void ParseLatexSegment(ReadOnlySpan<char> segment, StringBuilder builder)
    {
        ParseLatexSegment(segment.ToString(), builder);
    }

    public abstract void ParseLatexSegment(string segment, StringBuilder builder);
}
