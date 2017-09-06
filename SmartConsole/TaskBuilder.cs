using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.Server;

namespace Bessett.SmartConsole
{
    // Related Reading & Notes
    // https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
    // http://www.tugberkugurlu.com/archive/compiling-c-sharp-code-into-memory-and-executing-it-with-roslyn

    // CodeDOM
    // https://msdn.microsoft.com/en-us/library/system.codedom.compiler.codedomprovider(v=vs.110).aspx

    public static class DynamicTasks
    {
        // See: http://comealive.io/Syntax-Factory-Vs-Parse-Text/
        // https://roslynquoter.azurewebsites.net/
        
        private static List<PortableExecutableReference> ReferenceAssembies = new List<PortableExecutableReference>();

        private const string assemblyName = "DynamicTasks";
        private static Dictionary<string, TaskBuilder> TaskBuilders { get; set; } = new Dictionary<string, TaskBuilder>();  
         
        public static List<Type> Types { get; private set; } = new List<Type>();

        static DynamicTasks()
        {
            // add default assemblies
            ReferenceAssembies.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            ReferenceAssembies.Add(MetadataReference.CreateFromFile(typeof(ConsoleTask).Assembly.Location));
            ReferenceAssembies.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
        }

        public static void AddReference(Assembly assembly)
        {
            ReferenceAssembies.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
        public static void AddReferenceFromType(Type type)
        {
            ReferenceAssembies.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }

        public static TaskBuilder AddConsoleTask(string name) 
        {
            var tb = new TaskBuilder(name, typeof(ConsoleTask));
            TaskBuilders.Add(name, tb);
            return tb;
        }

        public static TaskBuilder AddConsoleTask<T>(string name) where T : ConsoleTask
        {
            Type targetBaseType = typeof(T);
            var tb = new TaskBuilder(name, targetBaseType);
            TaskBuilders.Add(name, tb);
            return tb;
        }
        public static TaskBuilder AddConsoleTask(TaskBuilder tb) 
        {
            TaskBuilders.Add(tb.Name, tb);
            return tb;
        }

        public static List<Diagnostic> CreateDynamic(params Type[] referenceTypes)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var typeBuilder in TaskBuilders.Values)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(typeBuilder.ToCode());
                syntaxTrees.Add(syntaxTree);
            }

            // clear out the builder source
            TaskBuilders.Clear();

            string assemblyName = Path.GetRandomFileName();
            
            var references = 
                referenceTypes
                    .Select(r => MetadataReference.CreateFromFile(r.GetType().Assembly.Location)).ToList();

            ReferenceAssembies.AddRange(references);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: ReferenceAssembies,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = new List<Diagnostic>();

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError || 
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    diagnostics.AddRange(failures);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    // Add applicable generated types to the local ConsoleTask Type cache
                    Types.AddRange(assembly.GetTypes()
                        .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.IsSubclassOf(typeof(ConsoleTask))
                            ));
                }
            }
            return diagnostics;
        }

        public static string BuildString(this List<Diagnostic> failures)
        {
            return
                string.Join("\n", failures.Select(d => $"{d.Id}: {d.GetMessage()} ({d.ToString()})"));
        }
    }

    /// <summary>
    /// Build dynamic console tasks based on a target method 
    /// (or any target source code) 
    /// </summary>
    public sealed class TaskBuilder
    {
        ClassBuilder builder;

        public string Name { get; private set; }
        public Type TargetType { get; private set; }

        /// <summary>
        /// Build a console task for every method in the class
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="typeAlias"></param>
        /// <returns></returns>
        //public TaskBuilder (Type targetType, string typeAlias = "")
        //{
        //    // build a ConsoleTask class for each method (really?)
        //    foreach (var methodInfo in targetType.GetMethods())
        //    {
        //        var taskName = $"{(typeAlias.Length > 0 ? typeAlias : targetType.Name)}_{methodInfo.Name}";
        //        CreateTask(methodInfo, taskName);
        //    }
        //}
        public TaskBuilder UseMethod (Type targetType, string methodName)
        {
            var targetMethod = targetType.GetMethods().FirstOrDefault();
            if (targetMethod != null)
            {
                GeneratePropertiesFromMethod(targetMethod);

                var isDisposable = targetType.GetInterfaces().Contains(typeof(IDisposable));
                
                var varName = "target";
                // build calling signature
                var innerCodeBlock = new CodeBuilder() 
                    .Line($"{varName}.{targetMethod.Name}({string.Join(",", builder.Properties.Select(p => p.Name).ToList())});")
                    .Line($"return TaskResult.Complete();");

                CodeBuilder codeBlock = (isDisposable)
                    ? CodeBuilder.UseDisposePattern(varName, targetType, "", innerCodeBlock)
                    : CodeBuilder.NewObject(varName, targetType, "")
                        .Lines(innerCodeBlock);
                
                var tryCatchBlock = CodeBuilder.UseTryCatchPattern(
                    codeBlock,
                    new CodeBuilder().Line($"return TaskResult.Exception(ex);"));

                StartTaskBody(tryCatchBlock);

                // make sure this Type is referenced
                DynamicTasks.AddReferenceFromType(targetType);
            }
            return this;
        }

        private TaskBuilder GeneratePropertiesFromMethod(MethodInfo method)
        {
            foreach (var parameterInfo in method.GetParameters())
            {
                AddProperty(parameterInfo.Name, parameterInfo.ParameterType);
            }
            return this;
        }
        public TaskBuilder(string name, Type baseType)
        {
            Name = name;
            TargetType = baseType;

            builder =  new ClassBuilder("Dynamics", name,TypeAttributes.Public, baseType)
                .WithUsing("System")
                .WithUsing("Bessett.SmartConsole"); 
        }

        public TaskBuilder WithUsing(string usingNamespace)
        {
            builder.WithUsing(usingNamespace);
            return this;
        }
        public TaskBuilder HasAlias(string aliasName)
        {
            builder = builder.WithAttribute($"TaskAlias(\"{aliasName}\")");
            return this;
        }

        public TaskBuilder AddTaskHelp(string helpText)
        {
            builder.WithAttribute($"TaskHelp(\"{helpText}\")");
            return this;
        }
        public TaskBuilder HasNoConfirmation()
        {
            builder.WithAttribute($"NoConfirmation");
            return this;
        }
        
        public TaskBuilder StartTaskBody(CodeBuilder codeBlock)
        {
            // create the startTask method and body
            builder.WithMethod(
                new MethodBuilder("StartTask", typeof(TaskResult), TypeAttributes.Public, true)
                .WithCodeLines(codeBlock)
                );

            return this;
        }
        public TaskBuilder ConfirmStartBody(CodeBuilder codeLines)
        {
            // create the startTask method and body
            builder.WithMethod(
                new MethodBuilder("ConfirmStart", typeof(bool), TypeAttributes.Public, true)
                .WithCodeLines(codeLines)
            );
            return this;
        }

        public TaskBuilder AddProperty(string name, Type dataType, string argumentHelp = "", bool IsRequired = false)
        {
            var prop = new PropertyBuilder(name, dataType);

            if (argumentHelp != null)
            {
                if (argumentHelp.Length == GetHashCode())
                    prop.WithAttribute("ArgumentHelp");
                else
                    prop.WithAttribute("ArgumentHelp", argumentHelp);

                if (IsRequired)
                    prop.WithAttribute("RequiredArgument");

            }

            builder.WithProperty(prop);

            return this;
        }

        public string ToCode()
        {
            return builder.ToCode();
        }

        public string ToView()
        {
            return builder.ToCode();
        }

        #region Internal Class building support (refactor later)
        public interface ICodeBuilder
        {
            IEnumerable<string> GetLines { get; }
            string ToCode(int indent = 0); 
        }

        public class CodeBuilder: ICodeBuilder
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

            public static CodeBuilder UseDisposePattern(string disposableVar, Type disposableType, string newDeclaration, CodeBuilder codeText)
            {
                var builder = new CodeBuilder();

                builder
                    .Line($"using (var {disposableVar} = new  {disposableType.FullName}({newDeclaration}))")
                    .OpenScope()
                    .Lines(codeText,1)
                    .CloseScope()
                    ;

                return builder;
            }

            public static CodeBuilder MethodSignature(string scope, string name, Type dataType, List<FieldInfo> parameters, bool IsOverride = false)
            {
                var builder = new CodeBuilder();
                var signature = new StringBuilder()
                    .Append($"{scope} {(IsOverride ? "override " : "")}{dataType.FullName} {name} (")
                    .Append(string.Join(",", parameters.Select(p => $"{p.DataType.FullName} {p.Name}")))
                    .Append(")");

                builder.Line(signature.ToString());

                return builder;

            }
            public static CodeBuilder UseTryCatchPattern(CodeBuilder tryBlock, CodeBuilder catchBlock, string exceptionVar = "ex")
            {
                var builder = new CodeBuilder();

                builder
                    .Line("try")
                    .OpenScope()
                    .Lines(tryBlock,1)
                    .CloseScope()
                    .Line($"catch(Exception {exceptionVar})")
                    .OpenScope()
                    .Lines(catchBlock,1)
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
        }

        internal class ClassBuilder: CodeBuilder
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

            public override IEnumerable<string> GetLines {
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
                AttrParams = attrParams.ToList() ;

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

        internal class PropertyBuilder: CodeBuilder
        {
            public List<AttributeBuilder> Attributes { get; private set; } = new List<AttributeBuilder>();
            public string Scope { get; private set; } = "public";
            public string Name { get; private set; }
            public Type DataType { get; private set; }

            public PropertyBuilder(string name, Type dataType, string scope = "public")
            {
                Name = name;
                DataType = dataType;
                Scope = scope;
            }

            public PropertyBuilder WithAttribute(string name, params object[] attrParams)
            {
                Attributes.Add(new AttributeBuilder(name, attrParams));
                return this;
            }

            public override IEnumerable<string> GetLines
            {
                get
                {
                    var signature = new StringBuilder()
                        .Append($"{Scope} {DataType.Name} {Name} ")
                        .Append("{get; set;}\n")
                        .ToString();

                    Lines(Attributes);
                    Line(signature);

                    return base.GetLines;
                }
            }

        }

        public class FieldInfo
        {
            public string Name { get; set; }
            public Type DataType { get; set; }
        }

        internal class MethodBuilder: CodeBuilder
        {
            private List<AttributeBuilder> Attributes { get; set; } = new List<AttributeBuilder>();
            protected List<FieldInfo> Parameters { get; set; } = new List<FieldInfo>();
            protected string Name { get; set; }
            protected Type DataType { get; set; }
            protected bool IsOverride { get; set; }
            protected TypeAttributes TypeAttribute { get; set; }
            protected CodeBuilder BodyBlock { get; set; } = new CodeBuilder();

            //protected List<string> CodeLines { get; set; } = new List<string>();

            public MethodBuilder (string name, Type returnType, TypeAttributes typeAttribute, bool isOverride)
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
                Attributes.Add(new AttributeBuilder(name, attrParams));
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
    }
    
    public static class CodeBuilderExtensions
    {
        public static TaskBuilder.CodeBuilder Foreach(this TaskBuilder.CodeBuilder bldr, string iterator, string enumeriable, TaskBuilder.CodeBuilder block )
        {
            TaskBuilder.CodeBuilder iterationBlock = new TaskBuilder.CodeBuilder()
                .Line($"foreach( var {iterator} in {enumeriable})")
                .OpenScope()
                .Lines(block, 1)
                .CloseScope();

            bldr.Lines(iterationBlock);
            return bldr;
        }
    }


    public class CodeAtom
    {
        
    }
}

