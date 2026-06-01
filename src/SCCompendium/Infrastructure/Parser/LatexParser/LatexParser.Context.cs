using OneOf;
using System.Text;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    /// <summary>
    /// Represents and manages the current state of parsing.
    /// </summary>
    private class Context
    {
        /// <summary>Chars to still parse.</summary>
        private StringBuilder Source { get; } = new();
        /// <summary>Result from parsing.</summary>
        private StringBuilder Result { get; }
        /// <summary>Stack of typesets, and the group depth they're instantiated at.</summary>
        private readonly Stack<(int depth, Typeset typeset)> _typesets = new();

        /// <summary>Current depth in groups (i.e. how many groups have been entered and not exited).</summary>
        public int GroupDepth { get; private set; } = 0;
        /// <summary>Number of chars to parse.</summary>
        public int LengthSource => Source.Length;
        /// <summary>Number of chars in current result.</summary>
        public int LengthResult => Result.Length;

        /// <param name="initialSource">Used to initiate the source chars to parse</param>
        /// <param name="result">Used as the result <see cref="StringBuilder"/>, modified by this class.</param>
        public Context(ReadOnlySpan<char> initialSource, StringBuilder result)
        {
            Source.Append(initialSource);
            Result = result;
        }

        /// <summary>
        /// returns the char at <paramref name="index"/> in source sb, or the next char by default.
        /// </summary>
        /// <remarks>If the specified char doesn't exist, returns <c>'\0'</c></remarks>
        public char PeekSource(int index = 0)
        {
            if (index >= Source.Length)
            {
                return '\0';
            }

            return Source[index];
        }

        /// <summary>
        /// Remove and return the next char from the source sb.
        /// </summary>
        /// <remarks>Unlike <see cref="PeekSource"/>, throws an exception if the next char doesn't exist.</remarks>
        public char PopSource()
        {
            char c = Source[0];
            Source.Remove(0, 1);
            return c;
        }

        /// <summary>
        /// Remove the next char from source sb, append it to the result sb, and return it.
        /// </summary>
        /// <remarks>Unlike <see cref="PeekSource"/>, throws an exception if the next char doesn't exist.</remarks>
        public char ConsumeSource()
        {
            char c = PopSource();
            Result.Append(c);
            return c;
        }

        /// <summary>
        /// Removes the last <paramref name="length"/> chars from result sb, and append it to source sb.
        /// </summary>
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
        /// <summary>
        /// Create a <see cref="StringSlice"/> of the result sb, at the corresponding index/length.
        /// </summary>
        public StringSlice SliceResult(int start, int length) => new StringSlice(Result, start, length);

        public void IncrementGroupDepth() => GroupDepth++;
        /// <remarks>Automatically removes typesets that were at the now left depth.</remarks>
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
