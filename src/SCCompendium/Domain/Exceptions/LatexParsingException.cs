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

    public LatexParsingException(Exception? innerException, string line, string error = "", int columnNumber = -1) : base(error, innerException)
    {
        Line = line;
        ColumNumber = columnNumber;
    }
    public LatexParsingException(string line, string error = "", int columnNumber = -1) : this(null, line, error, columnNumber)
    { }

    public override string Message
    {
        get
        {
            string message = base.Message;
            if (String.IsNullOrWhiteSpace(message))
            {
                return $"Could not part the following Latex: \n'{Line}'";
            }

            message = $"Error parsing Latex: {message}\n'{Line}'";
            if (ColumNumber >= 0)
            {
                message += $"\n{new String(' ', ColumNumber + 1)}^";
            }
            return message;
        }
    }
}
