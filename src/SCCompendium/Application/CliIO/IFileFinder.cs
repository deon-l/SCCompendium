namespace SCCompendium.Application.CliIO;

/// <summary>
/// Finds files.
/// </summary>
public interface IFileFinder
{
    /// <summary>
    /// Finds the file at the specified path, and returns a reader of it open for reading.
    /// </summary>
    public TextReader GetFile(string path);
}
