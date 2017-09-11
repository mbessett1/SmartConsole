using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole.CodeBuilderPOC
{
    // Notes
    // seriously look into this model: https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
    // http://www.tugberkugurlu.com/archive/compiling-c-sharp-code-into-memory-and-executing-it-with-roslyn

    // CodeDOM
    // https://msdn.microsoft.com/en-us/library/system.codedom.compiler.codedomprovider(v=vs.110).aspx


    #region Internal Class building support (refactor later)

    internal interface ICodeBuilder
    {
        IEnumerable<string> GetLines { get; }
        string ToCode(int indent = 0);
    }

    internal class CodeBuilder : ICodeBuilder
    {
        const char TabChar = ' ';
        const int TabLevelSize = 3;

        protected virtual List<string> CodeLines { get; set; } = new List<string>();

        public virtual IEnumerable<string> GetLines
        {
            get { return CodeLines.AsEnumerable(); }
        }

        public CodeBuilder Line(string codeLine, int indent = 0)
        {
            CodeLines.Add($"{Tab(indent)}{codeLine}");
            return this;
        }

        public CodeBuilder OpenScope()
        {
            CodeLines.Add("{");
            return this;
        }

        public CodeBuilder CloseScope()
        {
            CodeLines.Add("}");
            return this;
        }

        public CodeBuilder BlankLine()
        {
            CodeLines.Add("");
            return this;
        }

        public CodeBuilder Lines(IEnumerable<ICodeBuilder> codeLines, int indent = 0)
        {
            foreach (ICodeBuilder codeBuilder in codeLines)
            {
                Lines(codeBuilder, indent);
            }
            return this;
        }

        public CodeBuilder Lines(List<string> codeLines, int indent = 0)
        {
            CodeLines.AddRange(codeLines.Select(l => $"{Tab(indent)}{l}"));
            return this;
        }

        public CodeBuilder Lines(ICodeBuilder codeLines, int indent = 0)
        {
            CodeLines.AddRange(codeLines.GetLines.Select(l => $"{Tab(indent)}{l}"));
            return this;
        }

        public static string Tab(int level)
        {
            return new string(TabChar, level * TabLevelSize);
        }

        public static CodeBuilder Namespace(string namespaceName, CodeBuilder codeBlock)
        {
            var result = new CodeBuilder()
                .Line($"namespace {namespaceName}")
                .OpenScope()
                .Lines(codeBlock, 1)
                .CloseScope();

            return result;
        }

        public static CodeBuilder UseDisposePattern(string disposableVar, Type disposableType, string newDeclaration,
            CodeBuilder codeText)
        {
            var builder = new CodeBuilder();

            builder
                .Line($"using (var {disposableVar} = new  {disposableType.FullName}({newDeclaration}))")
                .OpenScope()
                .Lines(codeText, 1)
                .CloseScope()
                ;

            return builder;
        }

        public static CodeBuilder MethodSignature(string scope, string name, Type dataType, List<FieldInfo> parameters,
            bool IsOverride = false)
        {
            var builder = new CodeBuilder();
            var signature = new StringBuilder()
                .Append($"{scope} {(IsOverride ? "override " : "")}{dataType.FullName} {name} (")
                .Append(string.Join(",", parameters.Select(p => $"{p.DataType.FullName} {p.Name}")))
                .Append(")");

            builder.Line(signature.ToString());

            return builder;

        }

        public static CodeBuilder UseTryCatchPattern(CodeBuilder tryBlock, CodeBuilder catchBlock,
            string exceptionVar = "ex")
        {
            var builder = new CodeBuilder();

            builder
                .Line("try")
                .OpenScope()
                .Lines(tryBlock, 1)
                .CloseScope()
                .Line($"catch(Exception {exceptionVar})")
                .OpenScope()
                .Lines(catchBlock, 1)
                .CloseScope()
                ;

            return builder;
        }

        public static CodeBuilder NewObject(string varName, Type targetType, string newDeclaration)
        {
            var builder = new CodeBuilder()
                .Line($"var {varName} = new {targetType.FullName}();");
            return builder;

        }

        public virtual string ToCode(int indentLevel = 0)
        {
            return $"{Tab(indentLevel)}{string.Join($"\n{Tab(indentLevel)}", GetLines)}";
        }

        public virtual string ToView(int indentLevel = 0)
        {
            var result = new StringBuilder();
            var index = 0;

            foreach (var line in GetLines)
            {
                index++;
                result.AppendLine($"{index.ToString("D5")} {line}");
            }
            return result.ToString();
        }
    }


    internal class ClassBuilder : CodeBuilder
    {
        private CodeBuilder Usings { get; set; } = new CodeBuilder();
        private List<AttributeBuilder> Attributes { get; set; } = new List<AttributeBuilder>();
        protected List<MethodBuilder> Methods { get; set; } = new List<MethodBuilder>();
        public List<PropertyBuilder> Properties { get; private set; } = new List<PropertyBuilder>();
        private string Name { get; set; }
        private string NamespaceDeclaration { get; set; }
        private Type BaseClass { get; set; }
        private TypeAttributes TypeAttribute { get; set; }

        public ClassBuilder(string namespaceName, string name, TypeAttributes typeAttribute, Type baseType = null)
        {
            NamespaceDeclaration = namespaceName;
            Name = name;
            TypeAttribute = typeAttribute;
            BaseClass = baseType;
        }

        public ClassBuilder WithMethod(MethodBuilder method)
        {
            Methods.Add(method);
            return this;
        }

        public ClassBuilder WithProperty(PropertyBuilder property)
        {
            Properties.Add(property);
            return this;
        }

        public ClassBuilder WithUsing(string usingDeclaration)
        {
            Usings.Line($"using {usingDeclaration};");
            return this;
        }

        public ClassBuilder WithAttribute(string name, params object[] attrParams)
        {
            Attributes.Add(new AttributeBuilder(name, attrParams));
            return this;
        }

        public override IEnumerable<string> GetLines
        {
            get
            {
                CodeLines.Clear();

                Lines(Usings);
                Lines(CodeBuilder.Namespace(
                    NamespaceDeclaration,
                    new CodeBuilder()
                        .Lines(Attributes)
                        .Line($"public class {Name} : {BaseClass}")
                        .OpenScope()
                        .Lines(Properties, 1)
                        .Lines(Methods, 1)
                        .CloseScope()
                ));

                return CodeLines.AsEnumerable();
            }
        }

    }

    internal class AttributeBuilder : CodeBuilder
    {
        public string Name { get; set; }
        public List<object> AttrParams = new List<object>();

        public AttributeBuilder(string name, params object[] attrParams)
        {
            Name = name;
            AttrParams = attrParams.ToList();

            BuildAttruibuteCode();
        }

        private void BuildAttruibuteCode()
        {
            List<string> attrParams = new List<string>();
            string attrParamsExpanded = "";

            if (AttrParams.Any())
            {
                foreach (var attrParam in AttrParams)
                {
                    if (attrParam.GetType() == typeof(string))
                    {
                        attrParams.Add($"\"{attrParam}\"");
                    }
                    else if (attrParam.GetType() == typeof(bool))
                    {
                        attrParams.Add($"{attrParam.ToString().ToLower()}");
                    }
                    else
                    {
                        attrParams.Add($"{attrParam}");
                    }
                }
                attrParamsExpanded = $"({string.Join(",", attrParams)})";
            }

            var attribute = $"[{Name}{attrParamsExpanded}]";

            CodeLines.Add(attribute);
        }
    }

    internal class PropertyBuilder : CodeBuilder
    {
        private List<AttributeBuilder> Attributes { get; set; } = new List<AttributeBuilder>();
        public string Scope { get; private set; } = "public";
        public string Name { get; private set; } 
        public Type DataType { get; private set; }
        public object DefaultValue { get; private set; }

        private bool HasDefault{ get { return DefaultValue != null; }}
        

        public PropertyBuilder(string name, Type dataType, string scope = "public")
        {
            Name = name;
            DataType = dataType;
            Scope = scope;
        }

        public PropertyBuilder WithAttribute(string name, params object[] attrParams)
        {
            Func<AttributeBuilder, bool> nameMatch = (a => a.Name.Equals(name,StringComparison.InvariantCultureIgnoreCase));

            if (Attributes.Any(nameMatch))
            {
                Attributes.FirstOrDefault(nameMatch)
                    .AttrParams = attrParams.ToList();
            }
            else
            {
                Attributes.Add(new AttributeBuilder(name, attrParams));
            }

            return this;
        }

        internal PropertyBuilder WithDefaultValue(object defaultValue)
        {
            DefaultValue = defaultValue;
            return this;
        }

        public override IEnumerable<string> GetLines
        {
            get
            {
                var defaultSpec = HasDefault
                    ? DefaultValue is string 
                        ? $" = \"{DefaultValue}\";"
                        : $" = {DefaultValue};"
                    : "";

                var signature = new StringBuilder()
                    .Append($"{Scope} {DataType.Name} {Name} ")
                    .Append("{get; set;}")
                    .Append( defaultSpec )
                    .ToString();

                return new CodeBuilder()
                    .Lines(Attributes)
                    .Line(signature)
                    .BlankLine()
                    .GetLines;

            }
        }

    }

    internal class FieldInfo
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
    }

    internal class MethodBuilder : CodeBuilder
    {
        private List<AttributeBuilder> Attributes { get; set; } = new List<AttributeBuilder>();
        protected List<FieldInfo> Parameters { get; set; } = new List<FieldInfo>();
        protected string Name { get; set; }
        protected Type DataType { get; set; }
        protected bool IsOverride { get; set; }
        protected TypeAttributes TypeAttribute { get; set; }
        protected CodeBuilder BodyBlock { get; set; } = new CodeBuilder();

        //protected List<string> CodeLines { get; set; } = new List<string>();

        public MethodBuilder(string name, Type returnType, TypeAttributes typeAttribute, bool isOverride)
        {
            Name = name;
            DataType = returnType;
            TypeAttribute = typeAttribute;
            IsOverride = isOverride;
        }

        public MethodBuilder WithParameter(FieldInfo parameter)
        {
            Parameters.Add(parameter);
            return this;
        }
        public MethodBuilder WithAttribute(string name, params object[] attrParams)
        {
            Func<AttributeBuilder, bool> nameMatch = (a => a.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (Attributes.Any(nameMatch))
            {
                Attributes.FirstOrDefault(nameMatch)
                    .AttrParams = attrParams.ToList();
            }
            else
            {
                Attributes.Add(new AttributeBuilder(name, attrParams));
            }

            return this;
        }

        public MethodBuilder WithCodeStatement(string codeLine)
        {
            BodyBlock.Line(codeLine);
            return this;
        }

        public MethodBuilder WithCodeLines(CodeBuilder codeBlock)
        {
            BodyBlock.Lines(codeBlock);
            return this;
        }

        public override IEnumerable<string> GetLines
        {
            get
            {
                CodeBuilder codeBuilder = new CodeBuilder();

                // build signature
                codeBuilder
                    .Lines(Attributes)
                    .Lines(CodeBuilder.MethodSignature("public", Name, DataType, Parameters, IsOverride))
                    .OpenScope()
                    .Lines(BodyBlock, 1)
                    .CloseScope();

                return codeBuilder.GetLines;
            }
        }
    }
    #endregion
    
    #region Interface POC
    internal class GenericTypeReference : CodeBuilder
    {
        public string RefereneName { get; set; }
        public string Constraint { get; set; }
    }

    internal class InterfaceBuilder : CodeBuilder
    {
        public string Name { get; set; }
        public string[] ParentInterfaces { get; set; }


        public List<PropertyBuilder> Properties { get; set; } = new List<PropertyBuilder>();
        public List<MethodBuilder> Methods { get; set; } = new List<MethodBuilder>();

        public override IEnumerable<string> GetLines
        {
            get { return base.GetLines; }
        }
    }

    #endregion

    internal static class CodeBuilderExtensions
    {
        public static CodeBuilder Foreach(this CodeBuilder bldr, string iterator, string enumeriable, CodeBuilder block)
        {
            CodeBuilder iterationBlock = new CodeBuilder()
                .Line($"foreach( var {iterator} in {enumeriable})")
                .OpenScope()
                .Lines(block, 1)
                .CloseScope();

            bldr.Lines(iterationBlock);
            return bldr;
        }
        public static CodeBuilder DoLoop(this CodeBuilder bldr, string continueExpression, string enumeriable, CodeBuilder block)
        {
            CodeBuilder iterationBlock = new CodeBuilder()
                .Line($"do")
                .OpenScope()
                .Lines(block, 1)
                .CloseScope()
                .Line($"while ({continueExpression});")
                ;

            bldr.Lines(iterationBlock);
            return bldr;
        }
        public static CodeBuilder ForLoop(this CodeBuilder bldr,
            string startExpression, string continueExpression, string incrementExpression,
            CodeBuilder innerCodeBlock)
        {
            CodeBuilder iterationBlock = new CodeBuilder()
                .Line($"for ({startExpression}; {continueExpression}; {incrementExpression} )")
                .OpenScope()
                .Lines(innerCodeBlock, 1)
                .CloseScope()
                ;

            bldr.Lines(iterationBlock);
            return bldr;
        }
    }

}
