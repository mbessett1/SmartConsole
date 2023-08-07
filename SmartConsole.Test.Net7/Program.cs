using Bessett.SmartConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure your services here.
        services.Configure<SmartConsoleOptions>(options =>
        {
            options.Splash = () =>
            {
                Console.WriteLine("SmartConsole Service Test");
                Console.WriteLine("Type 'help' for a list of commands.");
                Console.WriteLine($"{Environment.UserDomainName}");
            };

            options.Prompt= () => $"{Environment.UserName}> ";
        });

        services.AddHostedService<SmartConsoleService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        //logging.AddConsole();
    })
    .Build();

// Now you can use the services provided by the host.
//var logger = host.Services.GetRequiredService<ILogger<Program>>();

await host.RunAsync();

