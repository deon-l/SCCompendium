namespace SCCompendium.Infrastructure.Parser.LatexParser;

public partial class LatexParser
{
    /// <summary>
    /// Defines a list of commands, ligatures, and replacements that a command defines (or other feature) defines.
    /// </summary>
    /// <param name="CommandList">Defined commands. Keys are the command names</param>
    /// <param name="Ligatures">Defined ligatures. Keys are the 2 chars which are converted into the corresponding value.</param>
    /// <param name="Replacements">Defined replacements. Keys are the char replaced with the corresponding value.</param>
    private record struct Typeset(
        Dictionary<string, CommandData>? CommandList = null,
        Dictionary<(char, char), string>? Ligatures = null,
        Dictionary<char, string>? Replacements = null)
    {


        /// <summary>
        /// Helper command for adding replacements to this typeset.
        /// Assumes replacements are all the same length, and
        /// <paramref name="output"/> length is a whole multiple of the length of <paramref name="input"/>.
        /// </summary>
        /// <returns>This instance, modified.</returns>
        public Typeset AddReplacements(string input, string output)
        {
            Replacements ??= new();
            Debug.Assert(output.Length % input.Length == 0);
            int charsPerReplace = output.Length / input.Length;
            for (int i = 0; i < input.Length; i++)
            {
                Replacements.Add(input[i], output.Substring(i * charsPerReplace, charsPerReplace));
            }

            return this;
        }

        /// <summary>
        /// Helper command for adding ligatures to this typeset.
        /// </summary>
        /// <param name="input">pairs of input chars. Must be an even length.</param>
        /// <param name="output">replaced ligatures. Assumes ligatures are all the same length.</param>
        /// <returns>This instance, modified.</returns>
        public Typeset AddLigatures(string input, string output)
        {
            Ligatures ??= new();
            Debug.Assert(input.Length % 2 == 0);
            Debug.Assert(output.Length % (input.Length / 2) == 0);
            int charsPerLigature = output.Length / (input.Length / 2);
            for (int i = 0; i < input.Length / 2; i++)
            {
                int inIndex = i * 2;
                int outIndex = i * charsPerLigature;
                Ligatures.Add((input[inIndex], input[inIndex + 1]), output.Substring(outIndex, charsPerLigature));
            }

            return this;
        }
    }
}
