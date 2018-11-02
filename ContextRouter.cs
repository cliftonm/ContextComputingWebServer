using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
    public ConcurrentDictionary<object, List<PendingContext>> AsyncPendingContexts { get; protected set; } = new ConcurrentDictionary<object, List<PendingContext>>();

    // Master list.
    private List<PendingContext> masterPendingContexts = new List<PendingContext>();

    public Type ListenerType { get; protected set; }

    public Trigger(string[] contexts, Type listenerType)
    {
        ListenerType = listenerType;
        contexts.ForEach(c => masterPendingContexts.Add(new PendingContext(c)));
    }

    public void Post(string context, object data, object asyncContext)
    {
        List<PendingContext> pendingContexts;

        if (masterPendingContexts.Any(mpc => mpc.ContextName == context))
        {
            lock (AsyncPendingContexts)
            {
                if (!AsyncPendingContexts.TryGetValue(asyncContext, out pendingContexts))
                {
                    // This async context gets its clone from the master list.
                    pendingContexts = new List<PendingContext>(masterPendingContexts);
                    AsyncPendingContexts[asyncContext] = pendingContexts;
                }
            }

            lock (pendingContexts)
            {
                pendingContexts.SingleOrDefault(c => c.ContextName == context).Post(data);
            }
        }
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
    protected ConcurrentDictionary<string, List<(IContextComputingListener, Guid)>> contextListeners = new ConcurrentDictionary<string, List<(IContextComputingListener, Guid)>>();
    protected ConcurrentDictionary<string, List<Type>> contextListenerTypes = new ConcurrentDictionary<string, List<Type>>();
    protected ConcurrentDictionary<Type, List<string>> typeContexts = new ConcurrentDictionary<Type, List<string>>();
    protected Dictionary<Type, Trigger> triggers = new Dictionary<Type, Trigger>();

    private Object nullContext = new object();

    // TODO: Ability to associate multiple required types with a context.
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
            triggers[t] = new Trigger(contexts, t);
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
        triggers.ForEach(kvp => kvp.Value.AsyncPendingContexts.Clear());
    }

    private void PostContextsToTriggers(string context, object data, object asyncContext)
    {
        lock (triggers)
        {
            triggers.ForEach(kvp =>
            {
                kvp.Value.Post(context, data, asyncContext);
            });
        }
    }

    private void CheckForTriggers(string context, object asyncContext)
    {
        lock (triggers)
        {
            triggers.ForEach(kvp =>
            {
                if (kvp.Value.AsyncPendingContexts.TryGetValue(asyncContext, out List<PendingContext> pendingContexts))
                {
                    if (pendingContexts.All(pc => pc.Posted))
                    {
                        IContextComputingListener listener = (IContextComputingListener)Activator.CreateInstance(kvp.Key);
                        TriggerData data;
                        ContextItem contextItem;

                        // We don't want anyone else touch the pending contexts.
                        lock (pendingContexts)
                        {
                            data = new TriggerData(pendingContexts.Select(c => c.Data).ToList());
                            contextItem = new ContextItem(null, data, asyncContext);
                            pendingContexts.ForEach(c => c.Clear());
                        }

                        Enqueue(listener, contextItem, data);
                        semQueue.Release();
                    }
                }
            });
        }
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
        MethodInfo mi = t.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public);

        try
        {
            if (mi.GetParameters().Length == 2)
            {
                mi.Invoke(listener, new object[] { this, contextItem });
            }
            else
            {
                if (data is TriggerData)
                {
                    // Put trigger data into param list in the order it's in the list.
                    List<object> parms = new List<object>() { this, contextItem };
                    parms.AddRange(((TriggerData)data).Data);
                    mi.Invoke(listener, parms.ToArray());
                }
                else
                {
                    mi.Invoke(listener, new object[] { this, contextItem, data });
                }
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

