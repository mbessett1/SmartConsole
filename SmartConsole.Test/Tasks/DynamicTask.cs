using Bessett.SmartConsole;

namespace SmartConsole.Test.Tasks
{
    [NoConfirmation]
    public class DynamicTaskProto : ConsoleTask
    {
        public string Name { get; set; }

        public override TaskResult StartTask()
        {
            return TaskResult.Complete($"{Name} has completed successfully");
        }
    }
}

