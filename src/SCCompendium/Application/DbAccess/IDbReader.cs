using SCCompendium.Domain.ValueObjects.Parsed;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Application.DbAccess;

/// <summary>
/// Class to read, search, and filter the values from the database uploaded by <see cref="IDbWriter"/>.
/// </summary>
public interface IDbReader
{
    /// <summary>
    /// Get and return a list of <see cref="PhonologicalRuleGroup"/> containing
    /// details such as names, credits, etc. but not any rules.
    /// </summary>
    public List<PhonologicalRuleGroup> GetAllGroupDetails(IDbConnectionRepository connectionRepo);
    /// <summary>
    /// Get all <see cref="IpaCharacter"/>'s listed in the database.
    /// </summary>
    public List<IpaCharacter> GetAllCharacterDetails(IDbConnectionRepository connectionRepo);

    /// <summary>
    /// Get and return a list of <see cref="PhonologicalRuleGroup"/> with all rules
    /// that contain the character specified by <paramref name="filter"/>.
    /// </summary>
    public List<PhonologicalRuleGroup>  FindRules(IDbConnectionRepository connectionRepo, CharacterSearch filter);
}
