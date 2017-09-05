using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    public class Test1 : ConsoleTask
    {
        public override void Complete()
        {
            //throw new NotImplementedException();
        }

        public override void Start()
        {
            Console.WriteLine("Test1 executing");
        }
    }

    [TaskAlias("get-test1")]
    public class AliasTest : ConsoleTask
    {
        public override void Complete()
        {
            Console.WriteLine($"Alias TEST complete.");
        }

        public override TaskResult StartTask()
        {
            Console.WriteLine($"Alias TEST executing...");

            return TaskResult.Complete();
        }
    }
}
