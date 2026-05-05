using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private class Context
    {
        private int _groupDepth = 0;
        private StringBuilder Source { get; } = new();
        private StringBuilder Result { get; } = new();
        private readonly Stack<(int depth, Typeset typeset)> _typesets = new();

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

        public void IncrementGroupDepth() => _groupDepth++;
        public void DecrementGroupDepth()
        {
            _groupDepth--;
            while (_typesets.Count > 0 && _typesets.Peek().depth > _groupDepth)
            {
                _typesets.Pop();
            }
        }
        public void AddTypeset(Typeset typeset) => _typesets.Push((_groupDepth, typeset));

        public CommandData GetCommand(string commandName)
        {
            foreach (var (_, typeset) in _typesets)
            {
                if (typeset.CommandList is not null &&
                    typeset.CommandList.TryGetValue(commandName, out CommandData data))
                {
                    return data;
                }
            }

            throw new InvalidOperationException($"command \\'{commandName}' is not defined at this point");
        }
    }
}
