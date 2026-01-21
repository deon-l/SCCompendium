namespace SCCompendium.Domain;

public readonly struct IpaCharacter
{
    public readonly string Character { get; }
    public readonly string[] Diacritics { get; }

    public IpaCharacter(string character, string[] diacritics)
    {
        Character = character;
        Diacritics = diacritics;
    }
}
