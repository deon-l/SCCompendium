
using CommandDotNet;
using SCCompendium.Tests.Presentation;

// Todo: set up "dependency injection" in setting static members in App
AppRunner<App> runner = new AppRunner<App>();
runner.Run(args);
