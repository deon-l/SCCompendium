using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Application.Parser;

/// <summary>
/// Creates <see cref="CharacterSearch"/>es from a given input.
/// </summary>
public interface ICharacterSearchParser
{
    /// <summary>
    /// Creates a <see cref="CharacterSearch"/> using <paramref name="input"/>, or the <see cref="CharacterSearch"/>
    /// representing no filter if one can't be constructed (or <paramref name="input"/> is null/empty).
    /// </summary>
    /// <remarks>
    /// Environment filters are specified via [properties].
    /// If not specified, defaults to <see cref="CharacterEnvironment.All"/>.
    /// <br/><br/>for example:
    /// <c>V[+output]</c> searches only in output areas,
    /// <c>V[-output]</c> searches everywhere but output areas.
    /// </remarks>
    public CharacterSearch GetCharacterSearch(string? input);
}
