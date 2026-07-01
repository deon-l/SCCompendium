using System.Data.Common;
using Apps72.Dev.Data.DbMocker;
using Apps72.Dev.Data.DbMocker.Data;

namespace SCCompendium.Tests.Infrastructure.DbAccess.Helper;

/// <summary>
/// An extension of the <see cref="MockDbConnection"/> and related classes
/// to modify the functionality so that parameters are saved by reference rather than copied.
/// </summary>
/// <remarks>This is done because it seems that <c>MySqlConnector</c> saves params by reference as well.</remarks>
public class BetterMockDbConnection : MockDbConnection
{
    protected override DbCommand CreateDbCommand()
    {
        return new BetterMockDbCommand(this, (MockDbCommand)base.CreateDbCommand());
    }
}
