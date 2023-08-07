using Bessett.SmartConsole;
using System;

namespace SmartConsole.TestDNC
{
    class Program
    {
        static void Main(string[] args)
        {

            var options = new SmartConsoleOptions()
            {
                Splash = () =>
                {
                    Console.WriteLine("SmartConsole Service Test");
                    Console.WriteLine("Type 'help' for a list of commands.");
                    Console.WriteLine($"{Environment.UserDomainName}");
                },
                Prompt = () => $"{Environment.UserName}> "
            };

            var taskResult = ConsoleProgram.StartShell(options);
            Console.WriteLine(taskResult.Message);                        
        }
    }

    //static DoMethod

}
