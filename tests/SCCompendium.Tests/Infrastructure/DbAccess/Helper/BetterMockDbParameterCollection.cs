using System.Collections;
using System.Data.Common;

namespace SCCompendium.Tests.Infrastructure.DbAccess.Helper;

/// <summary>
/// An extension of the <see cref="Apps72.Dev.Data.DbMocker.MockDbConnection"/> and related classes
/// to modify the functionality so that parameters are saved by reference rather than copied.
/// </summary>
/// <remarks>This is done because it seems that <c>MySqlConnector</c> saves params by reference as well.</remarks>
public class BetterMockDbParameterCollection : DbParameterCollection
{
    private readonly List <DbParameter> _parameters = new();

    public override int Add(object value)
    {
        _parameters.Add((DbParameter) value);
        return _parameters.Count - 1;
    }

    public override void Clear()
    {
        _parameters.Clear();
    }

    public override bool Contains(object value)
    {
        throw new NotImplementedException();
    }

    public override int IndexOf(object value)
    {
        throw new NotImplementedException();
    }

    public override void Insert(int index, object value)
    {
        throw new NotImplementedException();
    }

    public override void Remove(object value)
    {
        throw new NotImplementedException();
    }

    public override void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    public override void RemoveAt(string parameterName)
    {
        throw new NotImplementedException();
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        throw new NotImplementedException();
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        throw new NotImplementedException();
    }

    public override int Count => this._parameters.Count;
    public override object SyncRoot { get; } = new();

    public override int IndexOf(string parameterName)
    {
        throw new NotImplementedException();
    }

    public override bool Contains(string value)
    {
        throw new NotImplementedException();
    }

    public override void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public override IEnumerator GetEnumerator()
        => _parameters.GetEnumerator();

    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return _parameters.First(db => db.ParameterName == parameterName);
    }

    public override void AddRange(Array values)
    {
        _parameters.AddRange((DbParameter[])values);
    }
}
