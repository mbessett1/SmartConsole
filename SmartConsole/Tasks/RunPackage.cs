using System;
using System.Collections.Generic;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation,TaskHelp("Run a Task Package")]
    public class RunPackage : ConsoleTask
    {
        [ArgumentHelp("Package Name")]
        public string Name { get; set; }

        [ArgumentHelp("Package File Name (command script)")]
        public string Filename { get; set; }

        private TaskPackage package;

        public override bool ConfirmStart()
        {
            if (string.IsNullOrEmpty(Filename))
            {
                if (string.IsNullOrEmpty(Name))
                {
                    Console.WriteLine($"RunPackage requires either a name or a filename to execute");
                    return false;
                }
                else
                {
                    //activate & load the package
                    Console.WriteLine($"Activating Package {Name}");
                    package = PackageLibrary.GetPackage(Name);
                    if (package == null)
                    {
                        Console.WriteLine($"Package [{Name}] not found. GetPackages to view available packages.");
                        // run GetPackages for the user


                        return false;
                    }
                }
            }
            else
            {
                package = new TaskPackageFile(Filename);
            }

            return base.ConfirmStart();
        }

        public override TaskResult StartTask()
        {
            TaskResult result = new TaskResult();

            Console.WriteLine($"Executing Package {Name}");

            var Results = new List<TaskResult>();

            try
            {
                foreach (var task in package.GetTasks())
                {
                    task.Silent = Silent;
                    if (task.ConfirmStart())
                    {
                        result = task.StartTask();

                        Results.Add(result);  //$"{task.GetType().Name}  {result.Message}"
                        if (!result.IsSuccessful)
                        {
                            Console.WriteLine(result.Message);
                            break;
                        }
                    }
                    else
                    {
                        throw new Exception($"{task.GetType().Name} was not valid to start.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.ResultCode = -1;
                result.IsSuccessful = false;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}