using DiachronicaParserSearcher.Parser;

namespace IndexDiachronicaSearcher.Tests;

public partial class DiachronicaParserTests
{
    [Test]
    public async Task ParseFile_EntireDiachronica_NonEmptyListings()
    {
        DiachronicaParser parser = new();
        StringReader reader = new StringReader(_sampleDiachronica);

        var result = parser.ParseFile(reader);

        await Assert.That(result).IsNotEmpty();
    }
}
