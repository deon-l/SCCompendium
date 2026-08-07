using System.Text;
using SCCompendium.Domain.Exceptions;
using SCCompendium.Infrastructure.Parser.LatexParser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public class LatexParserTests
{
    [Test]
    public async Task ParseLatexSegment_EscapedChars_GetCharsWithoutBackslash()
    {
        const string input = @"\#\$\&\%\{\}\ ";
        const string expectedOutput = "#$&%{} ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_WhitespaceChars_GetOnlySpacesTabs()
    {
        string input = new String(' ', 2) + "\n\n\t\r" + ' ';
        const string expectedOutput = "  \t ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_BasicNoParamCommands_GetParsedOutput()
    {
        const string input = @"\change\textrightarrow";
        const string expectedOutput = "→→";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_GroupCommandBF_ResetsOutsideGroup()
    {
        const string input = @"{\bf abc}def";
        const string expectedOutput = "𝐚𝐛𝐜def";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexSegment_SourceLigatures_GetLigatures()
    {
        const string input = @"``''-- ---";
        const string expectedOutput = "“”– —";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    public class DataSource
    {
        public const string SimpleTipaInput  = ":;\"0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
        public const string SimpleTipaOutput = "ːˑˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʔʕɾʃθʊʋɯχʏʒ|";
        public static IEnumerable<(char, char)> TipaSourceToOutput()
        {
            Debug.Assert(SimpleTipaInput.Length == SimpleTipaOutput.Length, $"{SimpleTipaInput.Length} != {SimpleTipaOutput.Length}");
            for (int i = 0; i < SimpleTipaOutput.Length; i++)
            {
                yield return (SimpleTipaInput[i], SimpleTipaOutput[i]);
            }
        }
    }

    [Test]
    public async Task ParseLatexSegment_AllSimpleTipaInput_ExpectedSimpleTipaOutput()
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(@$"\ipa{{{DataSource.SimpleTipaInput}}}", sb);

        await Assert.That(sb.ToString()).IsEqualTo(DataSource.SimpleTipaOutput);
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.TipaSourceToOutput))]
    public async Task ParseLatex_Tipa1ReplacedChar_GetReplacementChar(char inputSegment, char outputSegment)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(@$"\ipa{{{inputSegment}}}");

        await Assert.That(result).IsEqualTo(outputSegment.ToString());
    }

    [Test]
    public async Task ParseLatex_TipaLigaturesSource_GetLigatures()
    {
        const string input = "\"\" ||";
        const string expectedOutput = "ˌ ‖";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(@$"\ipa{{{input}}}");

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\*f \*k \*r \*t \*w", "ⅎ ʞ ɹ ʇ ʍ")]
    // Note: "\*f" translation doesn't follow TIPA specs,
    //  but couldn't find similar character (that isn't also result of "\*j")
    [Arguments(@"\*j \*n \*h \*l \*z", "ɟ ɲ ħ ɬ ɮ")]
    [Arguments(@"\*A \*B \*C \*1 \*2 \*3", "A B C 1 2 3")]
    [Arguments(@"\*; \*: \*@ \*\# \*\$ \*\& \*\% \*\{ \*\}", "; : @ # $ & % { }")]
    [Arguments(@"\*{123}", "123")]
    public async Task ParseLatex_TipaAsteriskMacros_ExpectedOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(@$"\ipa{{{input}}}", sb);

        await Assert.That(sb.ToString()).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_TipaCommandSuper_ReturnsExpectedOutput()
    {
        const string input = @"\ipa{\super{hlmnjwxyHMNPQW}}";
        const string expectedOutput = "ʰˡᵐⁿʲʷˣʸʱᶬᵑˀˤᵚ";
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatex_EmptyMathInput_NoModification()
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment("$$", sb);

        await Assert.That(sb).IsEmpty();
    }

    [Test]
    [Arguments("$_0$", "₀")]
    [Arguments("$^1$", "¹")]
    [Arguments(@"$\Omega$", "Ω")]
    [Arguments(@"$\langle$", "⟨")]
    [Arguments(@"$\rangle$", "⟩")]
    public async Task ParseLatex_MathSingleCommands_ExpectedOutputs(string inputSegment, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexSegment(inputSegment, sb);
        string actualOutput = sb.ToString();

        await Assert.That(actualOutput).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\bbbbb")]
    [Arguments(@"\")]
    public async Task ParseLatex_InvalidInput_ThrowsException(string invalidInput)
    {
        LatexParser parser = new();

        void ErrorAction() => parser.ParseLatexSegment(invalidInput, new());

        await Assert.That(ErrorAction).ThrowsExactly<LatexParsingException>().WithMessageContaining(invalidInput);
    }

    [Test]
    [Arguments(@"\tab")]
    [Arguments(@"\ipa{\tab}")]
    public async Task ParseLatex_TabInput_ProducesSingleTab(string input)
    {
        LatexParser parser = new();

        string result = parser.ParseLatexSegment(input);

        await Assert.That(result).IsEqualTo("\t");
    }
}
