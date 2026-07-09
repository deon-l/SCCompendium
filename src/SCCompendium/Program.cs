
using CommandDotNet;
using SCCompendium.Presentation;
using SCCompendium.Presentation.App;

// Todo: set up "dependency injection" in setting static members in App
AppRunner<App> runner = new AppRunner<App>();
runner.Configure(b =>
{
    b.CustomHelpProvider = new HelpInterceptor(b.AppSettings, b.Console);
});
Environment.Exit(runner.Run(args));
