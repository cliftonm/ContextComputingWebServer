using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Clifton.Core.ExtensionMethods;

namespace ContextComputing
{
    internal class Trigger
    {
        public List<string> Contexts { get { return masterPendingContexts.Select(mpc => mpc.ContextName).ToList(); } }

        /// <summary>
        /// Triggers that receive just static data are effectively one-time triggers.  Once all the static data has
        /// been posted, the trigger is never fired again -- updating a static context value throws an exception.
        /// So we set this flag to indicate that the trigger has been fired and we don't want to fire this trigger ever again.
        /// </summary>
        private bool neverPostAgain;

        // Master list.
        private List<PendingContext> masterPendingContexts = new List<PendingContext>();

        // List of contexts with pending data to eventually trigger a listener.
        private ConcurrentDictionary<object, List<PendingContext>> asyncPendingContexts = new ConcurrentDictionary<object, List<PendingContext>>();

        public Type ListenerType { get; protected set; }

        public Trigger(string[] contexts, Type listenerType)
        {
            ListenerType = listenerType;
            contexts.ForEach(c => masterPendingContexts.Add(new PendingContext(c)));
        }

        public bool AllPosted(object asyncContext)
        {
            bool ret = false;

            if (!neverPostAgain)
            {
                if (asyncPendingContexts.TryGetValue(asyncContext, out List<PendingContext> pendingContexts))
                {
                    ret = pendingContexts.All(pc => pc.Posted);
                }
            }

            return ret;
        }

        public void Post(string context, object data, object asyncContext, bool isStatic)
        {
            List<PendingContext> pendingContexts;

            if (masterPendingContexts.Any(mpc => mpc.ContextName == context))
            {
                lock (asyncPendingContexts)
                {
                    if (!asyncPendingContexts.TryGetValue(asyncContext, out pendingContexts))
                    {
                        // This async context gets its clone from the master list.
                        pendingContexts = new List<PendingContext>(masterPendingContexts);
                        asyncPendingContexts[asyncContext] = pendingContexts;
                    }
                }

                lock (pendingContexts)
                {
                    pendingContexts.SingleOrDefault(c => c.ContextName == context).Post(data, isStatic);
                }
            }
        }

        public TriggerData GetDataAndClear(object asyncContext)
        {
            TriggerData data;

            List<PendingContext> pendingContexts = asyncPendingContexts[asyncContext];

            // We don't want anyone else updating the data in pending contexts at this point.
            // Furthermore, because we clear the pending contexts after acquiring the data, we
            // also want to make sure data isn't being posted while we're clearing the "Posted" flag and
            // the associated data.
            lock (pendingContexts)
            {
                data = new TriggerData(pendingContexts.Select(c => c.Data).ToList());
                pendingContexts.ForEach(c => c.Clear());
                neverPostAgain = pendingContexts.All(pc => pc.IsStatic);
            }

            return data;
        }

        public void ClearPendingContexts()
        {
            asyncPendingContexts.Clear();
        }
    }
}
