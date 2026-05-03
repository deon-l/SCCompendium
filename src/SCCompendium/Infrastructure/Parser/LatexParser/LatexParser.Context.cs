using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private class Context
    {
        public StringBuilder Source { get; } = new();
        public StringBuilder Result { get; } = new();
        public required Dictionary<string, CommandData> CommandList { get; set; }

        public Context(ReadOnlySpan<char> initialSource)
        {
            Source.Append(initialSource);
        }

        public char PeekSource(int index = 0)
        {
            if (index >= Source.Length)
            {
                return '\0';
            }

            return Source[index];
        }

        public char PopSource()
        {
            char c = Source[0];
            Source.Remove(0, 1);
            return c;
        }

        public char ConsumeSource()
        {
            char c = PopSource();
            Result.Append(c);
            return c;
        }
    }
}
