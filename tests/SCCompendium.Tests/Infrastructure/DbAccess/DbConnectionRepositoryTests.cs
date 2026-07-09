using System.Data;
using System.Data.Common;
using SCCompendium.Domain.Exceptions;
using SCCompendium.Infrastructure.DbAccess;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

public class DbConnectionRepositoryTests
{
    [Test]
    public async Task CreateConnection_IsDisposed_ThrowsException()
    {
        DbConnectionRepository repo = new();
        repo.Dispose();

        Action action = () => repo.CreateConnection("");

        await Assert.That(action).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task GetConnection_IsDisposed_ThrowsException()
    {
        DbConnectionRepository repo = new();
        repo.Dispose();

        Action action = () => repo.GetConnection<DbConnection>();

        await Assert.That(action).Throws<ObjectDisposedException>();
    }

    private interface ISpecificDbConnection : IDbConnection;
    [Test]
    public async Task GetConnection_UnsupportedType_ThrowsException()
    {
        DbConnectionRepository repo = new();

        Action action = () => repo.GetConnection<ISpecificDbConnection>();

        await Assert.That(action).Throws<TypeNotSupportedException>();
    }

    [Test]
    public async Task GetConnection_ProperState_NoDisposedNotSupportedException()
    {
        using DbConnectionRepository repo = new();

        // Can't open actual connection
        // so check that a different exception was thrown.
        Exception? caught = null;
        try
        {
            repo.CreateConnection("");
            repo.GetConnection<DbConnection>();
        }
        catch (Exception e)
        {
            caught = e;
        }

        Debug.Assert(caught is not null);
        await Assert.That(caught).IsNotTypeOf<TypeNotSupportedException>().And
            .IsNotTypeOf<ObjectDisposedException>();
    }
}
