using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextComputing
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ContextAttribute : Attribute
    {
        public string ContextName { get; protected set; }

        public ContextAttribute(string contextName)
        {
            ContextName = contextName;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ListenerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PublishesAttribute : Attribute
    {
        public List<string> Contexts { get; protected set; }

        /// <summary>
        /// Comma-delimited contexts.
        /// </summary>
        public PublishesAttribute(string contexts)
        {
            Contexts = new List<string>(contexts.Split(',').Select(c => c.Trim()));
        }

        public PublishesAttribute(string[] contexts)
        {
            Contexts = new List<string>(contexts);
        }
    }
}
