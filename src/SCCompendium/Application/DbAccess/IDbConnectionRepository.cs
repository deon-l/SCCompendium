using System.Data;
using SCCompendium.Domain.Exceptions;

namespace SCCompendium.Application.DbAccess;

/// <summary>
/// Used to generate a <see cref="IDbConnection"/> from a connection string,
/// and to be passed to other interfaces so that they can get said connection.
/// </summary>
public interface IDbConnectionRepository : IDisposable
{
    /// <summary>
    /// Use <paramref name="connectionString"/> to create an <see cref="IDbConnection"/>.
    /// </summary>
    public void CreateConnection(string connectionString);

    /// <summary>
    /// Get an <see cref="IDbConnection"/> instance, or a specified subclass of it.
    /// </summary>
    /// <remarks>
    /// It is the responsibility of the implementing type to hold onto and eventually dispose of the returned instance,
    /// possibly via <see cref="IDbConnectionRepository.Dispose"/>
    /// </remarks>
    /// <exception cref="TypeNotSupportedException">Cannot cast connection</exception>
    public T GetConnection<T>() where T : IDbConnection;
}
