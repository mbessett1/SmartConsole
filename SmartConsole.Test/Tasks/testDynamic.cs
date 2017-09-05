//using System;
//using Bessett.SmartConsole;
//namespace Dynamics
//{
//    [TaskHelp("This is a Hello World Dynamic Task")]
//    [TaskAlias("hello-world")]
//    [NoConfirmation]
//    public class HelloWorld : Bessett.SmartConsole.ConsoleTask
//    {
//        String Name { get; set; }

//        Int32 Age { get; set; }

//        public override TaskResult StartTask()
//        {
//            Console.WriteLine($"Hello World, {Name}! You specified Age={Age}.");
//            return TaskResult.Complete();
//        }

//        public override Boolean ConfirmStart()
//        {
//            Console.WriteLine("Please confirm!");
//            return base.ConfirmStart();
//        }

//    }
//}