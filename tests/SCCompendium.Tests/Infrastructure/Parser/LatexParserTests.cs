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
    [Arguments("_0", "₀")]
    [Arguments("^1", "¹")]
    [Arguments(@"\Omega", "Ω")]
    public async Task ParseLatexMathSegment_SingleCommands_ExpectedOutputs(string inputSegment, string expectedOutput)
    {
        LatexParser parser = new();
        StringBuilder sb = new();

        parser.ParseLatexMathSegment(inputSegment, sb);
        string actualOutput = sb.ToString();

        await Assert.That(actualOutput).IsEqualTo(expectedOutput);
    }
}
