using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private struct StringSlice
    {
        private readonly StringBuilder _sb;
        private readonly int _start;
        public int Length { get; }

        public char this[int i] => _sb[_start + i];

        public StringSlice(StringBuilder sb, int start, int length)
        {
            _sb = sb;
            _start = start;
            Length = length;
        }
    }
}
