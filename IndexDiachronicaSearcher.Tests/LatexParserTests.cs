using System.Text;
using DiachronicaParserSearcher.Parser;

namespace IndexDiachronicaSearcher.Tests;

public class LatexParserTests
{
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
