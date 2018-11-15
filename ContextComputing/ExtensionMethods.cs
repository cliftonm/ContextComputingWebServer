using System;
using System.Linq;

namespace ContextComputing
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
}
