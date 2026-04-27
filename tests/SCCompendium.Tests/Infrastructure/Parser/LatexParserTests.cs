using System.Text;
using SCCompendium.Infrastructure.Parser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public class LatexParserTests
{
    public class DataSource
    {
        public const string SimpleTipaInput  = ":;0123456789@ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
        public const string SimpleTipaOutput = "ː\u02D1ˈʉɨʌɜɥɐɒɤɵɘəɑβɕðɛɸɣɦɪʝʁʎɱŋɔʕɾʃθʊʋɯχʏʒ|";
        public static IEnumerable<(char, char)> TipaSourceToOutput()
        {
            Debug.Assert(SimpleTipaInput.Length == SimpleTipaOutput.Length);
            for (int i = 0; i < SimpleTipaInput.Length; i++)
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
    public async Task ParseLatexTipaSegment_1CharInputs_ExpectedOutputs(char inputSegment, char outputSegment)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexTipaSegment(new(ref inputSegment), sb);

        await Assert.That(sb.Length).IsEqualTo(1);
        await Assert.That(sb[0]).IsEqualTo(outputSegment);
    }

    [Test]
    public async Task ParseLatexTipaSegment_Ligatures_ExpectedOutput()
    {
        const string input = "\" \"\" | ||";
        const string expectedOutput = "ˈ ˌ | ‖";
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexTipaSegment(input, sb);

        await Assert.That(sb.ToString()).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments(@"\*f \*k \*r \*t \*w", "ⅎ ʞ ɹ ʇ ʍ")]
    // Note: "\*f" translation doesn't follow TIPA specs,
    //  but couldn't find similar character (that isn't also result of "\*j")
    [Arguments(@"\*j \*n \*h \*l \*z", "ɟ ɲ ħ ɬ ɮ")]
    [Arguments(@"\*A \*B \*C \*1 \*2 \*3", "A B C 1 2 3")]
    [Arguments(@"\*; \*: \*@ \*\# \*\$ \*\& \*\% \*\{ \*\}", "; : @ # $ & % { }")]
    [Arguments(@"\*{123}", "123")]
    public async Task ParseLatexTipaSegment_AsteriskMacros_ExpectedOutput(string input, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexTipaSegment(input, sb);

        await Assert.That(sb.ToString()).IsEqualTo(expectedOutput);
    }

    [Test]
    public async Task ParseLatexMathSegment_EmptyInput_NoModification()
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexMathSegment(String.Empty, sb);

        await Assert.That(sb).IsEmpty();
    }

    [Test]
    [Arguments("_0", "₀")]
    [Arguments("^1", "¹")]
    [Arguments(@"\Omega", "Ω")]
    [Arguments(@"\langle", "⟨")]
    [Arguments(@"\rangle", "⟩")]
    public async Task ParseLatexMathSegment_SingleCommands_ExpectedOutputs(string inputSegment, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexMathSegment(inputSegment, sb);
        string actualOutput = sb.ToString();

        await Assert.That(actualOutput).IsEqualTo(expectedOutput);
    }

    [Test]
    [Arguments("aaaaa")]
    [Arguments("\\bbbbb")]
    [Arguments("\\")]
    public async Task ParseLatexMathSegment_InvalidInput_ThrowsException(string invalidInput)
    {
        LatexParser parser = new();

        void ErrorAction() => parser.ParseLatexMathSegment(invalidInput, new());

        await Assert.That(ErrorAction).ThrowsExactly<ArgumentException>();
    }
}
