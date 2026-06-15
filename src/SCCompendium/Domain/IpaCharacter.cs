namespace SCCompendium.Domain;

/// <summary>
/// Represents a specific Ipa sound and some diacritics it has.
/// </summary>
public readonly struct IpaCharacter : IEquatable<IpaCharacter>
{
    /// <summary>Symbol of the represented Ipa Sound.</summary>
    public string Character { get; }
    /// <summary>Array of diacritics for Ipa sound.</summary>
    /// <remarks>For equality purposes, this should be sorted (by ordinal).</remarks>
    public string[] Diacritics { get; }

    /// <summary>
    /// Creates an Ipa Character with the specified character and diacritics
    /// </summary>
    /// <remarks><paramref name="diacritics"/> <i>isn't</i> copied. It should also be sorted (by ordinal)</remarks>
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

    /// <summary>
    /// Returns the <see cref="string"/> representation of this instance,
    /// combining <see cref="Character"/> and <see cref="Diacritics"/>.
    /// </summary>
    public override string ToString()
    {
        return Character + String.Join("", Diacritics);
    }
}
