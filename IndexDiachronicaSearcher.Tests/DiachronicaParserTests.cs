using DiachronicaParserSearcher.Parser;

namespace IndexDiachronicaSearcher.Tests;

public partial class DiachronicaParserTests
{
    [Test]
    public void ParseFile_EntireDiachronica_NoExceptions()
    {
        DiachronicaParser parser = new();
        StringReader reader = new StringReader(_sampleDiachronica);

        _ = parser.ParseFile(reader);

        // No Assertions - test is no errors occur.
    }
}
