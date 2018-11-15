using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ContextComputing
{
    public class MethodListener : CCListener
    {
        protected MethodInfo method;

        public override string Name { get { return method.Name; } }

        public MethodListener(MethodInfo method, List<string> dependentContexts)
        {
            this.method = method;
            DependentContexts = dependentContexts;
        }

        // TODO: This should return context names or the parameter type name if no context attribute is provided.
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
                if (attr.ConstructorArguments[0].ArgumentType.IsArray)
                {
                    var args = (IReadOnlyCollection<CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value;
                    ret.AddRange(args.Select(arg => arg.Value.ToString().Trim()));
                }
                else
                {
                    ret.Add(attr.ConstructorArguments[0].Value.ToString());
                }
            }

            return ret;
        }
    }
}
