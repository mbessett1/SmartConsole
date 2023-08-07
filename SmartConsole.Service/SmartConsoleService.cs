using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Bessett.SmartConsole
{
    public class SmartConsoleService : IHostedService
    {
        private Task _backgroundTask;
        private readonly IConfiguration _configuration;
        private readonly SmartConsoleOptions _options;

        public SmartConsoleService(IConfiguration configuration, IOptions<SmartConsoleOptions> options)
        {
            _configuration = configuration;
            _options = options.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // This method is called when the application starts.
            // You can start your background tasks here.
            _backgroundTask = Task.Run(BackgroundProcessing, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // This method is called when the application is stopping.
            // You can do any cleanup or finalization here.
            return _backgroundTask;
        }

        private async Task BackgroundProcessing()
        {
            await ConsoleProgram.StartShellAsync(_options);
        }
    }
}
