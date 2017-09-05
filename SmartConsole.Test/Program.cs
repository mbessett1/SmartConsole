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
using SmartConsole.Test.Tasks;

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
                    .StartTaskBody(new TaskBuilder.CodeBuilder() 
                        .Line("Console.WriteLine($\"Hello World, {Name}! You specified Age={Age}.\");") 
                        .Line("return TaskResult.Complete();")
                        )
                    .ConfirmStartBody(new TaskBuilder.CodeBuilder()
                        .Line("Console.WriteLine(\"Please confirm!\");")
                        .Line("return base.ConfirmStart();  ")
                        )
                    .AddProperty("Name", typeof(string), "Name of person", true)
                    .AddProperty("Age", typeof(int))
                ;

            var tb2 = DynamicTasks
                    .AddConsoleTask<ConsoleTask>("ServiceTest")
                    .AddTaskHelp("Call TestSystem.ServiceMethod Dynamically")
                    .UseMethod(typeof(TestSystem), "ServiceMethod")
                    .HasAlias("test3")
                ;
            Console.WriteLine(tb.ToCode());
            Console.WriteLine(tb2.ToCode());

            DynamicTasks.CreateDynamic();

            ConsoleProgram.Start(args);
        }

    }
}







