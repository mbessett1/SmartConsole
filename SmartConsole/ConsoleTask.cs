using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{

    public class ConsoleTask
    {
        private string[] _commandArguments;
        private bool _silent = false;

        [ArgumentHelp("Supress confirmation (default=NO)")]
        public bool Silent
        {
            get
            {
                return _silent || GetType().GetCustomAttributes(true).Any(a => a is NoConfirmation);
            }
            set { _silent = value; }
        }

        public Dictionary<string, string> CommandArguments { get; private set; }

        public virtual string[] Arguments
        {
            get
            {
                return _commandArguments;
            }
            set
            {
                _commandArguments = value;
                CommandArguments = Helpers.ParseCommandArguments(_commandArguments);
                Helpers.InjectObject(this, CommandArguments);
                var defaultArgument = GetType().DefaultArgument();
                if (_commandArguments.Length>1 && defaultArgument != null && defaultArgument.GetValue(this) == null)
                {
                    Helpers.InjectPropertyValue(this, _commandArguments[1], defaultArgument);
                }
            }
        }

        #region construction
        public ConsoleTask() 
        {
            // setup default values
            foreach (var arg in GetType().GetArgumentsWithDefaultValues())
            {
                Helpers.InjectPropertyValue(this, arg.Value, arg.PropertyInfo );
            }
        }

        protected ConsoleTask(string[] defaultArguments):this()
        {
            CommandArguments = Helpers.ParseCommandArguments(defaultArguments);
        }

        protected ConsoleTask(Dictionary<string, string> commandArguments):this()
        {
            CommandArguments = commandArguments;
        }
        #endregion

        protected bool RequiredArgumentsPresent()
        {
            var isValid = true;

            foreach (var property in GetType().GetProperties())
            {
                var attributes = property.GetCustomAttributes(true);

                foreach (var attrib in attributes)
                {
                    if (attrib is ArgumentHelp)
                    {
                        var attr = (ArgumentHelp)attrib;
                        if (attr.IsRequired)
                            if (!CommandArguments.ContainsKey(property.Name))
                            {
                                Console.WriteLine(attr.ErrorText, property.Name);
                                isValid = false;
                            }
                    }
                }
            }
            return isValid;
        }

        public virtual bool ConfirmStart()
        {
            bool executionPreAuthorized = Silent;
            var executionAuthorized = executionPreAuthorized;
            var isTaskValid = RequiredArgumentsPresent();
            
            if (!GetType().NoConfirmation())
                WriteArguments();
            
            if (isTaskValid)
            {
                if (!executionPreAuthorized)
                {
                    var consoleInfo = ConsoleProgram.ConsolePrompt();
                    executionAuthorized = (consoleInfo.KeyChar == 'Y' || consoleInfo.KeyChar == 'y');
                }
            }

            return executionAuthorized && isTaskValid;
        }

        public virtual void Complete() {}

        public virtual void Start() { }

        public void WriteArguments()
        {
            foreach (var arg in GetType().GetTaskArguments().Where(a=> a.PropertyInfo.Name != "Silent"))
            {
                Console.WriteLine("  {0} = {1} ", arg.PropertyInfo.Name, arg.PropertyInfo.GetValue(this));
            }
        }

        public virtual string HelpName {
            get
            {
                var helpText = GetType().GetTaskHelp();
                if (helpText != null)
                {
                    return ( string.IsNullOrEmpty(helpText.Name)) ? GetType().Name:  helpText.Name ;
                }
                return GetType().Name;
            }
        }

        public virtual string HelpDescription
        {
            get
            {
                var helpText = GetType().GetTaskHelp();
                return (helpText != null) ? helpText.Description : "";
            }
        }
        public virtual string HelpLongDescription
        {
            get
            {
                var helpText = GetType().GetTaskHelp();
                return (helpText != null) ? helpText.LongDescription : "";
            }
        }
    }
}
