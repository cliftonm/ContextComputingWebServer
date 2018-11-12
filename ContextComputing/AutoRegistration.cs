using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;

namespace ContextComputing
{
    public static class AutoRegistration
    {
        /// <summary>
        /// Listeners are always registered as triggers.
        /// </summary>
        public static void AutoRegister<T>(ContextRouter router) where T : IContextComputingListener
        {
            var listeners = GetListenerMethods<T>();

            listeners.ForEach(listener =>
            {
                var publishes = GetPublishes(listener);
                var contexts = GetContexts(listener);
                router.TriggerOn<T>(contexts.ToArray(), listener);
            });
        }

        private static IEnumerable<MethodInfo> GetListenerMethods<T>() where T : IContextComputingListener
        {
            Type t = typeof(T);

            var listenerMethods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).
                Where(mi => mi.GetCustomAttribute<ListenerAttribute>() != null);

            return listenerMethods;
        }

        private static List<string> GetPublishes(MethodInfo listener)
        {
            var ret = listener.GetCustomAttribute<PublishesAttribute>()?.Contexts ?? new List<string>();

            return ret;
        }

        private static IEnumerable<string> GetContexts(MethodInfo listener)
        {
            var parms = listener.GetParameters();

            Assert.That(parms.Length >= 2, "Listener must have ContextRouter, ContextItem as the minimum number of parameters.");
            Assert.That(parms[0].ParameterType == typeof(ContextRouter), "ContextRouter must be the first parameter type.");
            Assert.That(parms[1].ParameterType == typeof(ContextItem), "ContextItem must be the second parameter type.");

            // Use the specified context name, or the type name of the parameter as the context.
            var ret =  parms.Skip(2).Select(p => p.GetCustomAttribute<ContextAttribute>()?.ContextName ?? p.ParameterType.Name);

            return ret;
        }
    }
}
