using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObseumEU.Mluvii.Client;
using ObseumEU.Mluvii.Client.Cache;

// Prepare Dependency Injection and Environment
var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<MluviiCredentialOptions>(hostContext.Configuration.GetSection("Mluvii"));
        services.AddSingleton<ICacheService, InMemoryCache>();
        services.AddSingleton<ITokenEndpoint, TokenEndpoint>();
        services.AddSingleton<IMluviiClient, MluviiClient>();
    })
    .Build();

// Retrieve and Use mluvii client
using var serviceScope = host.Services.CreateScope();
var provider = serviceScope.ServiceProvider;

var mluviiClient = provider.GetRequiredService<IMluviiClient>();

//Call sessions
var sessions = await mluviiClient.GetSessions(limit: 10);
Console.WriteLine(string.Join(Environment.NewLine, sessions.value.Select(s => s.Id)));
