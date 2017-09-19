using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bessett.SmartConsole;
using SmartConsole.Test.Tasks;
using Bessett.SmartConsole.Tasks;

namespace SmartConsole.Test.Packages
{
    public class TestPackage : TaskPackage
    {
        public TestPackage()
        {
            AddTask(new PackageTestCaseA());
            AddTask(new PackageTestCaseB());
        }
    }

    [TaskAlias("test-packageTask1")]
    [TaskHelp("ConsoleTask artifact for package test (A)")]
    public class PackageTestCaseA : ConsoleTask
    {
        [RequiredArgument]
        public string Filename { get; set; }

        public override TaskResult StartTask()
        {
            Console.WriteLine($"Running PackageTestCaseA");
            return TaskResult.Complete("Packaged Task Successful");
        }
    }
    [TaskAlias("test-packageTask2")]
    [TaskHelp("ConsoleTask artifact for package test (B)")]
    public class PackageTestCaseB : ConsoleTask
    {
        public override TaskResult StartTask()
        {
            Console.WriteLine($"Running PackageTestCaseB");
            return TaskResult.Complete("Packaged Task Successful");
        }
    }

}
