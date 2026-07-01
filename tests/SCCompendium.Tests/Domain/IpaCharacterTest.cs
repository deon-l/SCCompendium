using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Tests.Domain;

public class IpaCharacterTest
{
    public partial class DataSource
    {
        public IEnumerable<Func<(string, string[])>> EqualIpaCharSource()
        {
            yield return () => ("a", Array.Empty<string>());
            yield return () => ("abc", Array.Empty<string>());
            yield return () => ("a", [":"]);
            yield return () => ("a", [":", "[+high]"]);
        }
    }
    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.EqualIpaCharSource))]
    public async Task Equals_EqualObjects_ReturnsTrue(string character, string[] diacritics)
    {
        IpaCharacter character1 = new(character, (string[])diacritics.Clone());
        IpaCharacter character2 = new((string)character.Clone(), (string[])diacritics.Clone());

        bool result = character1.Equals(character2);

        await Assert.That(result).IsTrue();
    }

    [Test]
    [MethodDataSource<DataSource>(nameof(DataSource.EqualIpaCharSource))]
    public async Task GetHashCode_EqualObjects_ReturnsTrue(string character, string[] diacritics)
    {
        IpaCharacter character1 = new(character, (string[])diacritics.Clone());
        IpaCharacter character2 = new((string)character.Clone(), (string[])diacritics.Clone());

        int hash1 = character1.GetHashCode();
        int hash2 = character2.GetHashCode();

        await Assert.That(hash1 == hash2).IsTrue();
    }

    [Test]
    public async Task Equals_DifferentCharacterEmptyDiacritics_ReturnsFalse()
    {
        IpaCharacter character1 = new("a", Array.Empty<string>());
        IpaCharacter character2 = new("z", Array.Empty<string>());

        bool result = character1.Equals(character2);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentDiacriticLength_ReturnsFalse()
    {
        IpaCharacter character1 = new("a", ["[+back]", "[+high]"]);
        IpaCharacter character2 = new("a", ["[+back]"]);

        bool result = character1.Equals(character2);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentDiacriticSameLength_ReturnsFalse()
    {
        IpaCharacter character1 = new("a", ["[+back]"]);
        IpaCharacter character2 = new("a", ["[+high]"]);

        bool result = character1.Equals(character2);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ToString_ExampleObject_ExpectedResult()
    {
        IpaCharacter character = new("a", [":", "[+high]"]);
        const string expected = "a:[+high]";

        string actual = character.ToString();

        await Assert.That(actual).IsEqualTo(expected);
    }
}
