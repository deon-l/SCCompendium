using SCCompendium.Application.CliIO;

namespace SCCompendium.Infrastructure.CliIO;

public class FileFinder : IFileFinder
{
    public TextReader GetFile(string path)
    {
        StreamReader file = new (File.OpenRead(path));
        return file;
    }
}
