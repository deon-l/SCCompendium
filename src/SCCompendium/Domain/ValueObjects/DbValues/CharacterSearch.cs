namespace SCCompendium.Tests.Domain.ValueObjects.DbValues;

public readonly record struct CharacterSearch()
{
    public static readonly CharacterSearch NonFilteringSearch = new();
    /// <summary>The character to search for.</summary>
    public string Character { get; init; } = String.Empty;
    /// <summary>The diacritics any characters found must have.</summary>
    public string[] Diacritics { get; init; } = [];
    /// <summary>Specific rule environment the character is found in.</summary>
    public CharacterEnvironment Environment { get; init; } = CharacterEnvironment.All;

    public bool Equals(CharacterSearch other)
        => Character == other.Character
           && Environment == other.Environment
           && Diacritics.Length == other.Diacritics.Length
           && Diacritics.All(other.Diacritics.Contains);

    public override int GetHashCode()
    {
        return HashCode.Combine(Character, Diacritics.Length, (int)Environment);
    }

    public override string ToString()
    {
        return
            $"CharacterSearch {{ Character = {Character}, Diacritics = [{String.Join(',',  Diacritics)}], Environment = {Environment} }}";
    }
}
