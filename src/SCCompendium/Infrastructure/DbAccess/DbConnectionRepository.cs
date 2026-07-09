using System.Data;
using SCCompendium.Application.DbAccess;
using MySqlConnector;

namespace SCCompendium.Infrastructure.DbAccess;

/// <summary>
/// Repo to hold <see cref="MySqlConnection"/>.
/// </summary>
public class DbConnectionRepository : IDbConnectionRepository
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void CreateConnection(string connectionString)
    {
        throw new NotImplementedException();
    }

    public T GetConnection<T>() where T : IDbConnection
    {
        throw new NotImplementedException();
    }
}
