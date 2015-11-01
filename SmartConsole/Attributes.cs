using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole
{
    public class DefaultValue : Attribute
    {
        public string Value { get; set; }
        public DefaultValue(string value)
        {
            Value = value;
        }

        internal PropertyInfo PropertyInfo { get; set; }
    }

    public class RequiredArgument : Attribute { }

    public class DefaultArgument : Attribute { }

    public class ArgumentHelp : Attribute
    {
        const string defaultErrorText = "{0} is required to be specified.";

        public string ErrorText { get; set; }
        public string HelpText { get; set; }
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
