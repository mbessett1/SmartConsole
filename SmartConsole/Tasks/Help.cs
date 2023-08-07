using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bessett.SmartConsole;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation, TaskHelp("SmartConsole Help")]
    internal class Help: ConsoleTask
    {
        [DefaultArgument, ArgumentHelp]
        public string TaskName { get; set; }

        public string UsageText { get; set; } = $"Usage: {System.AppDomain.CurrentDomain.FriendlyName} <Task Name> [task options]\n";

        public override void Start()
        {
            var helpTask = TaskLibrary.GetTask(TaskName);

            if (helpTask == null)
            {
                ShowGenericHelp();
            }
            else
            {
                if (!helpTask.NoHelp())
                {
                    ShowTaskHelp();
                }
            }
        }

        private void ShowTaskHelp()
        {
            var taskObject = TaskLibrary.GetTaskInstance<ConsoleTask>(TaskName);
            var arguments = taskObject.GetType().GetTaskArguments().OrderByDescending(arg=> arg.IsRequired).ToList();

            Console.WriteLine("\nTask: {0}\n   {1}",
                taskObject.HelpName,
                taskObject.HelpDescription);

            Console.Write("\nSyntax:\n   {0} {1}\n",
                taskObject.HelpName,
                MemberSyntax(arguments)
                );

            if (arguments.Any())
            {
                Console.WriteLine("\nParameters:");

                foreach (var arg in arguments)
                {
                    Console.WriteLine(ArgumentSyntax(arg));
                }
            }
            else
            {
                Console.WriteLine("\nNo Parameters");
            }
        }

        private void ShowGenericHelp()
        {
            Console.WriteLine(UsageText);
            Console.WriteLine("Tasks Available:\n");

            foreach (var task in TaskLibrary.AllTasks.OrderBy(t=> t.Name))
            {
                if (!task.NoHelp())
                {
                    var taskObject = TaskLibrary.GetTaskInstance<ConsoleTask>(task);
                    Console.WriteLine("  {0}:\n    {1}\n", taskObject.HelpName, taskObject.HelpDescription); 
                }
            }
        }

        private string MemberSyntax(List<ArgumentHelp> arguments)
        {
            string result = "";

            foreach (var arg in arguments)
            {
                result += MemberHelpText(arg);
            }
            return result;
        }
        private string ArgumentSyntax(ArgumentHelp arg)
        {
            return string.Format("  {3}-{0} <{1}>{4} {6} {5}",
                arg.PropertyInfo.Name,
                arg.PropertyInfo.PropertyType.Name,
                arg.IsRequired ? "* " : "  ",
                !arg.IsRequired ? "[" : " ",
                !arg.IsRequired ? "]" : " ",
                string.IsNullOrEmpty(arg.HelpText) ? "" : arg.HelpText,
                ""
                );
        }

        private string MemberHelpText(ArgumentHelp taskAttribute)
        {
            var result = "";

            if (taskAttribute.IsRequired)
            {
                result = string.Format("-{0} value ",
                    taskAttribute.PropertyInfo.Name,
                    taskAttribute.HelpText,
                    taskAttribute.IsRequired ? " *" : "");

            }
            else
            {
                result = string.Format("[-{0} {3} ]",
                    taskAttribute.PropertyInfo.Name,
                    taskAttribute.HelpText,
                    taskAttribute.IsRequired ? " *" : "",
                    taskAttribute.PropertyInfo.PropertyType.FullName == "System.Boolean" ? "[YES | no]" : "<value>"
                    );

            }
            return result;
        }

        public override void Complete()
        {

        }
    }

}
