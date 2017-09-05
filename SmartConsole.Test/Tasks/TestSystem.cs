using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartConsole.Test.Tasks
{
    public class TestSystem
    {
        public int ServiceMethod(string text = "", int length = 0, long longLength = 100)
        {
            Console.WriteLine($"TestSystem: ServiceMethod: text = {text}");
            Console.WriteLine($"TestSystem: ServiceMethod: length = {length}");
            Console.WriteLine($"TestSystem: ServiceMethod: lognLength ={longLength}");

            return text.Length;
        }
    }
    public class DisposableTestSystem : IDisposable
    {
        public void Dispose() { }

        public int ServiceMethod(string text = "", int length = 0, long longLength = 100)
        {
            Console.WriteLine($"TestSystem: ServiceMethod: text = {text}");
            Console.WriteLine($"TestSystem: ServiceMethod: length = {length}");
            Console.WriteLine($"TestSystem: ServiceMethod: lognLength ={longLength}");

            return text.Length;
        }
    }
}
