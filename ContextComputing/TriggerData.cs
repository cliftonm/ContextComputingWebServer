using System.Collections.Generic;

namespace ContextComputing
{
    internal class TriggerData
    {
        public List<object> Data { get; protected set; }

        public TriggerData(List<object> data)
        {
            Data = data;
        }
    }
}
