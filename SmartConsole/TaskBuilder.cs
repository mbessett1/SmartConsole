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
        private static List<PortableExecutableReference> ReferenceAssembies = new List<PortableExecutableReference>();

        private const string assemblyName = "DynamicTasks";
        private static List<TaskBuilder> TaskBuilders { get; set; } = new List<TaskBuilder>();
         
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
            TaskBuilders.Add(tb);
            return tb;
        }

        public static TaskBuilder AddConsoleTask<T>(string name) where T : ConsoleTask
        {
            Type targetBaseType = typeof(T);
            var tb = new TaskBuilder(name, targetBaseType);
            TaskBuilders.Add(tb);
            return tb;
        }
        public static void CreateDynamic(params Type[] referenceTypes)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var typeBuilder in TaskBuilders)
            {
                syntaxTrees.Add( CSharpSyntaxTree.ParseText(typeBuilder.ToCode()) );
            }

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


            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} ({diagnostic.ToString()})");
                    }
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
        }
    }

    /// <summary>
    /// Build dynamic console tasks based on a target method 
    /// (or any target source code) 
    /// </summary>
    public sealed class TaskBuilder
    {
        ClassBuilder builder;

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
                var codeBlock = new CodeBuilder()
                    .Line($"var {varName} = new {targetType.FullName}();")
                    .Line($"{varName}.{targetMethod.Name}({string.Join(",", builder.Properties.Select(p => p.Name).ToList())});")
                    .Line($"return TaskResult.Complete();");
                
                StartTaskBody(codeBlock );

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
            builder = builder.WithMethod(
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

        private void BuildFromMethod(MethodInfo methodInfo, string taskName, string taskAlias = "")
        {
            //ClassBuilder classBuilder = new ClassBuilder(
            //            "DynmaicTasks",
            //            taskName,
            //            TypeAttributes.Public,
            //            typeof(ConsoleTask)
            //        )
            //        .WithAttribute("TaskHelp")
            //    ;
           

            //// Add Task attributes
            //// Add properties for each mamber argument
            //foreach (var arg in methodInfo.GetParameters())
            //{
            //classBuilder = classBuilder
            //    .WithMethod(new MethodBuilder( )
            //    .WithParameter()
            //    classBuilder. (typeBuilder, arg.Name, arg.ParameterType);
            //    )
            //    ;
            //}
            //// build ovveride for StartTask, ConfirmStart
            //// place any result into the TaskResult.Complete

            //// this StartTask must 
            //// 1. Instantiate the container/parent Type
            //// 2. execute the method of the parent type 

            //var parentType = methodInfo.DeclaringType;

            //Add(typeBuilder);
            ////Microsoft.CodeAnalysis.CSharp.SyntaxFactory.PropertyDeclaration()
        }

        #region Internal Class building support (refactor later)

        public class CodeBuilder
        {
            private readonly char TabChar = ' ';
            private readonly int TabLevelSize = 3;

            private List<string> CodeLines = new List<string>();

            public CodeBuilder Line(string codeLine)
            {
                CodeLines.Add(codeLine);
                return this;
            }

            public CodeBuilder Lines(CodeBuilder codeLines)
            {
                CodeLines.AddRange(codeLines.CodeLines);
                return this;
            }

            protected string Tab(int level)
            {
                return new string(TabChar, level * TabLevelSize);
            }

            public CodeBuilder DisposePattern(string iterationVariable, Type disposableType, string newDeclaration, CodeBuilder codeText)
            {
                var builder = new CodeBuilder();

                builder
                    .Line($"using (var {iterationVariable} = new  {disposableType.Name}({newDeclaration}))")
                    .Line("{")
                    .Line(codeText.ToCode(1))
                    .Line("}")
                    ;

                return builder;
            }

            public CodeBuilder TryCatchPattern(CodeBuilder tryBlock, CodeBuilder catchBlock)
            {
                var builder = new CodeBuilder();

                builder
                    .Line("try")
                    .Line("{")
                    .Line(tryBlock.ToCode(1))
                    .Line("}")
                    .Line("catch(Exception ex)")
                    .Line("{")
                    .Line(catchBlock.ToCode(1))
                    .Line("}")
                    ;

                return builder;
            }

            public virtual string ToCode(int indentLevel = 0)
            {
                return $"{Tab(indentLevel)}{string.Join("\n", CodeLines)}";
            }
        }

        internal class ClassBuilder : CodeBuilder
        {
            private List<string> Usings { get; set; } = new List<string>();
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
                Usings.Add(usingDeclaration);
                return this;
            }
            public ClassBuilder WithAttribute(string name, params object[] attrParams)
            {
                Attributes.Add(new AttributeBuilder(name, attrParams));
                return this;
            }

            public override string ToCode(int indentLevel = 1)
            {
                string t = Tab(indentLevel);

                StringBuilder codeBuilder = new StringBuilder();
                foreach (var usingValue in Usings)
                {
                    codeBuilder.AppendLine($"using {usingValue};");
                }

                codeBuilder.AppendLine($"namespace {NamespaceDeclaration}");
                codeBuilder.AppendLine($"{{");

                foreach (var attr in Attributes)
                {
                    codeBuilder.AppendLine(attr.ToCode(indentLevel));
                }

                codeBuilder.AppendLine($"{t}public class {Name} : {BaseClass}");

                codeBuilder.AppendLine($"{t}{{");

                foreach (var propertyBuilder in Properties)
                {
                    codeBuilder.AppendLine(propertyBuilder.ToCode(indentLevel + 1));
                }

                foreach (var methodBuilder in Methods)
                {
                    codeBuilder.AppendLine(methodBuilder.ToCode(indentLevel + 1));
                }
                
                codeBuilder.AppendLine($"{t}}}");
                codeBuilder.AppendLine("}");

                return codeBuilder.ToString();
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
            }

            public override string ToCode(int indentLevel = 0)
            {
                List<string> attrParams = new List<string>();
                string attrParamsExpanded = "";

                if (AttrParams.Any())
                {
                    foreach (var attrParam in AttrParams)
                    {
                        if (attrParam.GetType() == typeof(string))
                        {
                            attrParams.Add( $"\"{attrParam}\"");
                        }
                        else
                        {
                            attrParams.Add($"{attrParam}");
                        }
                    }
                    attrParamsExpanded = $"({string.Join(",", attrParams)})";
                }
                return $"{Tab(indentLevel)}[{Name}{attrParamsExpanded}]";

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
            public override string ToCode(int indentLevel = 0)
            {
                string t = Tab(indentLevel);
                StringBuilder codeBuilder = new StringBuilder();
                foreach (var attr in Attributes)
                {
                    codeBuilder.AppendLine(attr.ToCode(indentLevel));
                }

                codeBuilder.Append($"{t}{Scope} {DataType.Name} {Name} ");
                codeBuilder.Append("{get; set;}");
                codeBuilder.AppendLine();
                return codeBuilder.ToString();
            }
        }

        //internal class StartTaskMethod : MethodBuilder
        //{
        //    public StartTaskMethod(MethodInfo method) : base("StartTask", typeof(TaskResult), TypeAttributes.Public, true)
        //    {
        //        WithAttribute("TaskHelp");
        //
        //        // build the method body
        //        foreach (var arg in method.GetParameters())
        //        {
        //            classBuilder = classBuilder
        //                .WithMethod(new MethodBuilder()
        //                .WithParameter()
        //                classBuilder. (typeBuilder, arg.Name, arg.ParameterType);
        //        )
        //            ;
        //        }
        //    }

        //    public override string ToCode()
        //    {
        //        return base.ToCode();
        //    }
        //}

        internal class FieldInfo
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

            public override string ToCode(int indentLevel = 0)
            {
                string t =Tab(indentLevel);

                CodeBuilder codeBuilder = new CodeBuilder();

                foreach (var attr in Attributes)
                {
                    codeBuilder.Line(attr.ToCode(indentLevel));
                }

                // build signature
                codeBuilder.Line($"{t}public {(IsOverride ? "override " : "")}{DataType.Name} {Name} (");

                //insert parameters
                codeBuilder.Line(string.Join(",", Parameters.Select(p=>  $"{p.DataType.Name} {p.Name}") ));

                codeBuilder.Line(")");

                // body
                codeBuilder.Line($"{t}{{");

                //todo - fix codelines
                codeBuilder.Lines(BodyBlock);

                codeBuilder.Line($"{t}}}");

                return codeBuilder.ToString();
            }
        }
        #endregion

    }

}
