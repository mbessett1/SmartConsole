using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    /// <summary>
    /// Help for this task
    /// </summary>
    public class TaskHelp : Attribute
    {
        internal string Name { get; set; }
        public string Description { get; set; }
        public string LongDescription { get; set; }
        public TaskHelp() { }
        public TaskHelp(string description) { Description = description; }

    }

    /// <summary>
    /// supress help for this task
    /// </summary>
    public class NoHelp : Attribute { }

    /// <summary>
    /// supress built-in confirmation for this task
    /// </summary>
    public class NoConfirmation : Attribute { }

    /// <summary>
    /// Provide an alternate Command text besides the class name
    /// </summary>
    public class TaskAlias : Attribute
    {
        public string AliasName { get; set; }

        public TaskAlias(string aliasName)
        {
            AliasName = aliasName;
        }
    }


}
