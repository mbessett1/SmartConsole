using System;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation, TaskHelp("List available packages")]
    public class GetPackages: ConsoleTask
    {
        public override TaskResult StartTask()
        {
            Console.WriteLine($"Packages Available:");
            foreach (var package in PackageLibrary.ListAll())
            {
                Console.WriteLine($"   {package.Name}");
            }

            return new TaskResult() {IsSuccessful = true};
        }
    }
}