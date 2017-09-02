using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    /// <summary>
    /// Specify default value if value not specified on command line
    /// </summary>
    public class DefaultValue : Attribute
    {
        public string Value { get; set; }
        public DefaultValue(string value)
        {
            Value = value;
        }

        internal PropertyInfo PropertyInfo { get; set; }
    }

    /// <summary>
    /// Provide an alternate Command text besides the class name
    /// </summary>
    public class Alias : Attribute
    {
        public string AliasName { get; set; }

        public Alias(string aliasName)
        {
            AliasName = aliasName;
        }
    }

    /// <summary>
    /// Specify default value if value not specified on command line
    /// </summary>
    public class RequiredArgument : Attribute { }

    /// <summary>
    /// Specify default value if value not specified on command line
    /// </summary>
    public class DefaultArgument : Attribute { }

    /// <summary>
    /// Provide help to user if requested, or command not understood
    /// </summary>
    public class ArgumentHelp : Attribute
    {
        const string defaultErrorText = "{0} is required to be specified.";

        /// <summary>
        /// Provide error prompt if marked required, but user did not specify
        /// </summary>
        /// 
        public string ErrorText { get; set; }

        /// <summary>
        /// Provide help text to user
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Specify that this parameter must be specified
        /// </summary>
        public bool IsRequired { get; set; }

        internal PropertyInfo PropertyInfo { get; set; }

        public ArgumentHelp()
            : this("", false, defaultErrorText) { }

        public ArgumentHelp(string helpText)
            : this(helpText, false, defaultErrorText)
        { }

        internal ArgumentHelp(string helpText, bool isRequired, string errorText)
        {
            this.HelpText = helpText;
            this.ErrorText = errorText;
            this.IsRequired = isRequired;
        }


    }

}
