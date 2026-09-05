
using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using CommandDotNet.Rendering;
using Microsoft.Extensions.DependencyInjection;
using SCCompendium.Application.CliIO;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Infrastructure.CliIO;
using SCCompendium.Infrastructure.DbAccess;
using SCCompendium.Infrastructure.Parser;
using SCCompendium.Infrastructure.Parser.LatexParser;
using SCCompendium.Presentation;
using SCCompendium.Presentation.App;

AppRunner<App> runner = new AppRunner<App>();
runner.Configure(b =>
{
    b.CustomHelpProvider = new HelpInterceptor(b.AppSettings, b.Console);
    b.AppSettings.Arguments.BooleanMode = BooleanMode.Implicit;
});
ServiceCollection collection = new();
foreach (var commandClassType in runner.GetCommandClassTypes())
{
    collection.AddScoped(commandClassType.type);
}
foreach (var type in App.GetCommandImplementationDependencies())
{
    collection.AddScoped(type);
}
collection.AddSingleton<IDbWriter, DbWriter>();
collection.AddSingleton<IDbReader, DbReader>();
collection.AddSingleton<IConsoleIO, SystemConsole>();
collection.AddSingleton<IFileFinder, FileFinder>();
collection.AddTransient<IDbConnectionRepository, DbConnectionRepository>();
collection.AddTransient<IDiachronicaParser, DiachronicaParser>();
collection.AddTransient<ILatexParser, LatexParser>();
collection.AddTransient<IPhonologicalRuleParser, PhonologicalRuleParser>();

runner.UseMicrosoftDependencyInjection(collection.BuildServiceProvider());

Environment.Exit(runner.Run(args));
