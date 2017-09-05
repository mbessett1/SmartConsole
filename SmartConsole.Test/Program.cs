using Bessett.SmartConsole;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartConsole.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var tb = DynamicTasks
                .AddConsoleTask<ConsoleTask>("HelloWorld")
                    .AddTaskHelp("This is a Hello World Dynamic Task")
                    .HasAlias("hello-world")
                    //.HasNoConfirmation()
                    .StartTaskBody("Console.WriteLine($\"Hello World, {Name}! You specified Age={Age}.\");\nreturn TaskResult.Complete();")
                    .ConfirmStartBody("Console.WriteLine(\"Please confirm!\"); \nreturn base.ConfirmStart();  ")
                    .AddProperty("Name", typeof(string), "Name of person", true)
                    .AddProperty("Age", typeof(int))
                ;
            Console.WriteLine(tb.ToCode());

            DynamicTasks.CreateDynamic();

            ConsoleProgram.Start(args);
        }

    }
}







