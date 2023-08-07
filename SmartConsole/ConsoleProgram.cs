using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Bessett.SmartConsole.Tasks;
using Bessett.SmartConsole.TimeExtensions;

namespace Bessett.SmartConsole
{
    public class SmartConsoleOptions
    {
        public Func<string> Prompt { get; set; } = () => "SC> ";
        public Action Splash { get; set; } = () => { };
        public Action<string> LogText { get; set; } = (s) => { Console.WriteLine(s); };
    }

    public class ConsoleProgram
    {
        #region Public 
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

        public static TaskResult StartShell(SmartConsoleOptions options)
        {
            var taskResult = new Shell(options).StartTask();

            if (Debugger.IsAttached)
            {
                ConsolePrompt("\nPress any key to return to IDE ...");
            }

            return taskResult;
        }

        public static async Task<TaskResult> StartShellAsync(SmartConsoleOptions options)
        {
            var task = await new Shell(options).StartTaskAsync();

            if (Debugger.IsAttached)
            {
                ConsolePrompt("\nPress any key to return to IDE ...");
            }

            return task;
        }

        public static ConsoleKeyInfo ConsolePrompt(string promptText = "\nPress 'Y' to continue, any key to cancel... ")
        {
            Console.Write(promptText);
            var consoleInfo = Console.ReadKey();
            Console.WriteLine();
            return consoleInfo;
        }

        public static TaskResult StartTask(string command)
        {
            return StartTask(ExpandCommand(command));
        }
        
        public static async Task<TaskResult> StartTaskAsync(string command)
        {
            return await Task.Run(() => StartTask(command));
        }

        #endregion

        #region internal
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

        internal static TaskResult StartTask(string[] args, string defaultTask = "shell")
        {
            var taskname = args.Length > 0 ? args[0] : defaultTask;
            var taskInstance = args.ToConsoleTask();

            if (taskInstance != null)
            {
                return StartTask(taskInstance);
            }

            return TaskResult.Failed($"ERROR: Could not start [{taskname}]\n");
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
            return TaskResult.Failed($"No Task to start\n");
        }


        #endregion

    }
}
