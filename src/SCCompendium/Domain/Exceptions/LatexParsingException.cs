namespace SCCompendium.Domain.Exceptions;

/// <summary>
/// The exception to throw when a presented Latex segment cannot be parsed.
/// </summary>
public class LatexParsingException : FormatException
{
    /// <summary>
    /// The line where the parsing error occured at.
    /// </summary>
    public string Line { get; }
    /// <summary>
    /// The column number where the parsing error occured at, or <c>-1</c>.
    /// </summary>
    public int ColumNumber { get; }
    /// <summary>
    /// The message explaining what the error is.
    /// </summary>
    public string ErrorMessage { get; }
    /// <summary>
    /// The provided <see cref="Line"/> is partially parsed.
    /// </summary>
    public bool IsPartiallyParsed { get; init; } = false;

    public LatexParsingException(Exception? innerException, string line, string error = "", int columnNumber = -1) : base(error, innerException)
    {
        Line = line;
        ColumNumber = columnNumber;
        ErrorMessage = error;
    }
    public LatexParsingException(string line, string error = "", int columnNumber = -1) : this(null, line, error, columnNumber)
    { }

    public LatexParsingException(string message) : this(String.Empty, message)
    { }

    public override string Message
    {
        get
        {
            string message = base.Message;
            if (String.IsNullOrWhiteSpace(Line))
            {
                return message;
            }
            if (String.IsNullOrWhiteSpace(message))
            {
                return $"Could not part the following Latex{(IsPartiallyParsed ? " (partially parsed)" : "")}: \n'{Line}'";
            }

            message = $"Error parsing Latex{(IsPartiallyParsed ? " (partially parsed)" : "")}: {message}\n'{Line}'";
            if (ColumNumber >= 0)
            {
                message += $"\n{new String(' ', ColumNumber + 1)}^";
            }
            return message;
        }
    }
}
