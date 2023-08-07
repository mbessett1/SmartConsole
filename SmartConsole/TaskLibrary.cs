using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    public static class TaskLibrary
    {
        internal static List<Assembly> AssemblyRegistry = new List<Assembly>();
        public static List<Type> AllTasks { get; private set; }

        public static string BaseTaskTypeName { get; private set; }

        static TaskLibrary()
        {
            AllTasks = new List<Type>();
            
            // add default assemblies
            RegisterAssembly(Assembly.GetCallingAssembly());
            RegisterAssembly(Assembly.GetEntryAssembly());

        }

        public static void RegisterAssembly(Assembly targetAssembly)
        {
            AssemblyRegistry.Add(targetAssembly);
            RegisterTasks(targetAssembly);
        }

        public static void RegisterTasks( Assembly targetAssembly)
        {
            try
            {
                var definedTasks = targetAssembly.GetTypes()
                    .Where(t => t.IsClass
                                && !t.IsAbstract
                                && t.IsSubclassOf(typeof(ConsoleTask))
                                );
                AllTasks.AddRange(definedTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        internal static List<Type> GetTypes<T>()
        {
            var baseType = typeof(T);

            var types = new List<Type>();

            foreach (var assembly in AssemblyRegistry)
            {
                types.AddRange(
                    assembly.GetTypes()
                        .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(baseType)
                    ));    
            }

            return types;
        }

        //internal static void BuildAvailableTasks()
        //{
        //    AllTasks = GetTypes<ConsoleTask>();
        //}

        //internal static void BuildAvailableTasks(string baseTypeName)
        //{
        //    BaseTaskTypeName = baseTypeName;
        //    Type baseType = Type.GetType(baseTypeName);
        //    BuildAvailableTasks(baseType);
        //}

        //internal static void BuildAvailableTasks(Type baseType)
        //{
        //    AllTasks.Clear();
        //    foreach (var assembly in AssemblyRegistry)
        //    {
        //        AllTasks.AddRange(
        //            assembly.GetTypes()
        //                .Where(t => t.IsClass
        //                            && !t.IsAbstract
        //                            && t.IsSubclassOf(baseType)
        //                )
        //        );
        //    }

        //}


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
                    return null; // GetTaskInstance<T>("help" ,new string[]{"help", taskName} );
                }

                return taskInstance;
            }

            Console.WriteLine($"Task Not found [{taskName}]\n");
            return null;

        }
    }

    
    public static class Extensions
    {
        public static ConsoleTask ToConsoleTask(this string[] args)
        {
            var taskname = args.Length > 0 ? args[0] : "help";

            var taskInstance = TaskLibrary.GetTaskInstance<ConsoleTask>(taskname, args);
            return taskInstance;
        }
    }
}
