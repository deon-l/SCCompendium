using Apps72.Dev.Data.DbMocker;

namespace SCCompendium.Tests.Infrastructure.DbAccess;

/// <summary>
/// Provides extension methods to <see cref="MockCommand"/>, to aid with testing.
/// </summary>
public static class MockCommandExtensions
{
    extension(MockCommand cmd)
    {
        /// <summary>
        /// Checks that the command text starts with specified symbols (delimited by spaces).
        /// Use <see langword="null"/> to skip a symbol.
        /// </summary>
        public bool CommandTextStartsWith(string?[] expectedSymbols)
        {
            var commandSymbols = cmd.CommandText.Split();
            for (int i = 0; i < expectedSymbols.Length; i++)
            {
                if (expectedSymbols[i] is not null && !expectedSymbols[i]!
                        .Equals(commandSymbols[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks that the symbol at index <paramref name="i"/> contains symbol (not case-sensative)
        /// Returns <see langword="false"/> if command text is too short.
        /// </summary>
        public bool SymbolAtContains(int i, string symbol)
        {
            var commandSymbols = cmd.CommandText.Split();
            if (commandSymbols.Length < i)
            {
                return false;
            }
            return commandSymbols[i].Contains(symbol, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
