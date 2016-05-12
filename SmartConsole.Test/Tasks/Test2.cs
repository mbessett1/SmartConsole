using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    public class Test2: Test1
    {
        [ArgumentHelp]
        public string ArgValue { get; set; }

        public Test2()
        {
            ArgValue = "TestValue";
        }
        public override bool ConfirmStart()
        {
            return base.ConfirmStart();
        }
        public override void Start()
        {
            Console.WriteLine("Test2 executing");
        }
    }
}
