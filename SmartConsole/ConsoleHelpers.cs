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
                        commandArguments.Add(identifier, null);
                    }
                    identifier = args[i].Substring(1);
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
                commandArguments.Add(identifier, null);
             }

            return commandArguments;
         }

         public static T InjectObject<T>(T destination, Dictionary<string, string> nameValuePairs) where T : new()
         {
             const BindingFlags flags =
                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

             foreach (var key in nameValuePairs.Keys)
             {
                 var property = destination.GetType().GetProperty(key, flags);
              
                 if (property != null)
                 {
                     InjectPropertyValue<T>(destination, nameValuePairs[key], property);
                 }
                else if (key!="arg1")
                {
                    // parameter specified that isn't a target task property
                    throw new ApplicationException($"'{key}' is not a valid argument");
                }
             }
             return destination;
         }

         public static void InjectPropertyValue<T>(T destination, string value, PropertyInfo property) where T : new()
         {
             try
             {
                object propValue;
                TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);

                // interpret a missing/default value for boolean fields as true, not false
                if (string.IsNullOrEmpty(value)
                    && ( property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)))
                {
                    propValue = true;
                }
                else
                {
                    propValue = typeConverter.ConvertFromString(value);
                }

                property.SetValue(destination, propValue, null);
             }
             catch (Exception ex)
             {
                 var message = String.Format("{0}: {1}", property.Name, ex.Message);
                 throw new Exception(message);
             }
         }

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
