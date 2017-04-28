using System;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation,TaskHelp("Run a Task Package")]
    public class RunPackage : ConsoleTask
    {
        [ArgumentHelp("Package Name")]
        public string Name { get; set; }

        [ArgumentHelp("Package File Name (xml or json)")]
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

            try
            {
                foreach (var task in package.GetTasks())
                {
                    result = task.StartTask();
                    if (!result.IsSuccessful)
                    {
                        break;
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