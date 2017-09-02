using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bessett.SmartConsole;
using SmartConsole.Test.Tasks;

namespace SmartConsole.Test.Packages
{
    public class TestPackage:TaskPackage
    {
        public TestPackage()
        {
            AddTask(new Test1());
            AddTask(new Test2());
        }
    }
}
