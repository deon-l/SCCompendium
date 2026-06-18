using System.Data;
using SCCompendium.Domain;

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
    public void InitiateDatabase(IDbConnection connection);

    /// <summary>
    /// Write the entirety of <paramref name="rules"/> into the database.
    /// </summary>
    public void Write(IDbConnection connection, List<PhonologicalRuleGroup> rules);
}
