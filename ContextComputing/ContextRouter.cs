﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ContextComputing
{
    public interface IContextComputingListener { }

    public static class ExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (T item in items)
            {
                action(item);
            }
        }
    }

    public class ContextItem
    {
        public string Context { get; protected set; }
        public object Data { get; protected set; }
        public object AsyncContext { get; protected set; }

        public ContextItem(string context, object data, object asyncContext)
        {
            Context = context;
            Data = data;
            AsyncContext = asyncContext;
        }
    }

    public class ContextExceptionInfo
    {
        public Exception Exception { get; protected set; }
        public object Data { get; protected set; }
        public object AsyncContext { get; protected set; }

        public ContextExceptionInfo(Exception ex, object data, object asyncContext)
        {
            Exception = ex;
            Data = data;
            AsyncContext = asyncContext;
        }
    }

    public class PendingContext
    {
        public string ContextName { get; protected set; }
        public object Data { get; protected set; }
        public bool Posted { get; protected set; }

        public PendingContext(string contextName)
        {
            ContextName = contextName;
        }

        public void Post(object data)
        {
            Data = data;
            Posted = true;
        }

        public void Clear()
        {
            Data = null;
            Posted = false;
        }
    }

    public class Trigger
    {
        public List<string> Contexts { get { return masterPendingContexts.Select(mpc => mpc.ContextName).ToList(); } }

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

            if (asyncPendingContexts.TryGetValue(asyncContext, out List<PendingContext> pendingContexts))
            {
                ret = pendingContexts.All(pc => pc.Posted);
            }

            return ret;
        }

        public void Post(string context, object data, object asyncContext)
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
                    pendingContexts.SingleOrDefault(c => c.ContextName == context).Post(data);
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
            }

            return data;
        }

        public void ClearPendingContexts()
        {
            asyncPendingContexts.Clear();
        }
    }

    public class TriggerData
    {
        public List<object> Data { get; protected set; }

        public TriggerData(List<object> data)
        {
            Data = data;
        }
    }

    public class ContextRouter
    {
        public EventHandler<ContextExceptionInfo> OnException;
        protected SemaphoreSlim semQueue = new SemaphoreSlim(0);
        protected ConcurrentQueue<(ContextItem contextItem, IContextComputingListener action, object data, EventWaitHandle waitHandle)> contextPool = new ConcurrentQueue<(ContextItem, IContextComputingListener, object, EventWaitHandle)>();
        protected ConcurrentDictionary<object, List<WaitHandle>> contextThreads = new ConcurrentDictionary<object, List<WaitHandle>>();

        // In case the application wants to do registration in a thread rather than at the application startup,
        // we use a ConcurrentDictionary here.
        protected ConcurrentDictionary<string, List<(IContextComputingListener listener, Guid id)>> contextListeners = new ConcurrentDictionary<string, List<(IContextComputingListener, Guid)>>();

        // TODO: Associate Type with a GUID so the type can be removed as a listener from a context.
        protected ConcurrentDictionary<string, List<Type>> contextListenerTypes = new ConcurrentDictionary<string, List<Type>>();

        // TODO: Ability to associate multiple required types with a context.
        protected ConcurrentDictionary<Type, List<string>> typeContexts = new ConcurrentDictionary<Type, List<string>>();

        protected List<Trigger> triggers = new List<Trigger>();

        private Object nullContext = new object();

        public List<string> GetAllContexts()
        {
            List<string> ret = new List<string>();

            contextListeners.ForEach(kvp => ret.Add(kvp.Key));
            contextListenerTypes.ForEach(kvp => ret.Add(kvp.Key));
            typeContexts.ForEach(kvp => ret.AddRange(kvp.Value));
            triggers.ForEach(t => ret.AddRange(t.Contexts));

            return ret.Distinct().ToList();
        }

        public List<Type> GetListeners(string context)
        {
            List<Type> ret = new List<Type>();

            contextListeners.Where(cl => cl.Key == context).ForEach(cl => ret.AddRange(cl.Value.Select(l => l.listener.GetType())));
            contextListenerTypes.Where(clt => clt.Key == context).ForEach(clt => ret.AddRange(clt.Value));

            return ret.Distinct().ToList();
        }

        public List<(Type type, string context)> GetTypeContexts()
        {
            List<(Type, string)> ret = new List<(Type type, string context)>();

            typeContexts.ForEach(kvp => kvp.Value.ForEach(c => ret.Add((kvp.Key, c))));

            return ret;
        }

        public ContextRouter AssociateType(Type t, string context)
        {
            List<string> contexts;

            lock (typeContexts)
            {
                if (!typeContexts.TryGetValue(t, out contexts))
                {
                    contexts = new List<string>();
                    typeContexts[t] = contexts;
                }
            }

            lock (contexts)
            {
                contexts.Add(context);
            }

            return this;
        }

        // TODO: Ability to associate multiple required types with a context.
        public ContextRouter AssociateType<T>(string context)
        {
            AssociateType(typeof(T), context);

            return this;
        }

        public ContextRouter Register(string context, Type listenerType)
        {
            Assert.That(listenerType.GetInterfaces().Any(t => t == typeof(IContextComputingListener)),
                "Type " + listenerType.Name + " does not implement IContextComputingListener");

            List<Type> listenerTypes;

            lock (contextListenerTypes)
            {
                if (!contextListenerTypes.TryGetValue(context, out listenerTypes))
                {
                    listenerTypes = new List<Type>();
                    contextListenerTypes[context] = listenerTypes;
                }
            }

            lock (listenerTypes)
            {
                listenerTypes.Add(listenerType);
            }

            return this;
        }

        /// <summary>
        /// Use the type name of T as the context name.
        /// </summary>
        public ContextRouter Register<T>() where T : IContextComputingListener
        {
            Register(typeof(T).Name, typeof(T));

            return this;
        }

        public ContextRouter Register<T>(string context) where T : IContextComputingListener
        {
            Register(context, typeof(T));

            return this;
        }

        public ContextRouter Register(string context, IContextComputingListener listener)
        {
            List<(IContextComputingListener, Guid)> listeners;

            // Prevent race condition in the event that tasks are adding listeners to the same context.
            // We don't want to associate the same key to two different listeners.
            lock (contextListeners)
            {
                if (!contextListeners.TryGetValue(context, out listeners))
                {
                    listeners = new List<(IContextComputingListener, Guid)>();
                    contextListeners[context] = listeners;
                }
            }

            // Prevent race condition in the event that tasks are adding/removing listeners.
            lock (listeners)
            {
                listeners.Add((listener, Guid.Empty));
            }

            return this;
        }

        public ContextRouter Register(string context, IContextComputingListener listener, out Guid id)
        {
            List<(IContextComputingListener, Guid)> listeners;
            id = Guid.NewGuid();

            // Prevent race condition in the event that tasks are adding listeners to the same context.
            // We don't want to associate the same key to two different listeners.
            lock (contextListeners)
            {
                if (!contextListeners.TryGetValue(context, out listeners))
                {
                    listeners = new List<(IContextComputingListener, Guid)>();
                    contextListeners[context] = listeners;
                }
            }

            // Prevent race condition in the event that tasks are adding/removing listeners.
            lock (listeners)
            {
                listeners.Add((listener, id));
            }

            return this;
        }

        public ContextRouter TriggerOn(string[] contexts, Type t)
        {
            // Setup placeholder for contexts when they get published for this type.
            lock (triggers)
            {
                triggers.Add(new Trigger(contexts, t));
            }

            return this;
        }

        /// <summary>
        /// This type is instantiated on-demand when the data in all the required contexts exists.
        /// </summary>
        public ContextRouter TriggerOn<T>(params string[] contexts)
        {
            TriggerOn(contexts, typeof(T));

            return this;
        }

        public ContextRouter Unregister(string context, Guid id)
        {
            List<(IContextComputingListener action, Guid id)> listeners;

            if (contextListeners.TryGetValue(context, out listeners))
            {
                // Prevent race condition in the event that tasks are adding/removing listeners.
                lock (listeners)
                {
                    listeners.RemoveAll(l => l.id == id);

                    // We never remove the contextMethods key because it is possible that
                    // another task is dynamically adding a listener.  We could potentially
                    // remove the contextMethod key while the key is being used to add a listener.
                }
            }

            return this;
        }

        public void Run()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    semQueue.Wait();
                    contextPool.TryDequeue(out (ContextItem contextItem, IContextComputingListener listener, object data, EventWaitHandle waitHandle) item);
                    Task.Run(() => InvokeListener(item.contextItem, item.listener, item.data, item.waitHandle));
                }
            });
        }

        public void Publish(object data, object asyncContext = null)
        {
            asyncContext = asyncContext ?? nullContext;
            List<string> clonedContexts;

            if (typeContexts.TryGetValue(data.GetType(), out List<string> contexts))
            {
                lock (contexts)
                {
                    clonedContexts = new List<string>(contexts);
                }

                clonedContexts.ForEach(c => Publish(c, data, asyncContext));
            }
        }

        public void Publish(string context, object data, object asyncContext = null)
        {
            asyncContext = asyncContext ?? nullContext;
            PublishDataToInstances(context, data, asyncContext);
            PublishDataToOnDemandListeners(context, data, asyncContext);
            PostContextsToTriggers(context, data, asyncContext);
            CheckForTriggers(context, asyncContext);
        }

        /// <summary>
        /// Waits until all processes have completed.  If a completed process adds another process that we
        /// must wait for, then we wait again.
        /// </summary>
        public void WaitForCompletion(object asyncContext)
        {
            if (contextThreads.ContainsKey(asyncContext))
            {
                int n1;
                int n2 = contextThreads[asyncContext].Count;

                do
                {
                    n1 = n2;
                    WaitHandle.WaitAll(contextThreads[asyncContext].ToArray());
                    n2 = contextThreads[asyncContext].Count;
                } while (n1 != n2 && n2 != 0);
            }
        }

        public void Cleanup(object asyncContext)
        {
            if (contextThreads.ContainsKey(asyncContext))
            {
                contextThreads[asyncContext].ForEach(waiter => waiter.Dispose());
                contextThreads.TryRemove(asyncContext, out _);
            }

            // Clear any pending context data.
            triggers.ForEach(t => t.ClearPendingContexts());
        }

        private void PostContextsToTriggers(string context, object data, object asyncContext)
        {
            lock (triggers)
            {
                triggers.ForEach(t => t.Post(context, data, asyncContext));
            }
        }

        private void CheckForTriggers(string context, object asyncContext)
        {
            triggers.Where(t => t.AllPosted(asyncContext)).ForEach(t =>
            {
                IContextComputingListener listener = (IContextComputingListener)Activator.CreateInstance(t.ListenerType);
                TriggerData data = t.GetDataAndClear(asyncContext);
                ContextItem contextItem = new ContextItem(null, data, asyncContext);
                Enqueue(listener, contextItem, data);
                semQueue.Release();
            });
        }

        private void PublishDataToInstances(string context, object data, object asyncContext)
        {
            List<(IContextComputingListener instance, Guid id)> listeners;
            List<(IContextComputingListener instance, Guid id)> clonedListeners;

            if (contextListeners.TryGetValue(context, out listeners))
            {
                var contextItem = new ContextItem(context, data, asyncContext);

                // Clone the current state of the listener list in a lock so we don't update this
                // list while a task is dynamically adding/removing listeners.
                lock (listeners)
                {
                    clonedListeners = new List<(IContextComputingListener action, Guid id)>(listeners);
                }

                // Work with the cloned list so any tasks changing listeners for this context don't cause problems.
                // They will have missed the message, oh well.
                clonedListeners.ForEach(listener =>
                {
                // Create a wait handle for each listener so we know when the listener has completed.
                // The wait handles are accumlated for the specified context.
                Enqueue(listener.instance, contextItem, data);
                });

                semQueue.Release(clonedListeners.Count);
            }
        }

        private void PublishDataToOnDemandListeners(string context, object data, object asyncContext)
        {
            List<Type> listenerTypes;
            List<Type> clonedListenerTypes;

            if (contextListenerTypes.TryGetValue(context, out listenerTypes))
            {
                var contextItem = new ContextItem(context, data, asyncContext);

                lock (listenerTypes)
                {
                    clonedListenerTypes = new List<Type>(listenerTypes);
                }

                clonedListenerTypes.ForEach(listenerType =>
                {
                    var listener = (IContextComputingListener)Activator.CreateInstance(listenerType);
                    Enqueue(listener, contextItem, data);
                });

                semQueue.Release(clonedListenerTypes.Count);
            }
        }

        private void Enqueue(IContextComputingListener listener, ContextItem contextItem, object data)
        {
            var waitHandler = new EventWaitHandle(false, EventResetMode.ManualReset);
            AddContextThreadWaiter(contextItem.AsyncContext, waitHandler);
            contextPool.Enqueue((contextItem, listener, data, waitHandler));
        }

        /// <summary>
        /// Perform the action specified by the listener and then set the listener's wait handle to signalled (completed.)
        /// </summary>
        private void InvokeListener(ContextItem contextItem, IContextComputingListener listener, object data, EventWaitHandle waitHandle)
        {
            // Use runtime binding to invoke the Execute method.
            Type t = listener.GetType();
            IEnumerable<MethodInfo> methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == "Execute");

            try
            {
                // Any method on the listener that doesn't take any parameters.
                methods.Where(m => m.GetParameters().Length == 2).ForEach(m => m.Invoke(listener, new object[] { this, contextItem }));

                if (data is TriggerData)
                {
                    // Put trigger data into param list in the order it's in the list.
                    List<object> parms = new List<object>() { this, contextItem };
                    parms.AddRange(((TriggerData)data).Data);

                    var mi = methods.SingleOrDefault(m =>
                    {
                        var methodParamTypes = m.GetParameters().Select(p => p.ParameterType);
                        var paramTypes = parms.Select(p => p.GetType());
                        bool equal = methodParamTypes.SequenceEqual(paramTypes);

                        return equal;
                    });

                   Assert.That(mi != null, "No suitable method in " + listener.GetType().Name + " found for parameters " + String.Join(", ", parms.Select(p => p.GetType().Name)));
                   mi.Invoke(listener, parms.ToArray());
                }
                else
                {
                    var methods3parms = methods.Where(m => m.GetParameters().Length == 3 && m.GetParameters().Last().ParameterType == data.GetType());
                    methods3parms.ForEach(m => m.Invoke(listener, new object[] { this, contextItem, data }));
                }
            }
            catch (TargetInvocationException ex)
            {
                OnException?.Invoke(this, new ContextExceptionInfo(ex.InnerException, data, contextItem.AsyncContext));
            }
            catch (Exception ex2)
            {
                OnException?.Invoke(this, new ContextExceptionInfo(ex2, data, contextItem.AsyncContext));
            }

            waitHandle.Set();
        }

        private void AddContextThreadWaiter(object asyncContext, WaitHandle waiter)
        {
            List<WaitHandle> waiters;

            // Block creating a context key from simultaneous tasks.
            lock (contextThreads)
            {
                if (!contextThreads.TryGetValue(asyncContext, out waiters))
                {
                    waiters = new List<WaitHandle>();
                    contextThreads[asyncContext] = waiters;
                }
            }

            // Block adding a waiter from simultaneous tasks on the same context.
            lock (waiters)
            {
                waiters.Add(waiter);
            }
        }
    }
}