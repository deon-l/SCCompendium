using SCCompendium.Parser;

namespace SCCompendium.Tests;

public partial class DiachronicaParserTests
{
    [Test]
    public async Task ParseFile_EntireDiachronica_NonEmptyListings()
    {
        DiachronicaParser parser = new();
        StringReader reader = new(_sampleDiachronica);

        var result = parser.ParseDiachronica(reader);

        await Assert.That(result).IsNotEmpty();
    }
}
