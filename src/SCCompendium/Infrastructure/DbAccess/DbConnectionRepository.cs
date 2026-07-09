using System.Data;
using SCCompendium.Application.DbAccess;
using MySqlConnector;
using SCCompendium.Domain.Exceptions;

namespace SCCompendium.Infrastructure.DbAccess;

/// <summary>
/// Repo to hold <see cref="MySqlConnection"/>.
/// </summary>
public class DbConnectionRepository : IDbConnectionRepository
{
    private readonly MySqlConnection _connection = new();
    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _connection.Dispose();
        }

        _disposed = true;
    }

    ~DbConnectionRepository()
    {
        Dispose(false);
    }

    public void CreateConnection(string connectionString)
    {
        _connection.ConnectionString = connectionString;
    }

    public T GetConnection<T>() where T : IDbConnection
    {
        T value = TypeNotSupportedException.CastOrThrowIfCantCast<T>(_connection);
        if (_connection.State == ConnectionState.Closed)
        {
            _connection.Open();
        }

        return value;
    }
}
