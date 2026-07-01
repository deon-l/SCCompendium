namespace SCCompendium.Tests.Domain.ValueObjects.DbValues;

public record struct CharacterSearch()
{
    /// <summary>The character to search for.</summary>
    public string Character { get; init; }
    /// <summary>The diacritics any characters found must have.</summary>
    public string[] Diacritics { get; init; }
    /// <summary>Specific rule environment the character is found in.</summary>
    public CharacterEnvironment Environment { get; init; } = CharacterEnvironment.All;
}
