using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private class Context
    {
        private StringBuilder Source { get; } = new();
        private StringBuilder Result { get; } = new();
        public required Dictionary<string, CommandData> CommandList { get; set; }

        public int LengthSource => Source.Length;
        public int LengthResult => Result.Length;

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

        public void ConsumeResult(int length)
        {
            Source.EnsureCapacity(Source.Length + length);
            Source.Append(Result, Result.Length - 1, length);
            Result.Remove(Result.Length - 1, length);
        }

        public void AppendResult(char c) => Result.Append(c);

        public void RemoveResult(int start, int length) => Result.Remove(start, length);
        public StringSlice SliceResult(int start, int length) => new StringSlice(Result, start, length);

        public CommandData GetCommand(string commandName) => CommandList[commandName];
    }
}
