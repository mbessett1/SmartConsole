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

namespace Bessett.SmartConsole
{
    public class ConsoleProgram
    {
        public static void Start(string[] args)
        {
            Start(args, typeof(ConsoleTask));
        }

        public static void Start(string[] args, Type baseTaskType)
        {
            Thread.CurrentPrincipal = new GenericPrincipal(WindowsIdentity.GetCurrent(), null);

            TaskLibrary.BuildAvailableTasks(baseTaskType);

            string[] validArgs = args.Length > 0 ? args : new string[] { "help" };

            StartTask(validArgs);

            if (Debugger.IsAttached )
            {
                ConsolePrompt("\nPress any key to return to IDE ...");
            }
        }

        public static ConsoleKeyInfo ConsolePrompt(string promptText = "\nPress 'Y' to continue, any key to cancel... ")
        {
            Console.Write(promptText);
            var consoleInfo = Console.ReadKey();
            Console.WriteLine();
            return consoleInfo;
        }
     
        private static void StartTask(string[] args)
        {
            var taskname = args.Length > 0 ? args[0] : "help";

            var taskInstance = TaskLibrary.GetTaskInstance<ConsoleTask>(taskname, args);

            if (taskInstance == null)
            {
                Console.WriteLine("ERROR: Could not start task: [{0}]\n", taskname);

                taskInstance = TaskLibrary.GetTaskInstance<Help>("help");
            }

            if (taskInstance != null)
            {
                if (taskInstance.ConfirmStart())
                {
                    taskInstance.Start();
                    taskInstance.Complete();
                }
            }
        }

    }
}
