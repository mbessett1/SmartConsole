using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    internal static class TypeExtensions
    {
        public static TaskHelp GetTaskHelp(this Type Member)
        {
            var taskHelpAttr = Member.GetCustomAttributes(true).Where(a => a is TaskHelp).FirstOrDefault();
            if (taskHelpAttr != null)
            {
                var taskHelp = (TaskHelp)taskHelpAttr;
                return taskHelp;
            }
            return null;
        }

        public static bool NoConfirmation(this Type task)
        {
            return task.GetCustomAttributes(true).Any(a => a is NoConfirmation);
        }

        public static bool NoHelp(this Type task)
        {
            return task.GetCustomAttributes(true).Any(a => a is NoHelp);
        }

        public static PropertyInfo DefaultArgument(this Type task)
        {
            var result = task.GetProperties()
                .Where(p => p.GetCustomAttributes(true).Any(a => a is DefaultArgument))
                .FirstOrDefault();
            return result;
        }

        public static IEnumerable<DefaultValue> GetArgumentsWithDefaultValues(this Type task)
        {

            foreach (var property in task.GetProperties().Where(p => p.GetCustomAttributes(true).Any(a => a is DefaultValue)))
            {
                var attribute = property.GetCustomAttributes(true).FirstOrDefault(a => a is DefaultValue);
                var argument = (DefaultValue)attribute;

                argument.PropertyInfo = property;

                yield return argument;
            }

        }

        public static IEnumerable<ArgumentHelp> GetTaskArguments(this Type task)
        {
            var silentOverriden = task.NoConfirmation();

            foreach (var property in task.GetProperties().Where(p => p.GetCustomAttributes(true).Any(a => a is ArgumentHelp)))
            {
                var attribute = property.GetCustomAttributes(true).FirstOrDefault(a => a is ArgumentHelp);
                var argument = (ArgumentHelp)attribute;

                argument.PropertyInfo = property;
                
                if (property.Name != "Silent" || !silentOverriden )
                    yield return argument;
            }

        }

        public static string TaskAlias(this Type task)
        {
            var taskAliasAttr = task.GetCustomAttributes(true)
                .Where(a => a is TaskAlias).FirstOrDefault();

            if (taskAliasAttr != null)
            {
                var taskAlias = (TaskAlias) taskAliasAttr;
                return taskAlias.AliasName;
            }
            return task.Name;
        }
    }
}
