using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    // ParserTest -arg1 -arg2
    [NoConfirmation, TaskHelp("Test Command parsing")]
    class ParserTest : ConsoleTask
    {
        [ArgumentHelp]
        public string TestCmd { get; set; }

        public bool Arg1 { get; set; }

        public string Arg2 { get; set; }

        public override TaskResult StartTask()
        {
            Console.WriteLine($"Arg1: '{Arg1}'");
            Console.WriteLine($"Arg2: '{Arg2}'");

            return TaskResult.Complete();
        }
    }
}
