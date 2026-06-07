namespace SCCompendium.Domain;

public readonly struct IpaCharacter : IEquatable<IpaCharacter>
{
    public string Character { get; }
    public string[] Diacritics { get; }

    public IpaCharacter(string character, string[] diacritics)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(diacritics);
        for (int i = 0; i < diacritics.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(diacritics[i]);
            Debug.Assert(i == 0 || diacritics[i - 1].CompareTo(diacritics[i], StringComparison.Ordinal) <= 0,
                "param 'diacritics' should be is sorted");
        }
        Character = character;
        Diacritics = diacritics;
    }

    public bool Equals(IpaCharacter other)
    {
        if (Character != other.Character)
        {
            return false;
        }
        if (other.Diacritics.Length != Diacritics.Length)
        {
            return false;
        }
        for (int i = 0; i < Diacritics.Length; i++)
        {
            if (Diacritics[i] != other.Diacritics[i])
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is IpaCharacter other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Character);
        foreach (string diacritic in Diacritics)
        {
            hash.Add(diacritic);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(IpaCharacter left, IpaCharacter right) => left.Equals(right);
    public static bool operator !=(IpaCharacter left, IpaCharacter right) => !(left == right);
}
