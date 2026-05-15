using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    private class Context
    {
        private StringBuilder Source { get; } = new();
        private StringBuilder Result { get; }
        private readonly Stack<(int depth, Typeset typeset)> _typesets = new();

        public int GroupDepth { get; private set; } = 0;
        public int LengthSource => Source.Length;
        public int LengthResult => Result.Length;

        public Context(ReadOnlySpan<char> initialSource, StringBuilder result)
        {
            Source.Append(initialSource);
            Result = result;
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
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

            Source.EnsureCapacity(Source.Length + length);
            for (int i = 0; i < length; i++)
            {
                Source.Insert(0, Result[Result.Length - i - 1]);
            }
            Result.Remove(Result.Length - length, length);
        }

        public void AppendSource(char c) => Source.Insert(0, c);
        public void AppendResult(char c) => Result.Append(c);

        public void RemoveResult(int start, int length) => Result.Remove(start, length);
        public StringSlice SliceResult(int start, int length) => new StringSlice(Result, start, length);

        public void IncrementGroupDepth() => GroupDepth++;
        public void DecrementGroupDepth()
        {
            GroupDepth--;
            while (_typesets.Count > 0 && _typesets.Peek().depth > GroupDepth)
            {
                _typesets.Pop();
            }
        }
        public void AddTypeset(Typeset typeset) => _typesets.Push((GroupDepth, typeset));

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

        public bool TryGetReplacement(char c, out string replacement)
        {
            foreach (var (_, typeset) in _typesets)
            {
                if (typeset.Replacements?.TryGetValue(c, out replacement!) is true)
                {
                    return true;
                }
            }

            replacement = String.Empty;
            return false;
        }

        public bool TryGetLigature(char c1, char c2, out string ligature)
        {
            foreach (var (_, typeset) in _typesets)
            {
                if (typeset.Ligatures?.TryGetValue((c1, c2), out ligature!) is true)
                {
                    return true;
                }
            }

            ligature = String.Empty;
            return false;
        }
    }
}
