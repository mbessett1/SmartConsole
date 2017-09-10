using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    [NoConfirmation]
    [TaskAlias("get-test1")]
    public class AliasTest : ConsoleTask
    {
        public override void Complete()
        {
            Console.WriteLine($"Alias TEST complete.");
        }

        public override Bessett.SmartConsole.TaskResult StartTask()
        {
            Console.WriteLine($"Alias TEST executing...");

            return TaskResult.Complete();
        }
    }
}
