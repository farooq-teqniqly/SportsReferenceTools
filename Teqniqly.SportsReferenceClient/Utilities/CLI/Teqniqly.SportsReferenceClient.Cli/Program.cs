using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Teqniqly.BaseballReferenceClient;
using Teqniqly.SportsReferenceClient.Cli;
using Teqniqly.SportsReferenceClient.Cli.Commands;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection().AddBaseballReferenceClient(configuration);
services.AddSingleton(AnsiConsole.Console);

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config.SetApplicationName("sportsref");

    config.AddBranch(
        "baseball",
        baseball =>
        {
            baseball.SetDescription("Baseball Reference data.");

            baseball.AddBranch(
                "schedule",
                schedule =>
                {
                    schedule.SetDescription("Season schedules.");

                    schedule
                        .AddCommand<GetScheduleCommand>("get")
                        .WithDescription("Download a season schedule to a file.")
                        .WithExample(
                            "baseball",
                            "schedule",
                            "get",
                            "--year",
                            "2026",
                            "--file",
                            "schedule.shtml"
                        );
                }
            );
        }
    );
});

return await app.RunAsync(args);
