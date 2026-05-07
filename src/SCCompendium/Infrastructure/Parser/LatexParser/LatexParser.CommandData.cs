using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private record struct CommandData(
        int Arguments,
        Action<Context> Command,
        Typeset? Typeset = null);
}
