using System;
using System.Web;
using System.Threading.Tasks;

using ContextComputing;

// iisexpress /path:c:\projects\ContextComputingWebServer /port:9090
// Then browse to: http://localhost:9090/

/*
    Call sequence:

        this(InitializeContextRouter)
        Handler(Action<Handler> initializeContextRouter)
        InitializeContextRouter
        body of Handler() constructor
        BeginProcessRequest
*/

// Some useful links:
// https://tpodolak.com/blog/2016/02/12/using-asyncawait-iasyncresult-pattern/

public class Handler : IHttpAsyncHandler
{
    public bool IsReusable { get { return true; } }

    private static ContextRouter contextRouter;

    /// <summary>
    /// Public constructor that invokes the internal constructor with the initialization method.
    /// </summary>
    public Handler() : this(InitializeContextRouter)
    {
        // A less convoluted approach simply calles InitializeContextRouter here.
    }

    /// <summary>
    /// Given the initialization method, this constructor is called with the function provided
    /// in the above constructor.
    /// </summary>
    internal Handler(Action initializeContextRouter)
    {
        initializeContextRouter();
    }

    /// <summary>
    /// Now we initialize our state, which means creating the ContextRouter if it doesn't exist.
    /// </summary>
    private static void InitializeContextRouter()
    {
        // If we don't have a context router for this session lifetime, create it now.
        if (contextRouter == null)
        {
            contextRouter = new ContextRouter();
            contextRouter.OnException += (router, info) => OnException((ContextRouter)router, info);
            InitializeContextRouter(contextRouter);
            contextRouter.Run();
        }
    }

    private static void OnException(ContextRouter router, ContextExceptionInfo info)
    {
        if (info.Data is HttpContext)
        {
            ((HttpContext)info.Data).Response.Write("<p><font  color='red'>" + info.Exception.Message + "</font></p>");
        }
    }

    private static void InitializeContextRouter(ContextRouter contextRouter)    
    {
        Listeners.Listeners.InitializeContext(contextRouter);
    }

    /// <summary>
    /// Synchronous handler which we don't use but the interface requires us to implement.
    /// </summary>
    public void ProcessRequest(HttpContext context)
    {
        throw new InvalidOperationException();
    }

    public IAsyncResult BeginProcessRequest(HttpContext httpContext, AsyncCallback callback, Object extraData)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        Task t = tcs.Task;

        t.ContinueWith(_ =>
        {
            httpContext.Response.Write(String.Format("<p>{0} Publishing HttpContext...</p>", DateTime.Now.ToString("ss.fff")));
            object asyncContext = new object();
            contextRouter.Publish(httpContext, asyncContext);
            contextRouter.WaitForCompletion(asyncContext);
            contextRouter.Cleanup(asyncContext);
            callback(t);
        });

        tcs.SetResult(true);

        return t;
    }

    public void EndProcessRequest(IAsyncResult result)
    {
        // Because we're waiting for the main task to complete, we don't have to worry about 
        // pool recycles (see comments below) and since Task uses the thread pool, we're not
        // queueing tasks onto the ASP.NET thread pool.
        ((Task)result).Wait();
    }
}

// Refer to https://stackoverflow.com/a/28806198/2276361 regarding HostingEnvironment.
// The salient point being: This can lead to edge cases where a pool recycle executes, 
// ignoring your background task completely, causing an abnormal abort. That is why you have 
// to use a mechanism which registers the task, such as HostingEnvironment.QueueBackgroundWorkItem.

// And this: https://stackoverflow.com/q/6388793/2276361


