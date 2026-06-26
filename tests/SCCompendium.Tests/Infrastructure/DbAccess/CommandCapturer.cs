using Apps72.Dev.Data.DbMocker;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

internal class CommandCapturer
{
    private readonly List<MockCommand> _captured = new();
    public MockCommand this[int i] => _captured[i];
    public int Count => _captured.Count;

    public MockCommand[] ToArray() => _captured.ToArray();
    /// <summary>
    /// Captures all commands, and allows all SQL commands.
    /// </summary>
    public Func<MockCommand, bool> Any => CaptureAll;

    private bool CaptureAll(MockCommand command)
    {
        _captured.Add(command);
        return true;
    }

    /// <summary>
    /// Allows only SQL commands that satisfy <paramref name="predicate"/>, and captures only if they do.
    /// </summary>
    public Func<MockCommand, bool> If(Func<MockCommand, bool> predicate)
    {
        return (command) =>
        {
            if (!predicate(command))
            {
                return false;
            }
            _captured.Add(command);
            return true;
        };
    }
}
