using System.Collections.Generic;
using System.Reflection;

namespace ContextComputing
{
    internal class TriggerData
    {
        public List<object> Data { get; protected set; }
        public MethodInfo Method { get; protected set; }

        public TriggerData(List<object> data, MethodInfo method)
        {
            Data = data;
            Method = method;
        }
    }
}
