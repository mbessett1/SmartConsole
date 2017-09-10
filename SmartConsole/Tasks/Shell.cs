using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation, TaskHelp("Command Shell")]
    public class Shell : ConsoleTask
    {
        public override TaskResult StartTask()
        {
            // Read a command line and perform the tasks over
            // and over

            string command;
            const string helpCommand = "help -Usagetext \"SHELL Usage: <Task Name> [task options]\n\"";

            Console.WriteLine($"{System.AppDomain.CurrentDomain.FriendlyName} Shell\n");
            Console.WriteLine("'exit', 'quit' or 'q' to quit");
            Console.WriteLine("'?' or 'help' for help\n");

            do
            {
                Console.Write("SC> ");
                { command = Console.ReadLine().Trim();}

                if (command.ToLower() == "quit") break;
                if (command.ToLower() == "q") break;
                if (command.ToLower() == "exit") break;

                // augment requests for help
                if (
                    command.Equals("?") ||
                    command.ToLower().Equals("help")) command = helpCommand;

                if (command.Length > 0)
                {
                    var result = ConsoleProgram.StartTask(command);

                    if(!string.IsNullOrEmpty(result.Message))
                        Console.WriteLine($"[{result.ResultCode}]:\n{result.Message}\n");
                }

            } while (true);

            return new TaskResult() {IsSuccessful = true, Message = "exiting SmartConsole Shell"};
        }

    }
}
