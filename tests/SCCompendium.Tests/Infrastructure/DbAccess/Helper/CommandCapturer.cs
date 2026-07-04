using System.Text;
using Apps72.Dev.Data.DbMocker;

namespace SCCompendium.Tests.Infrastructure.DbAccess.Helper;

internal class CommandCapturer : List<CommandTrace>
{
    /// <summary>
    /// Captures all commands, and allows all SQL commands.
    /// </summary>
    public Func<MockCommand, bool> Any => CaptureAll;

    private bool CaptureAll(MockCommand command)
    {
        Add(new CommandTrace(command));
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
            Add(new CommandTrace(command));
            return true;
        };
    }

    /// <summary>
    /// Get string representation of all commands captured.
    /// </summary>
    public override string ToString()
        => String.Join("\n--- ---\n", this.Select(cmd => cmd.ToString())) + "\n";
}
