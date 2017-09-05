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
            AddTaskFromMethod();
        }

        internal static List<Type> GetTypes<T>()
        {
            var baseType = typeof(T);

            var internalTasks = Assembly.GetCallingAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract //
                            && t.IsSubclassOf(baseType)
                            );

            var definedTasks = Assembly.GetEntryAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(baseType)
                            );

            var types = internalTasks.ToList();
            types.AddRange(definedTasks.ToList());

            return types;
        }

        internal static void BuildAvailableTasks()
        {
            AllTasks = GetTypes<ConsoleTask>();
        }

        internal static void BuildAvailableTasks(string baseTypeName)
        {
            
            BaseTaskTypeName = baseTypeName;
            Type baseType = Type.GetType(baseTypeName);
            BuildAvailableTasks(baseType);
        }

        internal static void BuildAvailableTasks(Type baseType)
        {
            var internalTasks = Assembly.GetCallingAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract //
                            && t.IsSubclassOf(baseType)
                            );

            var definedTasks = Assembly.GetEntryAssembly().GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(baseType)
                            );

            var tasks = internalTasks.ToList();
            tasks.AddRange(definedTasks.ToList());
            tasks.AddRange(DynamicTasks.Types);
            AllTasks = tasks;
        }

        public static ConsoleTask ToConsoleTask(this string[] args)
        {
            var taskname = args.Length > 0 ? args[0] : "help";

            var taskInstance = GetTaskInstance<ConsoleTask>(taskname, args);
            return taskInstance;
        }

        internal static Type GetTask(string taskName)
        {
            if (taskName != null)
            {
                var taskSpecified = AllTasks.FirstOrDefault(t => t.TaskAlias().ToLower() == taskName.ToLower());
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

        // Build Task from Method (using TypeBuilder)

        internal static void AddTaskFromMethod()
        {
            
        }
    }

    

}
