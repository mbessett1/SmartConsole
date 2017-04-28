using System;
using System.IO;

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
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(Filename))
                {
                    string line = sr.ReadToEnd();
                    var args = ConsoleProgram.ParseCommand(line);
                    base.AddTask( args.ToConsoleTask());
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