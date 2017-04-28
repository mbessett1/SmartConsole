using System.Collections.Generic;
using System.Xml.Serialization;

namespace Bessett.SmartConsole
{
    public abstract class TaskPackage
    {
        public bool AutoConfirm { get; set; } = false;
        public string Name { get; set; }

        private List<ConsoleTask> _tasks { get; set; }

        public TaskPackage()
        {
            _tasks= new List<ConsoleTask>();
        }

        public TaskPackage(ConsoleTask[] tasks): this()
        {
            AddTaskRange(tasks);
        }

        public TaskPackage(string name): this()
        {
            Name = name;
        }

        public TaskPackage AddTask(ConsoleTask task)
        {
            _tasks.Add(task);
            return this;
        }

        public TaskPackage AddTaskRange(IEnumerable<ConsoleTask> tasks)
        {
            _tasks.AddRange(tasks);
            return this;
        }

        public TaskPackage AddPackage(TaskPackage tasks)
        {
            _tasks.AddRange(tasks._tasks);
            return this;
        }

        public IEnumerable<ConsoleTask> GetTasks()
        {
            foreach (var consoleTask in _tasks)
            {
                consoleTask.Silent = AutoConfirm;
                yield return consoleTask;
            }
        }
    }
}