using System.Collections.Generic;
using Bessett.SmartConsole;
using System;

namespace SmartConsole.Test.Tasks
{
    [NoConfirmation]
    [TaskAlias("Test")]
    public class TestContainer : ConsoleTask
    {
        public string Name { get; set; } = "";

        public override TaskResult StartTask()
        {
            var allTests = new Dictionary<string, List<string>>()
            {
                {
                    "dynamic", new List<string>()
                    {
                        "test-dynamic1 -name MyDynamicTask",
                        "test-dynmaic2"
                    }
                },
                {
                    "packages", new List<string>()
                    {
                        "runpackage -name TestPackage",
                    }
                }
            };

            var runAllTests = Name.Length == 0;

            if (!runAllTests && !allTests.ContainsKey(Name))
            {
                Console.WriteLine($"Test '{Name}' not found. Tests defined are:");
                foreach (var test in allTests)
                {
                    Console.WriteLine($"   {Name}");
                }
                return TaskResult.Failed($"No test '{Name}' is defined.");
            }
            else
            {
                foreach (var test in allTests)
                {
                    if (( test.Key == Name) || runAllTests)
                    {
                        Console.WriteLine($"Running Test: {Name}");
                        RunTestGroup(test.Value);
                    }
                }
            }

            return TaskResult.Complete();
        }

        void RunTestGroup(List<string> testGroup)
        {
            foreach (var test in testGroup)
            {
                Console.WriteLine($"   Command: {test}");
                ConsoleProgram.StartTask(test);
            }

        }
    }
}