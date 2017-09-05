using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    public class TestSystem
    {
        public int ServiceMethod(string text)
        {
            Console.WriteLine($"TestSystem: ServiceMethod: {text}");
            return text.Length;
        }
    }
}
