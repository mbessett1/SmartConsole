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
            AddTask(new PackageTestCaseA() );
            AddTask(new BuildTask()
            {
                Name = "WierdTest",
                Alias = "test-WierdTest",
                Compile = true,
                Description = "Testing result  from BuildTask Task (I know, wierd)",
                TargetMethod = "ServiceMethod",
                TargetType = "SmartConsole.Test.DisposableTestSystem"
            }) ;
        }
    }

    [TaskAlias("test-packageTask1")]
    [TaskHelp("ConsoleTask artifact for package test")]
    public class PackageTestCaseA : ConsoleTask
    {
        public override TaskResult StartTask()
        {
            Console.WriteLine($"Running PackageTestCaseA");
            return TaskResult.Complete("Packaged Task Successful");
        }
    }

}
