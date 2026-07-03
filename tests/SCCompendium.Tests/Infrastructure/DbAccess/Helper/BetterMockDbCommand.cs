using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Apps72.Dev.Data.DbMocker.Data;

namespace SCCompendium.Tests.Infrastructure.DbAccess.Helper;

/// <summary>
/// An extension of the <see cref="Apps72.Dev.Data.DbMocker.MockDbConnection"/> and related classes
/// to modify the functionality so that parameters are saved by reference rather than copied.
/// </summary>
/// <remarks>This is done because it seems that <c>MySqlConnector</c> saves params by reference as well.</remarks>
public class BetterMockDbCommand : DbCommand
{
    private readonly MockDbCommand _command;
    private readonly BetterMockDbParameterCollection _dbParameterCollection = new();

    internal BetterMockDbCommand(BetterMockDbConnection connection, MockDbCommand command)
    {
        _command = command;
        DbConnection = connection;
    }

    [AllowNull] public override string CommandText { get => _command.CommandText; set => _command.CommandText = value!; }
    public override int CommandTimeout { get => _command.CommandTimeout; set => _command.CommandTimeout = value; }
    public override CommandType CommandType { get => _command.CommandType; set => _command.CommandType = value; }
    public override UpdateRowSource UpdatedRowSource { get => _command.UpdatedRowSource; set => _command.UpdatedRowSource = value; }
    protected sealed override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => _dbParameterCollection;

    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get => _command.DesignTimeVisible; set => _command.DesignTimeVisible = value; }
    public override void Cancel()
    {
        _command.Cancel();
    }

    private void PrepExec()
    {
        _command.Parameters.Clear();
        foreach (var param in _dbParameterCollection)
        {
            _command.Parameters.Add(param);
        }
    }

    public override int ExecuteNonQuery()
    {
        PrepExec();
        return _command.ExecuteNonQuery();
    }

    public override object? ExecuteScalar()
    {
        PrepExec();
        return _command.ExecuteScalar();
    }

    public override void Prepare()
    {
        _command.Prepare();
    }


    protected override DbParameter CreateDbParameter()
    {
        return _command.CreateParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        PrepExec();
        return _command.ExecuteReader(behavior);
    }
}
