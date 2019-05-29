using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bessett.SmartConsole.TimeExtensions;

namespace Bessett.SmartConsole
{
    public class ConsoleProgram
    {
        public static TaskResult Start(string[] args, string defaultTask = "shell")
        {
            string[] validArgs = args.Length > 0 ? args : new string[] { defaultTask };

            var result = StartTask(validArgs);

            if (Debugger.IsAttached)
            {
                ConsolePrompt("\nPress any key to return to IDE ...");
            }

            return result;
        }

        internal static string[] ExpandCommand(string args)
        {
            var result = new List<string>();

            var temp = "";
            var escaped = false;

            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];

                switch (c)
                {
                    case ' ':
                        if (escaped)
                        {
                            temp += c.ToString();
                        }
                        else
                        {
                            if (temp.Length > 0)
                            {
                                result.Add(temp);
                                temp = "";
                            }
                        }
                        break;
                    case '"':
                        escaped = !escaped;
                        break;

                    default:
                        temp += c.ToString();
                        break;
                }
            }
            if (temp.Length > 0)
                result.Add(temp);
            return result.ToArray();
        }

        public static ConsoleKeyInfo ConsolePrompt(string promptText = "\nPress 'Y' to continue, any key to cancel... ")
        {
            Console.Write(promptText);
            var consoleInfo = Console.ReadKey();
            Console.WriteLine();
            return consoleInfo;
        }

        internal static TaskResult StartTask(string[] args, string defaultTask = "shell")
        {
            var taskname = args.Length > 0 ? args[0] : defaultTask;
            var taskInstance = args.ToConsoleTask();

            if (taskInstance != null)
            {
                return StartTask(taskInstance);
            }

            return TaskResult.Failed( $"ERROR: Could not start [{taskname}]\n");

        }

        public static TaskResult StartTask(string command)
        {
            return StartTask(ExpandCommand(command));
        }

        internal static TaskResult StartTask(ConsoleTask taskInstance)
        {
            if (taskInstance != null)
            {
                if (taskInstance.ConfirmStart())
                {
                    var clock = new Stopwatch();
                    clock.Start();
                    var result = taskInstance.StartTask();
                    taskInstance.Complete();

                    Console.WriteLine($"Completed in {clock.Elapsed.ToText()}");

                    return result;
                }
                return TaskResult.Failed("Unable to confirm task to start.", 1);
            }
            return TaskResult.Failed( $"No Task to start\n");
        }

    }
}
