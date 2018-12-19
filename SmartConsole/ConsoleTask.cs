using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{

    public class TaskResult
    {
        public bool IsSuccessful { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }

        public static TaskResult Complete(string message = "", int resultCode = 0)
        {
            return new TaskResult() { IsSuccessful = true, Message = message, ResultCode = resultCode };
        }
        public static TaskResult Failed(string message , int resultCode = 0)
        {
            return new TaskResult() { IsSuccessful = false, Message = message, ResultCode = resultCode };
        }
        public static TaskResult Exception(Exception ex)
        {
            return new TaskResult() { IsSuccessful = false, Message = ex.Message, ResultCode = ex.HResult };
        }
    }

    /// <summary>
    /// Base console task available via command line
    /// </summary>
    public  class ConsoleTask
    {
        private string[] _commandArguments;
        private bool _silent = false;

        /// <summary>
        /// Built-in parameter for all tasks to supress confirmation default is NO
        /// </summary>
        [ArgumentHelp("Supress confirmation (default=NO)")]
        public bool Silent
        {
            get
            {
                return _silent || GetType().GetCustomAttributes(true).Any(a => a is NoConfirmation);
            }
            set { _silent = value; }
        }

        /// <summary>
        /// Access to Command arguments
        /// </summary>
        public Dictionary<string, string> CommandArguments { get; private set; } 

        // override when special argument handling required
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

        /// <summary>
        /// Indicator that all required arguments are specified
        /// </summary>
        /// <returns></returns>
        protected bool RequiredArgumentsPresent()
        {
            var isValid = true;

            // check if the task was not created via command.
            //if not, arguments validity check presumed irrelavent/
            if (CommandArguments == null) return true;

            foreach (var property in GetType().GetProperties())
            {
                var paramIsRequired = false;

                var attributes = property.GetCustomAttributes(true);

                foreach (var attrib in attributes)
                {
                    if (attrib is ArgumentHelp)
                    {
                        var attr = (ArgumentHelp)attrib;
                        paramIsRequired = attr.IsRequired;
                        if (paramIsRequired && !CommandArguments.ContainsKey(property.Name))
                        {
                            Console.WriteLine(
                                string.IsNullOrEmpty(attr.ErrorText) ? "{0} is required." : attr.ErrorText
                                , property.Name);
                            isValid = false;
                            break;
                        }
                    }
                    else if (attrib is RequiredArgument)
                    {
                        paramIsRequired = true;
                        if (!CommandArguments.ContainsKey(property.Name))
                        {
                            Console.WriteLine($" ** {property.Name} is a required parameter\n.");
                            isValid = false;
                            break;
                        }
                    }
                }
            }
            return isValid;
        }

        /// <summary>
        /// Provides built-in confirmation before executing by prompting user to press 'y'
        /// override for specialized confirmation 
        /// </summary>
        /// <returns></returns>
        public virtual bool ConfirmStart()
        {
            bool executionPreAuthorized = Silent;
            var executionAuthorized = executionPreAuthorized;
            var isTaskValid = RequiredArgumentsPresent();
            
            // Can't assume the user wants to write the arguments in an override
            //if (!GetType().NoConfirmation())
            //    WriteArguments();
            
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

        /// <summary>
        /// function called upon completion of Start()
        /// </summary>
        public virtual void Complete() { }

        /// <summary> 
        /// Deprecate in favor of StartTask, which returns a result
        /// if StartTask() is not overridden, a call Start() is automatic
        /// and generates a generic artificial successful message.
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Function called once all validation and confirmation complete
        /// </summary>
        /// <returns></returns>
        public virtual TaskResult StartTask()
        {
            // if we get here, this method was not overridden, so call Start() and
            // hope for the best :)
            Start();
            return new TaskResult()
            {
                IsSuccessful = true,
                Message = ""
            };
        }

        /// <summary>
        /// Echo all arguments (that have ArgumentHelp attribute)
        /// </summary>
        protected void WriteArguments()
        {
            var taskArguments = GetType().GetTaskArguments().ToList();
            if (taskArguments.Any())
            {
                var maxFieldNameSize = taskArguments.Max(a => a.PropertyInfo.Name.Length);

                Console.WriteLine("Arguments:");
                foreach (var arg in taskArguments.Where(a => a.PropertyInfo.Name != "Silent"))
                {
                    Console.WriteLine($"  {arg.PropertyInfo.Name.PadRight(maxFieldNameSize)} = {arg.PropertyInfo.GetValue(this)} ");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Returns the reflected help text, or member Name if not specified
        /// </summary>
        public virtual string HelpName {
            get
            {
                var helpText = GetType().GetTaskHelp();
                if (helpText != null)
                {
                    return ( string.IsNullOrEmpty(helpText.Name)) ? GetType().TaskAlias():  helpText.Name ;
                }
                return GetType().TaskAlias();
            }
        }

        /// <summary>
        /// Returns the reflected help description, or blank if not specified
        /// </summary>
        public virtual string HelpDescription
        {
            get
            {
                var helpText = GetType().GetTaskHelp();
                return (helpText != null) ? helpText.Description : "";
            }
        }

        /// <summary>
        /// Returns the reflected help verbose description, or blank if not specified
        /// </summary>
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
