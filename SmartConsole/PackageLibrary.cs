using System;
using System.Collections.Generic;
using System.Linq;

namespace Bessett.SmartConsole
{
    public static class PackageLibrary
    {
        private static List<Type> _packages { get; set; }= new List<Type>();

        static PackageLibrary()
        {
            // get all pacakges into an available list
            _packages = TaskLibrary.GetTypes<TaskPackage>().Where(t => t.GetConstructor(Type.EmptyTypes) != null).ToList();
        }

        public static IEnumerable<Type> ListAll()
        {
            foreach (var package in _packages)
            {
                yield return package;
            }
        }
        public static TaskPackage GetPackage(string name)
        {
            var packageType = _packages.FirstOrDefault(p => p.Name == name);
            return (packageType != null)
                ? (TaskPackage) Activator.CreateInstance(packageType)
                : null;
        }

    }
}