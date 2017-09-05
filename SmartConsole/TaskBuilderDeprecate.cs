using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Bessett.SmartConsole.Emit
{
    // seriously look into this model: https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
    // http://www.tugberkugurlu.com/archive/compiling-c-sharp-code-into-memory-and-executing-it-with-roslyn

    // CodeDOM
    // https://msdn.microsoft.com/en-us/library/system.codedom.compiler.codedomprovider(v=vs.110).aspx

    public static class VirtualTasks
    {
        private const string assemblyName = "VirtialTasks";

        public static List<Type> Types { get; private set; } = new List<Type>();
        public static ModuleBuilder Module { get; set; }

        static VirtualTasks()
        {
            // see https://msdn.microsoft.com/en-us/library/system.reflection.emit.typebuilder%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
            // for more details on emitting a class using reflection

            AssemblyName assyName = new AssemblyName(assemblyName);

            AssemblyBuilder assyBuilderb =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    assyName,
                    AssemblyBuilderAccess.Run);

            // For a single-module assembly, the module name is usually
            // the assembly name plus an extension.
            Module =
                assyBuilderb.DefineDynamicModule(assyName.Name, $"{assyName.Name}.dll");
        }
    }

    public class TaskBuilder : List<TypeBuilder>
    {
        public TaskBuilder BuildTasks(Type targetType, string typeAlias = "")
        {
            // build a ConsoleTask class for each method (really?)
            foreach (var methodInfo in targetType.GetMethods())
            {
                var taskName = $"{(typeAlias.Length > 0 ? typeAlias : targetType.Name)}_{methodInfo.Name}";
                CreateTask(methodInfo, taskName);
            }

            return this;
        }
        public TaskBuilder BuildTask(Type targetType, string methodName, string taskAlias = "")
        {
            var targetMethod = targetType.GetMethods().FirstOrDefault();
            if (targetMethod != null)
                CreateTask(targetMethod, taskAlias);

            return this;
        }

        private void CreateTask(MethodInfo methodInfo, string taskAlias = "")
        {
            TypeBuilder typeBuilder = VirtualTasks.Module.DefineType(
                $"{(taskAlias.Length > 0 ? taskAlias : methodInfo.Name)}",
                TypeAttributes.Public,
                typeof(ConsoleTask)
                );

            // Add Task attributes

            // Add properties for each mamber argument
            foreach (var arg in methodInfo.GetParameters())
            {
                BuildProperty(typeBuilder, arg.Name, arg.ParameterType);
            }
            // build ovveride for StartTask, ConfirmStart
            // place any result into the TaskResult.Complete

            // this StartTask must 
            // 1. Instantiate the container/parent Type
            // 2. execute the method of the parent type 

            var parentType = methodInfo.DeclaringType;

            Add(typeBuilder);

        }

        private PropertyBuilder BuildProperty(TypeBuilder tb, string name, Type dataType)
        {
            var nullable = false;//retsField.Required != "0";

            // PROPERTY Construction:
            // the backing field
            FieldBuilder fieldBuilder = tb.DefineField(
                $"m_{name}",
                dataType,
                FieldAttributes.Private);

            // Define a property named Number that gets and sets the private 
            // field.
            //
            // The last argument of DefineProperty is null, because the
            // property has no parameters. (If you don't specify null, you must
            // specify an array of Type objects. For a parameterless property,
            // use the built-in array with no elements: Type.EmptyTypes)
            PropertyBuilder propertyBuilder = tb.DefineProperty(
                name,
                PropertyAttributes.None,
                dataType,
                null);

            // The property "set" and property "get" methods require a special
            // set of attributes.
            MethodAttributes getSetAttr = MethodAttributes.Public |
                                          MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method for Number. The method returns
            // an integer and has no arguments. (Note that null could be 
            // used instead of Types.EmptyTypes)
            MethodBuilder getAccessor = tb.DefineMethod(
                $"get_{name}",
                getSetAttr,
                dataType,
                Type.EmptyTypes);

            ILGenerator getILG = getAccessor.GetILGenerator();
            // For an instance property, argument zero is the instance. Load the 
            // instance, then load the private field and return, leaving the
            // field value on the stack.
            getILG.Emit(OpCodes.Ldarg_0);
            getILG.Emit(OpCodes.Ldfld, fieldBuilder);
            getILG.Emit(OpCodes.Ret);

            // Define the "set" accessor method for Number, which has no return
            // type and takes one argument of type int (Int32).
            MethodBuilder setAccessor = tb.DefineMethod(
                $"set_{name}",
                getSetAttr,
                null,
                new Type[] { dataType });

            ILGenerator numberSetIL = setAccessor.GetILGenerator();
            // Load the instance and then the numeric argument, then store the
            // argument in the field.
            numberSetIL.Emit(OpCodes.Ldarg_0);
            numberSetIL.Emit(OpCodes.Ldarg_1);
            numberSetIL.Emit(OpCodes.Stfld, fieldBuilder);
            numberSetIL.Emit(OpCodes.Ret);

            // Last, map the "get" and "set" accessor methods to the 
            // PropertyBuilder. The property is now complete. 
            propertyBuilder.SetGetMethod(getAccessor);
            propertyBuilder.SetSetMethod(setAccessor);

            return propertyBuilder;
        }

        /// <summary>
        /// Define types from builder in VirtualTasks virtual assembly
        /// </summary>
        public void CreateVirtual()
        {
            foreach (var typeBuilder in this)
            {
                Type createdType = typeBuilder.CreateType();
                VirtualTasks.Types.Add(createdType);
            }
        }

    }

    internal static class TypeBuilderExtensions
    {
        internal static void Test(PropertyBuilder propertyBuilder)
        {
            TaskAlias attr = new TaskAlias("MyAlias");

            propertyBuilder.AddAttribute(attr, "AliasName");
        }

        internal static void AddAttribute<T>(this PropertyBuilder propBuilder, T attrInstance, params string[] propertyNames)
        {
            Type[] constructorParameters = new Type[0];
            ConstructorInfo constructorInfo = typeof(T).GetConstructor(constructorParameters);

            var props = new PropertyInfo[propertyNames.Length];
            var values = new object[propertyNames.Length];

            for (int i = 0; i < propertyNames.Length; i++)
            {
                props[i] = typeof(T).GetProperty(propertyNames[i]);
                values[i] = props[i].GetValue(attrInstance);
            }

            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { }, props, values);

            propBuilder.SetCustomAttribute(attributeBuilder);
        }

        internal static void AddAttributesMultipleValueConstructionSemiOrig<T>(PropertyBuilder custNamePropBldr)
        {
            Type[] constructorParameters = new Type[0];
            ConstructorInfo constructorInfo = typeof(T).GetConstructor(constructorParameters);

            PropertyInfo nameProperty = typeof(T).GetProperty("Name");
            PropertyInfo orderProperty = typeof(T).GetProperty("Order");

            CustomAttributeBuilder displayAttributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { }, new PropertyInfo[] { nameProperty, orderProperty }, new object[] { "Prop Name", 1 });

            custNamePropBldr.SetCustomAttribute(displayAttributeBuilder);
        }
        internal static void AddAttributeWellKnown<T>(PropertyBuilder propertyBuilder, params Type[] paramTypes) where T : Attribute
        {
            Type[] constructorParameters = new Type[] { typeof(string) };
            ConstructorInfo constructorInfo = typeof(T).GetConstructor(constructorParameters);

            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { "Property Name A" });

            propertyBuilder.SetCustomAttribute(attributeBuilder);
        }
    }
}
