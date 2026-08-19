using System.Text;
using SCCompendium.Domain.Exceptions;

namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    /// <summary>
    /// Represents and manages the current state of parsing.
    /// </summary>
    private class Context
    {
        /// <summary>Chars to still parse.</summary>
        private readonly StringBuilder _source = new();
        /// <summary>Result from parsing.</summary>
        private readonly StringBuilder _result;
        /// <summary>Stack of typesets, and the group depth they're instantiated at.</summary>
        private readonly Stack<(int depth, Typeset typeset)> _typesets = new();

        /// <summary>Current depth in groups (i.e. how many groups have been entered and not exited).</summary>
        public int GroupDepth { get; private set; } = 0;
        /// <summary>Number of chars to parse.</summary>
        public int LengthSource => _source.Length;
        /// <summary>Number of chars in current result.</summary>
        public int LengthResult => _result.Length;

        /// <param name="initialSource">Used to initiate the source chars to parse</param>
        /// <param name="result">Used as the result <see cref="StringBuilder"/>, modified by this class.</param>
        public Context(ReadOnlySpan<char> initialSource, StringBuilder result)
        {
            _source.Append(initialSource);
            _result = result;
        }

        /// <summary>
        /// returns the char at <paramref name="index"/> in source sb, or the next char by default.
        /// </summary>
        /// <remarks>If the specified char doesn't exist, returns <c>'\0'</c></remarks>
        public char PeekSource(int index = 0)
        {
            if (index >= _source.Length)
            {
                return '\0';
            }

            return _source[index];
        }

        /// <summary>
        /// Remove and return the next char from the source sb.
        /// </summary>
        /// <remarks>Unlike <see cref="PeekSource"/>, throws an exception if the next char doesn't exist.</remarks>
        public char PopSource()
        {
            char c = _source[0];
            _source.Remove(0, 1);
            return c;
        }

        /// <summary>
        /// Remove the next char from source sb, append it to the result sb, and return it.
        /// </summary>
        /// <remarks>Unlike <see cref="PeekSource"/>, throws an exception if the next char doesn't exist.</remarks>
        public char ConsumeSource()
        {
            char c = PopSource();
            _result.Append(c);
            return c;
        }

        /// <summary>
        /// Removes the last <paramref name="length"/> chars from result sb, and append it to source sb.
        /// </summary>
        public void ConsumeResult(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

            _source.EnsureCapacity(_source.Length + length);
            for (int i = 0; i < length; i++)
            {
                _source.Insert(0, _result[_result.Length - i - 1]);
            }
            _result.Remove(_result.Length - length, length);
        }

        public void AppendSource(char c) => _source.Insert(0, c);
        public void AppendResult(char c) => _result.Append(c);
        public void AppendResult(string str) => _result.Append(str);

        public void RemoveResult(int start, int length) => _result.Remove(start, length);
        /// <summary>
        /// Create a <see cref="StringSlice"/> of the result sb, at the corresponding index/length.
        /// </summary>
        public StringSlice SliceResult(int start, int length) => new StringSlice(_result, start, length);

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

        /// <remarks><i>Doesn't</i> limit itself to commands at same depth.</remarks>
        /// <exception cref="LatexParsingException"><paramref name="commandName"/> wasn't found.</exception>
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

            throw CreateParseError($"command \\'{commandName}' is not defined at this point");
        }

        /// <remarks>Only finds replacements at current depth</remarks>
        public bool TryGetReplacement(char c, out string replacement)
        {
            foreach (var (depth, typeset) in _typesets)
            {
                if (depth < GroupDepth)
                {
                    break;
                }
                if (typeset.Replacements?.TryGetValue(c, out replacement!) is true)
                {
                    return true;
                }
            }

            replacement = String.Empty;
            return false;
        }

        /// <remarks>Only finds ligatures at current depth</remarks>
        public bool TryGetLigature(char c1, char c2, out string ligature)
        {
            foreach (var (depth, typeset) in _typesets)
            {
                if (depth < GroupDepth)
                {
                    break;
                }
                if (typeset.Ligatures?.TryGetValue((c1, c2), out ligature!) is true)
                {
                    return true;
                }
            }

            ligature = String.Empty;
            return false;
        }

        public LatexParsingException CreateParseError(string message)
        {
            return new LatexParsingException(_result.ToString() + _source.ToString(), message, _result.Length);
        }
    }
}
