using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bessett.SmartConsole.Tasks;
using Bessett.SmartConsole;

namespace Bessett.SmartConsole
{
    public class ConsoleProgram
    {
        public static TaskResult Start(string[] args)
        {
            return Start(args, typeof(ConsoleTask));
        }

        public static TaskResult Start(string[] args, Type baseTaskType)
        {
            Thread.CurrentPrincipal = new GenericPrincipal(WindowsIdentity.GetCurrent(), null);

            TaskLibrary.BuildAvailableTasks(baseTaskType);

            string[] validArgs = args.Length > 0 ? args : new string[] { "help" };

            var result  = StartTask(validArgs);

            if (Debugger.IsAttached )
            {
                ConsolePrompt("\nPress any key to return to IDE ...");
            }

            return result;
        }

        public static string[] ParseCommand(string args)
        {
            var result = new List<string>();

            int ctr = 0;
            string temp = "";
            bool escaped = false;

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

        internal static TaskResult StartTask(string[] args)
        {
            var taskname = args.Length > 0 ? args[0] : "help";
            var taskInstance = args.ToConsoleTask();

            if (taskInstance != null)
            {
                return StartTask(taskInstance);
            }

            return BadResult(-1, $"ERROR: Could not discover task: [{taskname}]\n");

        }
        internal static TaskResult StartTask(ConsoleTask taskInstance)
        {
            if (taskInstance != null)
            {
                if (taskInstance.ConfirmStart())
                {
                    var result = taskInstance.StartTask();
                    taskInstance.Complete();
                    return result;
                }
                return BadResult(-1, "Unable to confirm task to start.");
            }
            return BadResult(0, $"Empty Task\n");
        }

        private static TaskResult BadResult(int resultcode, string message)
        {
            return new TaskResult()
            {
                 ResultCode = resultcode,
                 IsSuccessful = false,
                 Message = message
            };
        }
 

    }
}
