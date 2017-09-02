using System;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation, TaskHelp("List available packages")]
    public class GetPackages: ConsoleTask
    {
        private const string indent = "   ";

        public override TaskResult StartTask()
        {
            Console.WriteLine($"{indent}Task Packages Available:");
            foreach (var package in PackageLibrary.ListAll())
            {
                Console.WriteLine($"{indent}{indent}{package.Name}");
            }

            return new TaskResult() {IsSuccessful = true};
        }
    }
}