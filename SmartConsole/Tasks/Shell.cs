using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation, TaskHelp("Command Shell")]
    public class Shell : ConsoleTask
    {
        private readonly SmartConsoleOptions _options;

        internal Shell(SmartConsoleOptions options)
        {
            if (options != null)
            {
                _options = options;
            }
            else
            {
                _options = new SmartConsoleOptions();
            }

            SplashText = _options.Splash;
            Prompt = _options.Prompt;

        }

        public Shell() { }

        internal Action SplashText { get; set; } = () =>
        {
            Console.WriteLine($"{System.AppDomain.CurrentDomain.FriendlyName} Shell\n");
            Console.WriteLine("'exit', 'quit' or 'q' to quit");
            Console.WriteLine("'?' or 'help' for help\n");
        };

        internal Func<string> Prompt { get; set; } = () => "SC> ";

        public override TaskResult StartTask()
        {
            return StartTaskAsync().Result;
        }

        public override async Task<TaskResult> StartTaskAsync()
        {
            // Read a command line and perform the tasks over
            // and over

            string command;
            const string helpCommand = "help -Usagetext \"SHELL Usage: <Task Name> [task options]\n\"";

            SplashText.Invoke();

            do
            {
                Console.Write(Prompt.Invoke());
                { command = Console.ReadLine().Trim();}

                if (   command.ToLower() == "quit"
                    || command.ToLower() == "q"
                    || command.ToLower() == "exit")  break;

                // augment requests for help
                if (
                    command.Equals("?") ||
                    command.ToLower().Equals("help")) command = helpCommand;

                if (command.Length > 0)
                {
                    try
                    {
                        var result = await ConsoleProgram.StartTaskAsync(command);

                        if (!string.IsNullOrEmpty(result.Message))
                            Console.WriteLine($"[{result.ResultCode}]:\n{result.Message}\n");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"An exception ocurred: {ex.GetBaseException().Message}");
                    }
                }

            } while (true);

            return TaskResult.Complete("exiting SmartConsole Shell");
        }

    }
}
