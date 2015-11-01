using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
 namespace Bessett.SmartConsole
{
     internal static class Helpers
     {   
         public static Dictionary<string, string> ParseCommandArguments(string[] args)
         {
             var commandArguments = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase );

             var identifier = "";
             string defaultIdentifier;
             bool isIdentifier;

             for (int i = 1; i < args.Length; i++)
             {
                 var argumentExpected = (identifier.Length > 0);
                 defaultIdentifier = string.Format("arg{0}", i);
                 isIdentifier = args[i].StartsWith("-");

                 if (isIdentifier)
                 {
                     if (argumentExpected)
                     {
                         commandArguments.Add(identifier, "true");
                         identifier = "";
                     }
                     else
                     {
                         identifier = args[i].Substring(1);
                     }
                 }
                 else
                 {
                     string value = args[i];
                     commandArguments.Add(identifier.Length > 0 ? identifier : defaultIdentifier, value);
                     identifier = "";
                 }
             }

             if (identifier.Length > 0)
             {
                 commandArguments.Add(identifier, "true");
             }

             return commandArguments;
         }

         public static string TextFromFile(string filename)
         {
             try
             {
                 using (var sr = new StreamReader(filename))
                 {
                     return sr.ReadToEnd();
                 }
             }
             catch (Exception ex)
             {
                 throw new Exception(String.Format("Exception reading [{0}] - {1}", filename, ex.Message));
             }
         }

         public static T InjectObject<T>(T destination, Dictionary<string, string> nameValuePairs) where T : new()
         {
             const BindingFlags flags =
                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

             foreach (var key in nameValuePairs.Keys)
             {
                 var p = destination.GetType().GetProperty(key, flags);
              
                 if (p != null)
                 {
                     InjectPropertyValue<T>(destination, nameValuePairs[key], p);
                 }
             }
             return destination;
         }

         public static void InjectPropertyValue<T>(T destination, string value, PropertyInfo p) where T : new()
         {
             try
             {
                 TypeConverter typeConverter = TypeDescriptor.GetConverter(p.PropertyType);
                 object propValue = typeConverter.ConvertFromString(value);

                 p.SetValue(destination, propValue, null);
             }
             catch (Exception ex)
             {
                 var message = String.Format("{0}: {1}", p.Name, ex.Message);
                 throw new Exception(message);
             }
         }

         //public static T InjectObject<T>(Dictionary<string, string> nameValuePairs) where T : new()
         //{
         //    var t = new T();
         //    t = InjectObject(t, nameValuePairs);
         //    return t;
         //}

         public static string[] ParseText(string buffer, out bool isIncomplete, char fieldDelimiter = ',', char escapeDelimiter = '"')
         {
             bool insideQuote = false;
             var lineContents = new List<string>();
             string currItem = String.Empty;
             char previousChar = ' ';

             try
             {
                 foreach (var character in buffer)
                 {
                     if (character == escapeDelimiter)
                     {
                         if (!insideQuote && previousChar == escapeDelimiter)
                         {
                             currItem += character;
                         }
                         insideQuote = !insideQuote;
                     }
                     else if (character == fieldDelimiter)
                     {
                         if (!insideQuote)
                         {
                             lineContents.Add(currItem);
                             currItem = "";
                         }
                         else
                         {
                             currItem += character;
                         }
                     }
                     else
                     {
                         currItem += character;
                     }
                     previousChar = character;
                 }

                 isIncomplete = insideQuote;

                 if (!insideQuote)
                 {
                     lineContents.Add(currItem);
                     return lineContents.ToArray();
                 }

                 return null;

             }
             catch (Exception ex)
             {
                 throw new Exception("Parsing exception", ex);
             }
         }

         public static bool AsBool(this string source)
         {
             return source.ToLower() == "yes" || source.ToLower() == "true";
         }
         
     }

}
