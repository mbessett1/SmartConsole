using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole.Tasks
{
    [NoConfirmation]
    [TaskHelp("Creates Dynamic ConsoleTask based on encapsulated method")]
    public class BuildTask : ConsoleTask
    {
        [ArgumentHelp("Description for Dynamic task"), RequiredArgument]
        public string Name { get; set; } = "";
        [ArgumentHelp("Compile Task after building")]
        public bool Compile { get; set; } = false;
        [ArgumentHelp("Dynamic task should not confirm start")]
        public bool NoConfirmation { get; set; }
        [ArgumentHelp("Optional Alias for Dynamic task")]
        public string Alias { get; set; } = "Dynamic";
        [ArgumentHelp("Description for Dynamic task")]
        public string Description { get; set; } = "";
        [ArgumentHelp("Type name containing target method")]
        public string TargetType { get; set; } 
        [ArgumentHelp("Method to encapsulate")]
        public string TargetMethod { get; set; }

        [ArgumentHelp("Custom base type. Must derive from ConsoleTask. Default = ConsoleTask")]
        public string BaseTypeName { get; set; } = null;

        public override TaskResult StartTask()
        {
            try
            {
                Type baseType;

                if (!string.IsNullOrEmpty(BaseTypeName))
                {
                    baseType = Type.GetType(BaseTypeName);

                    if (baseType == null)
                        return TaskResult.Failed($"'{baseType} not a valid Type'");

                    if (baseType.IsSubclassOf(typeof(ConsoleTask)))
                        return TaskResult.Failed($"'{baseType} not a valid ConsoleType'");
                }
                else
                {
                    baseType = typeof(ConsoleTask);
                }

                var tb = new TaskBuilder(Name, baseType)
                        .EncapsulateMethod(Type.GetType(TargetType), TargetMethod)
                    ;
                
                if (Description.IsValid())
                    tb.AddTaskHelp(Description);
                else
                    tb.AddTaskHelp($"Call {TargetType}.{TargetMethod} Dynamically");

                if (Alias.IsValid())
                    tb.HasAlias(Alias);

                if (NoConfirmation)
                    tb.HasNoConfirmation();

                if (Compile)
                {
                    DynamicTasks.AddConsoleTask(tb);
                    var failures = DynamicTasks.CreateDynamic();

                    if (!failures.Any())
                        ConsoleProgram.StartTask($"Help {Alias.ValueOrDefault(Name)}");
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
            }
            catch (Exception ex)
            {
                return TaskResult.Exception(ex);
            }

            return TaskResult.Complete();
        }
    }

    static class Validation
    {
        public static bool IsValid(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }
        public static string ValueOrDefault(this string value, string defaultValue)
        {
            return value.IsValid()?value:defaultValue;
        }

    }
}
