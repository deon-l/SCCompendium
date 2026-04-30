using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private delegate ReadOnlySpan<char> Command(ReadOnlySpan<char> remaining, Context context);
    private record struct Context(StringBuilder Result,
        Dictionary<string, Command> Commands)
    {

    }
}
