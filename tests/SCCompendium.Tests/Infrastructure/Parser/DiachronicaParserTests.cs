using SCCompendium.Infrastructure.Parser;
using SCCompendium.Infrastructure.Parser.LatexParser;

namespace SCCompendium.Tests.Infrastructure.Parser;

public partial class DiachronicaParserTests
{
    [Test]
    [Skip("Unreasonable to use at this point, when other dependant classes are incomplete.")]
    public async Task ParseFile_EntireDiachronica_NonEmptyListings()
    {
        DiachronicaParser parser = new(new LatexParser(), new PhonologicalRuleParser(new LatexParser()));
        StringReader reader = new(_sampleDiachronica);

        var result = parser.ParseDiachronica(reader);

        await Assert.That(result).IsNotEmpty();
    }
}
