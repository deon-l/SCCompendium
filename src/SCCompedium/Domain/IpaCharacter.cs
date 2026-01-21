namespace SCCompendium.Domain;

public readonly struct IpaCharacter
{
    public string Character { get; }
    public List<string> Diacritics { get; }

    public IpaCharacter(string character, List<string> diacritics)
    {
        Character = character;
        Diacritics = diacritics;
    }
}
