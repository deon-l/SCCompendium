using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Application.Parser;

/// <summary>
/// Creates <see cref="CharacterSearch"/>.
/// </summary>
public interface ICharacterSearchParser
{
    /// <summary>
    /// Creates a <see cref="CharacterSearch"/> using <paramref name="input"/>, or the <see cref="CharacterSearch"/>
    /// representing no filter if one can't be constructed (or <paramref name="input"/> is null/empty).
    /// </summary>
    public CharacterSearch GetCharacterSearch(string? input);
}
