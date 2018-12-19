using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    [NoConfirmation]
    [TaskAlias("qtest1")]
    [TaskHelp("NoArguments")]
    public class AliasTest : ConsoleTask
    {
        public override void Complete()
        {
            Console.WriteLine($"Alias TEST complete.");
        }

        public override TaskResult StartTask()
        {
            Console.WriteLine($"Alias TEST executing...");
            WriteArguments();
            return TaskResult.Complete();
        }
    }

    [NoConfirmation]
    [TaskAlias("qtest2")]
    [TaskHelp("Arguments present")]
    public class AliasTest2 : ConsoleTask
    {
        [ArgumentHelp]
        public string Name { get; set; }
        [ArgumentHelp]
        public string LongDescription { get; set; }
        [ArgumentHelp]
        public string Instance { get; set; }

        public override void Complete()
        {
            Console.WriteLine($"{GetType().Name} complete.");
        }

        public override TaskResult StartTask()
        {
            Console.WriteLine($"Alias TEST executing...");
            WriteArguments();
            return TaskResult.Complete();
        }
    }
}
