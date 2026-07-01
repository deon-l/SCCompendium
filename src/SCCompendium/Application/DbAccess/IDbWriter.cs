using SCCompendium.Domain.ValueObjects.Parsed;

namespace SCCompendium.Application.DbAccess;

/// <summary>
/// Class for values to Database.
/// </summary>
public interface IDbWriter
{
    /// <summary>
    /// Initiate the database with the structure required for storing data (e.g. create tables for relational DBs),
    /// if such structures don't already exist.
    /// </summary>
    public void InitiateDatabase(IDbConnectionRepository connectionRepo);

    /// <summary>
    /// Write the entirety of <paramref name="rules"/> into the database.
    /// </summary>
    public void Write(IDbConnectionRepository connectionRepo, List<PhonologicalRuleGroup> rules);
}
