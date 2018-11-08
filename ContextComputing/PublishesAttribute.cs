using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextComputing
{
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
    }
}
