using System;
using System.Collections.Generic;
using System.Linq;
using Bessett.SmartConsole;

namespace SmartConsole.Test.Tasks
{
    [NoConfirmation]
    [TaskHelp("Build a sample Dynamic Console Task")]
    [TaskAlias("test-dynamic3")]
    public class DynamicTaskTest : ConsoleTask
    {
        [ArgumentHelp("Compile Task after building")]
        public bool Compile { get; set; } = false;

        [ArgumentHelp("Compile Task after building")]
        public bool Alias { get; set; } = false;

        public override TaskResult StartTask()
        {
            try
            {
                var tb = DynamicTasks
                        .AddConsoleTask<ConsoleTask>("DynamicSample")
                        .AddTaskHelp("Sample Dynamic Task")
                        .HasAlias("ds")
                        .HasNoConfirmation()
                        .StartTaskBody(new List<string>()
                        {
                            "Console.WriteLine($\"Hello World, {Name}! You specified Age={Age}.\");",
                            "return TaskResult.Complete();"
                        })
                        .ConfirmStartBody(new List<string>()
                        {
                            "Console.WriteLine(\"Please confirm!\");",
                            "return base.ConfirmStart();  "
                        })
                        .AddProperty("Name", typeof(string), "Name of person", true)
                        .AddProperty("Age", typeof(int), "Age of person", false)
                    ;

                if (Compile)
                {
                    DynamicTasks.AddConsoleTask(tb);
                    var failures = DynamicTasks.CreateDynamic();

                    if (!failures.Any())
                        ConsoleProgram.StartTask($"Help ds");
                    else
                    {
                        Console.WriteLine(tb.ToView());
                        Console.WriteLine(failures.Expand());
                        return TaskResult.Failed("Compile failed.");
                    }
                }
                else
                {
                    Console.WriteLine(tb.ToCode());
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

