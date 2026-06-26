using Apps72.Dev.Data.DbMocker;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

internal class CommandCapturer
{
    private readonly List<MockCommand> _captured = new();
    public MockCommand this[int i] => _captured[i];
    public int Count => _captured.Count;
    public Func<MockCommand, bool> All => CaptureAll;

    private bool CaptureAll(MockCommand command)
    {
        _captured.Add(command);
        return true;
    }

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
