using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ContextComputing;

namespace Designer
{
    public static class ExtensionMethods
    {
        public static bool Implements<T>(this Type t)
        {
            string name = typeof(T).Name;
            bool implements = t.GetInterfaces().Any(i => i.Name == name);

            return implements;
        }
    }

    public static class Model
    {
        public static IEnumerable<Type> GetListeners()
        {
            var dlls = Directory.GetFiles(".\\", "*.dll");

            var methods = dlls.Aggregate(new List<Type>(), (acc, dll) =>
            {
                try
                {
                    var assy = Assembly.ReflectionOnlyLoadFrom(dll);
                    var listeners = assy.GetTypes().Where(t => t.IsClass && t.Implements<IContextComputingListener>());
                    acc.AddRange(listeners);                    
                }
                catch { }

                return acc;
            });

            return methods;
        }

        /// <summary>
        /// Returns a list of comma-separated parameters for all Execute methods implemented by the listener.
        /// </summary>
        public static IEnumerable<string> GetParameters(IEnumerable<Type> listeners, string name)
        {
            Type t = listeners.Single(l => l.Name == name);
            var methods = t.GetMethods().Where(m => m.Name == "Execute");

            var items = methods.Select(m =>
            {
                var paramTypes = m.GetParameters().Skip(2);
                string parms = String.Join(", ", paramTypes.Select(p => p.ParameterType.Name));

                return (parms == String.Empty) ? "[none]" : parms;
            });

            return items;
        }
    }
}
