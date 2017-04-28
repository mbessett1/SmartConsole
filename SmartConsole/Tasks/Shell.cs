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

            Console.WriteLine("Smart Console Shell\n");
            Console.WriteLine("'exit' to quit");
            Console.Write("'?' for help\n");

            do
            {
                Console.Write("SC> ");
                command = Console.ReadLine().Trim();

                if (command.ToLower() == "quit") break;
                if (command.ToLower() == "q") break;
                if (command.ToLower() == "exit") break;
                if (command.Equals("?")) command = "Help";

                if (command.Length > 0)
                {
                    var args = ConsoleProgram.ParseCommand(command);
                    var result = ConsoleProgram.StartTask(args);

                    Console.WriteLine($"\nResult {result.ResultCode}: {result.Message}\n");
                }

            } while (true);

            return new TaskResult() {IsSuccessful = true, Message = "exiting SmartConsole Shell"};
        }

    }
}
