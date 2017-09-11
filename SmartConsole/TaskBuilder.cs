using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
//using System.Reflection.Emit;
using Bessett.SmartConsole.CodeBuilderPOC;
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

    /// <summary>
    /// Build dynamic console tasks based on a target method 
    /// (or any target source code) 
    /// </summary>
    public sealed class TaskBuilder
    {
        ClassBuilder builder;

        public string Name { get; private set; }
        public Type TargetType { get; private set; }

        public TaskBuilder EncapsulateMethod (Type targetType, string methodName)
        {
            var targetMethod = targetType.GetMethods().FirstOrDefault(m=> m.Name.ToLower() == methodName.ToLower() );
            if (targetMethod != null)
            {
                GeneratePropertiesFromMethod(targetMethod);

                var isDisposable = targetType.GetInterfaces().Contains(typeof(IDisposable));
                
                var varName = "target";
                // build calling signature
                var innerCodeBlock = new CodeBuilder() 
                    .Line($"{varName}.{targetMethod.Name}({string.Join(", ", builder.Properties.Select(p => p.Name).ToList())});")
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
                var titleCaseName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parameterInfo.Name);
                AddProperty(titleCaseName, 
                    parameterInfo.ParameterType,
                    "", 
                    parameterInfo.HasDefaultValue 
                        ? parameterInfo.DefaultValue 
                        : null, 
                    !parameterInfo.HasDefaultValue 
                    );
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

        public TaskBuilder AddProperty(string name, Type dataType, string argumentHelp = "", object defaultValue = null, bool IsRequired = false)
        {
            var prop = new PropertyBuilder(name, dataType);

            if (argumentHelp != null)
            {
                prop.WithAttribute("ArgumentHelp", argumentHelp, IsRequired, "");

            }

            if (IsRequired)
                prop.WithAttribute("RequiredArgument");

            if (defaultValue != null)
                prop.WithDefaultValue(defaultValue);

            builder.WithProperty(prop);

            return this;
        }

        public TaskBuilder StartTaskBody(List<string> codeLines)
        {
            return StartTaskBody(new CodeBuilder().Lines(codeLines));
        }
        public TaskBuilder ConfirmStartBody(List<string> codeLines)
        {
            return ConfirmStartBody(new CodeBuilder().Lines(codeLines));
        }

        internal TaskBuilder StartTaskBody(CodeBuilder codeBlock)
        {
            // create the startTask method and body
            builder
                .WithMethod(new MethodBuilder("StartTask", typeof(TaskResult), TypeAttributes.Public, true)
                .WithCodeLines(codeBlock)
                );

            return this;
        }
        internal TaskBuilder ConfirmStartBody(CodeBuilder codeLines)
        {
            // create the startTask method and body
            builder.WithMethod(
                new MethodBuilder("ConfirmStart", typeof(bool), TypeAttributes.Public, true)
                .WithCodeLines(codeLines)
            );
            return this;
        }
        
        public string ToCode()
        {
            return builder.ToCode();
        }

        public string ToView()
        {
            return builder.ToView();
        }

    }
    



}

