using Bessett.SmartConsole;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Isam.Esent.Interop;
using SmartConsole.Test.Tasks;

namespace SmartConsole.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // prebuild some Dynamic ConsoleTasks
            ConsoleProgram.Start(args);
        }

    }
}







