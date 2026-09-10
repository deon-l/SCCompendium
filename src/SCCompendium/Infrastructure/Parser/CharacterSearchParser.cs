using System.Globalization;
using SCCompendium.Application.Parser;
using SCCompendium.Tests.Domain.ValueObjects.DbValues;

namespace SCCompendium.Infrastructure.Parser;

/// <inheritdoc cref="ICharacterSearchParser"/>
public class CharacterSearchParser(IConsoleIO console) : ICharacterSearchParser
{

    /// <summary>
    /// Gets the first char in <paramref name="segment"/>, or <c>'\0'</c> if it is empty.
    /// </summary>
    private char GetFirstChar(ReadOnlySpan<char> segment)
    {
        return segment.Length == 0 ? '\0' : segment[0];
    }

    /// <summary>
    /// Returns the prefix substring consisting of <c>"[chars...]"</c>.
    /// If <paramref name="input"/> starts with anything else (include whitespace), it returns nothing.
    /// If it isn't properly enclosed, the remainder of it.
    /// </summary>
    private string GetBracketSection(string input)
    {
        if (String.IsNullOrEmpty(input))
        {
            return String.Empty;
        }
        if (input[0] != '[')
        {
            return String.Empty;
        }

        int depth = 1;
        int index;
        for (index = 1; index < input.Length && depth > 0; index++)
        {
            char c = input[index];
            if (c == '[')
            {
                depth++;
            }
            if (c == ']')
            {
                depth--;
            }
        }

        return input[..index];
    }

    public CharacterSearch GetCharacterSearch(string? input)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return CharacterSearch.NonFilteringSearch;
        }

        input = input.TrimStart();

        string ipaCharacter = "";
        int currentI;
        for (currentI = 0; currentI < input.Length; currentI++)
        {
            char c = input[currentI];

            if (Char.IsWhiteSpace(c))
            {
                break;
            }
            if (Char.GetUnicodeCategory(c) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
                    or UnicodeCategory.TitlecaseLetter or UnicodeCategory.OtherLetter
                    or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber
                    or UnicodeCategory.Surrogate
                || c == '!')
            {
                ipaCharacter += c;
                continue;
            }
            if (c == '[' && GetFirstChar(input.AsSpan()[(currentI + 1)..]) is not ('+' or '-'))
            {
                string section = GetBracketSection(input);
                if (!section.EndsWith(']'))
                {
                    console.Error.WriteLine($"Warning: Unclosed bracket '[]' group in search filter: '{input}'");
                }

                if (section.Length == 2)
                {
                    console.Error.WriteLine($"Warning: Empty bracket group '[]' in search filter: '{input}'");
                }
                else
                {
                    ipaCharacter += section;
                }
                currentI += section.Length - 1;
                continue;
            }

            break;
        }

        List<string> diacritics = new();
        CharacterEnvironment? searchEnv = null;
        for (currentI = currentI; currentI < input.Length; currentI++)
        {
            char c = input[currentI];
            if (Char.IsWhiteSpace(c))
            {
                continue;
            }

            if (c == '[')
            {
                string section = GetBracketSection(input[currentI..]);
                currentI += section.Length - 1;
                if (!section.EndsWith(']'))
                {
                    console.Error.WriteLine($"Warning: Unclosed bracket '[]' group in search filter: '{input}'");
                }

                if (section.Length == 2)
                {
                    console.Error.WriteLine($"Warning: Empty bracket group '[]' in search filter: '{input}'");
                    continue;
                }

                switch (section)
                {
                    case "[+input]":
                        searchEnv ??= CharacterEnvironment.None;
                        searchEnv |= CharacterEnvironment.Input;
                        break;
                    case "[+output]":
                        searchEnv ??= CharacterEnvironment.None;
                        searchEnv |= CharacterEnvironment.Output;
                        break;
                    case "[+context]":
                        searchEnv ??= CharacterEnvironment.None;
                        searchEnv |= CharacterEnvironment.Context;
                        break;
                    case "[-input]":
                        searchEnv ??= CharacterEnvironment.All;
                        searchEnv &= ~CharacterEnvironment.Input;
                        break;
                    case "[-output]":
                        searchEnv ??= CharacterEnvironment.All;
                        searchEnv &= ~CharacterEnvironment.Output;
                        break;
                    case "[-context]":
                        searchEnv ??= CharacterEnvironment.All;
                        searchEnv &= ~CharacterEnvironment.Context;
                        break;
                    default:
                        diacritics.Add(section);
                        break;
                }

                continue;
            }

            if (Char.GetUnicodeCategory(c) is UnicodeCategory.Surrogate && currentI + 1 != input.Length)
            {
                diacritics.Add(input.Substring(currentI, 2));
                currentI++;
                continue;
            }
            diacritics.Add(c.ToString());
        }

        return new CharacterSearch
        {
            Character =  ipaCharacter,
            Diacritics = diacritics.ToArray(),
            Environment = searchEnv.GetValueOrDefault(CharacterEnvironment.All)
        };
    }
}
