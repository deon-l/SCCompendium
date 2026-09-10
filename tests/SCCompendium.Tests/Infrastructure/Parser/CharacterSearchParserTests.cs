using CommandDotNet.TestTools;
using SCCompendium.Infrastructure.Parser;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Tests.Infrastructure.Parser;

public partial class CharacterSearchParserTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("        ")]
    [Arguments("\t \n\t")]
    public async Task GetCharacterSearch_NullEmptyInputs_ReturnsNonFilterSearch(string? input)
    {
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(CharacterSearch.NonFilteringSearch);
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.SampleCharacterSearchCases))]
    public async Task GetCharacterSearch_SampleCharacterInput_ReturnsExpectedSearch(string input, CharacterSearch expected)
    {
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.SampleDiacriticSearchCases))]
    public async Task GetCharacterSearch_SampleDiacriticInput_ReturnsExpectedSearch(string input, CharacterSearch expected)
    {
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.SampleEnvironmentSearchCases))]
    public async Task GetCharacterSearch_SampleEnvironmentInput_ReturnsExpectedSearch(string input, CharacterSearch expected)
    {
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GetCharacterSearch_MixDiacriticEnvironment_ReturnsExpectedSearch()
    {
        const string input = "[+prop1][+input][+prop2][+output]";
        CharacterSearch expectedSearch = new CharacterSearch {Diacritics = ["prop1", "prop2"], Environment = CharacterEnvironment.Input | CharacterEnvironment.Output};
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expectedSearch);
    }

    [Test]
    public async Task GetCharacterSearch_MixAllComponents_ReturnsExpectedSearch()
    {
        const string input = "[non-prop][+prop1][+input]ʰ[+output]";
        CharacterSearch expectedSearch = new CharacterSearch { Character = "[non-prop]", Diacritics = ["prop1", "ʰ"], Environment = CharacterEnvironment.Input | CharacterEnvironment.Output};
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expectedSearch);
    }

    [Test]
    public async Task GetCharacterSearch_StartingWhiteSpace_IgnoresWhitespaceReturnsExpected()
    {
        const string input = "  \t\n   \t[non-prop][+prop1][+input]ʰ[+output]";
        CharacterSearch expectedSearch = new CharacterSearch { Character = "[non-prop]", Diacritics = ["prop1", "ʰ"], Environment = CharacterEnvironment.Input | CharacterEnvironment.Output};
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expectedSearch);
    }

    [Test]
    public async Task GetCharacterSearch_IntermediateWhitespace_IgnoresWhitespaceReturnsExpected()
    {
        const string input = "[non-prop] [+prop1]\t[+input]   ʰ\n[+output]";
        CharacterSearch expectedSearch = new CharacterSearch { Character = "[non-prop]", Diacritics = ["prop1", "ʰ"], Environment = CharacterEnvironment.Input | CharacterEnvironment.Output};
        TestConsole console = new();
        CharacterSearchParser parser = new(console);

        CharacterSearch result = parser.GetCharacterSearch(input);

        await Assert.That(result).IsEqualTo(expectedSearch);
    }
}
