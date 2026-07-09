
using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SCCompendium.Application.DbAccess;
using SCCompendium.Application.Parser;
using SCCompendium.Infrastructure.DbAccess;
using SCCompendium.Infrastructure.Parser;
using SCCompendium.Infrastructure.Parser.LatexParser;
using SCCompendium.Presentation;
using SCCompendium.Presentation.App;

// Todo: set up "dependency injection" in setting static members in App
AppRunner<App> runner = new AppRunner<App>();
runner.Configure(b =>
{
    b.CustomHelpProvider = new HelpInterceptor(b.AppSettings, b.Console);
});
ServiceCollection collection = new();
foreach (var commandClassType in runner.GetCommandClassTypes())
{
    collection.AddScoped(commandClassType.type);
}

collection.AddSingleton<IDbWriter, DbWriter>();
collection.AddSingleton<IDbReader, DbReader>();
collection.AddTransient<IDbConnectionRepository>(_ => null!); // TODO: connect implementation.
collection.AddTransient<IDiachronicaParser, DiachronicaParser>();
collection.AddTransient<ILatexParser, LatexParser>();
collection.AddTransient<IPhonologicalRuleParser, PhonologicalRuleParser>();

runner.UseMicrosoftDependencyInjection(collection.BuildServiceProvider());

Environment.Exit(runner.Run(args));
