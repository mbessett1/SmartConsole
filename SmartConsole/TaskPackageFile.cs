using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Bessett.SmartConsole
{
    public class TaskPackageFile : TaskPackage
    {
        [ArgumentHelp]
        public string Filename { get; set; }

        public TaskPackageFile(string filename)
        {
            Filename = filename;

            Console.WriteLine($"Reading Package from {filename}");
            try
            {
                foreach (var line in File.ReadAllLines(Filename).Where(t => t.Trim().Any()))
                {
                    var args = ConsoleProgram.BuildCommand(line);
                    AddTask(args.ToConsoleTask());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The package file could not be read:");
                Console.WriteLine(e.Message);
            }

        }
    }
}