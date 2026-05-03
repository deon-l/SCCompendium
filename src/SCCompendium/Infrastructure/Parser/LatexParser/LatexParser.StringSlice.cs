using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private struct StringSlice
    {
        private readonly StringBuilder _sb;
        public int Start { get; }
        public int Length { get; private set; }

        public char this[int i] => _sb[Start + i];

        public StringSlice(StringBuilder sb, int start, int length)
        {
            _sb = sb;
            Start = start;
            Length = length;
        }
        public override string ToString() => _sb.ToString(Start, Length);
    }
}
