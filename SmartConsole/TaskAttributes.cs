using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    public class TaskHelp : Attribute
    {
        internal string Name { get; set; }
        public string Description { get; set; }
        public string LongDescription { get; set; }
        public TaskHelp() { }
        public TaskHelp(string description) { Description = description; }

    }

    public class NoHelp : Attribute { }

    public class NoConfirmation : Attribute { }

}
