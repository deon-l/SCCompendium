using System.Diagnostics;
using CommandDotNet;
using CommandDotNet.Builders;
using CommandDotNet.Help;

namespace SCCompendium.Presentation;

/// <summary>
/// If <see cref="EnableIntercept"/> is true, intercept the help message and redirect it to <see cref="IConsole.Error"/>.
/// </summary>
/// <remarks>Also changes naming of application to not assume executables end in '.exe'</remarks>
public class HelpInterceptor : HelpTextProvider
{
    private readonly IConsole _console;
    public static bool EnableIntercept { get; set; } = true;

    private static string GetAppName()
    {
        AppInfo info = AppInfo.Instance;
        if (info.IsRunViaDotNetExe)
        {
            return $"dotnet {info.FileName}";
        }

        // AppInfo.FileName is inaccurate for standalone non-.exe files (e.g. Mac executables)
        string? pathname = Process.GetCurrentProcess().MainModule?.FileName;
        if (pathname is null)
        {
            if (info.FileName.EndsWith(".dll"))
            {
                return $"dotnet {info.FileName}";
            }

            return info.FileName;
        }
        return Path.GetFileName(pathname);
    }

    public HelpInterceptor(AppSettings appSettings, IConsole console) : base(appSettings, GetAppName())
    {
        _console = console;
    }

    public override string GetHelpText(Command command)
    {
        string helpStr = base.GetHelpText(command);
        if (EnableIntercept)
        {
            _console.Error.WriteLine(helpStr);
            return String.Empty;
        }

        return helpStr;
    }
}
