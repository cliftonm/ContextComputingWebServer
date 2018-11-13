using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    public abstract class CCListener
    {
        public abstract string Name { get; }

        public abstract IEnumerable<string> GetParameters();
        public abstract List<string> GetContextsPublished();
    }

    public class TypeListener : CCListener
    {
        private const BindingFlags bindingFlags = BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

        protected Type t;

        public override string Name { get { return t.Name; } }

        public TypeListener(Type t)
        {
            this.t = t;
        }

        public override IEnumerable<string> GetParameters()
        {
            // Only one "Execute" method per class is supported.
            var method = t.GetMethods(bindingFlags).Where(m => m.Name == "Execute").SingleOrDefault();
            var paramTypes = method?.GetParameters().Skip(2);
            var parms = paramTypes?.Select(p => p.ParameterType.Name) ?? new List<string>();

            return parms;
        }

        public override List<string> GetContextsPublished()
        {
            List<string> ret = new List<string>();

            var method = t.GetMethods(bindingFlags).Where(m => m.Name == "Execute").SingleOrDefault();

            var cad = method?.GetCustomAttributesData();
            var attr = cad?.SingleOrDefault(c => c.AttributeType.Name == typeof(PublishesAttribute).Name);

            if (attr != null)
            {
                ret.AddRange(attr.ConstructorArguments[0].Value.ToString().Split(',').Select(c => c.Trim()));
            }

            return ret;
        }
    }

    public class MethodListener : CCListener
    {
        protected MethodInfo method;

        public override string Name { get { return method.Name; } }

        public MethodListener(MethodInfo method)
        {
            this.method = method;
        }

        public override IEnumerable<string> GetParameters()
        {
            var paramTypes = method.GetParameters().Skip(2);
            var parms = paramTypes.Select(p => p.ParameterType.Name);

            return parms;
        }

        public override List<string> GetContextsPublished()
        {
            List<string> ret = new List<string>();

            var cad = method.GetCustomAttributesData();
            var attr = cad.SingleOrDefault(c => c.AttributeType.Name == typeof(PublishesAttribute).Name);

            if (attr != null)
            {
                ret.AddRange(attr.ConstructorArguments[0].Value.ToString().Split(',').Select(c => c.Trim()));
            }

            return ret;
        }
    }
}
