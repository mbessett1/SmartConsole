using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Bessett.SmartConsole
{
    public static class DynamicTasks
    {
        // See: http://comealive.io/Syntax-Factory-Vs-Parse-Text/
        // https://roslynquoter.azurewebsites.net/
        
        private static List<PortableExecutableReference> ReferenceAssembies = new List<PortableExecutableReference>();

        private const string assemblyName = "DynamicConsoleTasks";
        private static Dictionary<string, TaskBuilder> TaskBuilders { get; set; } = new Dictionary<string, TaskBuilder>();  

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

            return AddConsoleTask(tb);
        }

        public static TaskBuilder AddConsoleTask<T>(string name) where T : ConsoleTask
        {
            Type targetBaseType = typeof(T);
            var tb = new TaskBuilder(name, targetBaseType);

            return AddConsoleTask(tb);
        }
        public static TaskBuilder AddConsoleTask(TaskBuilder tb)
        {
            if (TaskBuilders.ContainsKey(tb.Name))
                TaskBuilders[tb.Name] = tb;
            else
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
                    TaskLibrary.RegisterTasks(assembly);
                }
            }
            return diagnostics;
        }

        public static string Expand(this List<Diagnostic> failures)
        {
            return
                string.Join("\n", failures.Select(d => $"{d.Id}: {d.GetMessage()} ({d.ToString()})"));
        }
    }
}