using System;
using System.Collections.Generic;
using System.Linq;
using Bessett.SmartConsole;

namespace SmartConsole.Test.Tasks
{
    public abstract class ConsoleTaskCreator : ConsoleTask
    {
        [ArgumentHelp("Description for Dynamic task")]
        public string Name { get; set; } = "HelloWorld";
        [ArgumentHelp("Compile Task after building")]
        public bool Compile { get; set; } = true;
        [ArgumentHelp("Dynamic task should not confirm start")]
        public bool NoConfirmation { get; set; }
        [ArgumentHelp("Optional Alias for Dynamic task")]
        public string Alias { get; set; } = "";

        public TaskResult CompileTask(TaskBuilder tb)
        {
            if (!string.IsNullOrEmpty(Alias))
                tb.HasAlias(Alias);

            if (NoConfirmation)
                tb.HasNoConfirmation();

            if (Compile)
            {
                DynamicTasks.AddConsoleTask(tb);
                var failures = DynamicTasks.CreateDynamic();

                if (!failures.Any())
                {
                    var helpCommand = $"Help {(Alias.Length > 0 ? Alias : Name)}";
                    ConsoleProgram.StartTask(helpCommand);
                }
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

            return TaskResult.Complete("Compile Successful");
        }
    }

    [NoConfirmation]
    [TaskHelp("Create Dynamic ConsoleTask")]
    [TaskAlias("test-dynamic1")]
    public class DynamicTask : ConsoleTaskCreator
    {
        [ArgumentHelp("Description for Dynamic task")]
        public string Description { get; set; } = "This is a Hello World Dynamic Task";

        public override TaskResult StartTask()
        {
            try
            {
                var tb = DynamicTasks
                        .AddConsoleTask<ConsoleTask>(Name)
                        .AddTaskHelp(Description)
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
                return CompileTask(tb);
            }
            catch (Exception ex)
            {
                return TaskResult.Exception(ex);
            }
        }
    }

    // CreateTask -Name DoTask -TargetType SmartConsole.Test.Tasks.TestSystem -TargetMethod ServiceMethod
    [NoConfirmation]
    [TaskHelp("Creates Dynamic ConsoleTask based on encapsulated method")]
    [TaskAlias("tast-dynamic2")]
    public class CreateTaskTest : ConsoleTaskCreator
    {
        [ArgumentHelp("Description for Dynamic task")]
        public string Description { get; set; } = "";
        [ArgumentHelp("Type name containing target method")]
        public string TargetType { get; set; } = "SmartConsole.Test.DisposableTestSystem";
        [ArgumentHelp("Method to encapsulate")]
        public string TargetMethod { get; set; } = "ServiceMethod";

        public override TaskResult StartTask()
        {
            try
            {
                var tb = new TaskBuilder(Name, typeof(ConsoleTask))
                        .EncapsulateMethod(Type.GetType(TargetType), TargetMethod)
                        .HasAlias("Hello-World")
                    ;
                   
                if (!string.IsNullOrEmpty(Description))
                    tb.AddTaskHelp(Description);
                else
                    tb.AddTaskHelp($"Call {TargetType}.{TargetMethod} Dynamically");

                return CompileTask(tb);
            }
            catch (Exception ex)
            {
                return TaskResult.Exception(ex);
            }
        }
    }
}