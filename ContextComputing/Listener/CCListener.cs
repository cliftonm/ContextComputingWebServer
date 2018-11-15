using System.Collections.Generic;

namespace ContextComputing
{
    public abstract class CCListener
    {
        public abstract string Name { get; }

        public abstract IEnumerable<string> GetParameters();
        public abstract List<string> GetContextsPublished();

        public virtual List<string> DependentContexts { get; protected set; }

        public CCListener()
        {
            DependentContexts = new List<string>();
        }
    }
}
