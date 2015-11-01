using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    internal static class TaskLibrary
    {
        public static List<Type> AllTasks { get; set; }
        public static string BaseTaskTypeName { get; private set; }

        static TaskLibrary()
        {
            AllTasks = new List<Type>();

        }

        internal static void BuildAvailableTasks(string baseTypeName)
        {
            BaseTaskTypeName = baseTypeName;

            var internalTasks = Assembly.GetCallingAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.BaseType.Name == BaseTaskTypeName);

            var definedTasks = Assembly.GetEntryAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.BaseType.Name == BaseTaskTypeName);

            var tasks = internalTasks.ToList();
            tasks.AddRange(definedTasks.ToList());

            AllTasks = tasks;
        }

        internal static void BuildAvailableTasks(Type baseType)
        {
            BuildAvailableTasks(baseType.Name);
        }

        internal static Type GetTask(string taskName)
        {
            if (taskName != null)
            {
                var taskSpecified = AllTasks.FirstOrDefault(t => t.Name.ToLower() == taskName.ToLower());
                return taskSpecified;
            }
            return null;
        }

        internal static T GetTaskInstance<T>(Type taskArgument) where T : ConsoleTask
        {
            var target =
                (T) (Activator.CreateInstance(taskArgument));

            return target;
        }

        internal static T GetTaskInstance<T>(string taskName) where T : ConsoleTask
        {                        
            var taskArgument = GetTask(taskName);

            if (taskArgument != null)
            {
                return GetTaskInstance<T>(taskArgument);
            }

            return default(T);

        }

        internal static T GetTaskInstance<T>(string taskName, string [] args) where T : ConsoleTask
        {
            var taskInstance = GetTaskInstance<T>(taskName);

            if (taskInstance != null)
            {
                try
                {
                    taskInstance.Arguments = args;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot evaluate parameters:\n{0}", ex.Message);
                    return GetTaskInstance<T>("help" ,new string[]{"help", taskName} );
                }

                return taskInstance;
            }

            return null;

        }

    }
}
