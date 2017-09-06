using Bessett.SmartConsole;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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
            ConsoleProgram.Start(args);
        }

    }

    [NoConfirmation]
    public class T1 : ConsoleTask
    {
        public bool Compile { get; set; }

        public override TaskResult StartTask()
        {
            var tb = DynamicTasks
                .AddConsoleTask<ConsoleTask>("HelloWorld")
                    .AddTaskHelp("This is a Hello World Dynamic Task")
                    .HasAlias("hello-world")
                    .HasNoConfirmation()
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

            Console.WriteLine(tb.ToCode());

            if (Compile)
                DynamicTasks.CreateDynamic();

 //           Debugger.Break();
            return TaskResult.Complete();

        }
    }
    // CreateTask -Name DoTask -TargetType SmartConsole.Test.Tasks.TestSystem -TargetMethod ServiceMethod
    [NoConfirmation]
    public class CreateTask : ConsoleTask
    {
        public bool Compile { get; set; } = true;
        public string Name { get; set; } = "DoTask";
        public string Alias { get; set; } = "";
        public string TargetType { get; set; } = "SmartConsole.Test.Tasks.DisposableTestSystem";
        public string TargetMethod { get; set; } = "ServiceMethod";

        public override TaskResult StartTask()
        {
            try
            {
                var tb2 = new TaskBuilder(Name, typeof(ConsoleTask))
                        .AddTaskHelp($"Call {TargetType}.{TargetMethod} Dynamically")
                        .UseMethod(Type.GetType(TargetType), TargetMethod)
                        //.HasAlias(Alias)
                    ;

                Console.WriteLine(tb2.ToCode());

                if (Compile)
                {
                    DynamicTasks.AddConsoleTask(tb2);
                    var failures = DynamicTasks.CreateDynamic();

                    Console.WriteLine( failures.Any()? failures.BuildString() : "Successfully compiled" );
                    // figure out a way to RUN a task from any other task

                }

                return TaskResult.Complete();
            }
            catch (Exception ex)
            {
                return TaskResult.Exception(ex);
            }
        }
    }
}







