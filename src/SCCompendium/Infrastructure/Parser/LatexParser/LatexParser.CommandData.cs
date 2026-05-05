using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private record struct CommandData(int Arguments, Action<Context, ReadOnlySpan<StringSlice>> Command);
}
