using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public interface IContextComputingPacket { }
public interface IContextComputingListener { }

public class HelloWorld : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
    {
        httpContext.Response.Write(String.Format("<p>{0} Hello World!</p>", DateTime.Now.ToString("ss.fff")));
        router.Publish("Wait1", httpContext, item.AsyncContext);
        router.Publish("GetPeople", null, item.AsyncContext);
        router.Publish("GetPets", null, item.AsyncContext);
    }
}

public class Wait1 : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
    {
        Thread.Sleep(500);
        httpContext.Response.Write(String.Format("<p>{0} Wait 1</p>", DateTime.Now.ToString("ss.fff")));
        router.Publish("Wait2", httpContext, item.AsyncContext);
    }
}

public class Wait2 : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
    {
        Thread.Sleep(500);
        httpContext.Response.Write(String.Format("<p>{0} Wait 2</p>", DateTime.Now.ToString("ss.fff")));
    }
}

public class Wait2Again : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
    {
        httpContext.Response.Write(String.Format("<p>{0} Wait 2 again</p>", DateTime.Now.ToString("ss.fff")));
    }
}

public class People
{
    private List<string> people = new List<string>()
    {
        "Tom", "Dick", "Harry"
    };

    public List<string> GetPeople()
    {
        return people;
    }
}

public class Pets
{
    private List<string> pets = new List<string>()
    {
        "Cat", "Dog", "Tarantula", "Snake", "Fish", "Bird"
    };

    public List<string> GetPets()
    {
        return pets;
    }
}

public class GetPeople : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item)
    {
        // Simulate having queried people:
        var people = new People();
        // Simulate the query having taken some time.
        Thread.Sleep(250);
        router.Publish("People", people, item.AsyncContext);
    }
}

public class GetPets : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item)
    {
        // Simulate having queried people:
        var pets = new Pets();
        // Simulate the query having taken some time.
        Thread.Sleep(750);
        router.Publish("Pets", pets, item.AsyncContext);
    }
}

public class Render : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, People people, Pets pets, HttpContext httpContext)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<table style='border: 1px solid black; display:inline-table;'>");
        people.GetPeople().ForEach(p => sb.Append("<tr><td>" + p + "</td></tr>"));
        sb.Append("</table>");
        sb.Append("&nbsp;");                                                                                    
        sb.Append("<table style='border: 1px solid black; display:inline-table;'>");
        pets.GetPets().ForEach(p => sb.Append("<tr><td>" + p + "</td></tr>"));
        sb.Append("</table>");

        httpContext.Response.Write(sb.ToString());
    }
}

public class CountPeople : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, People people, HttpContext httpContext)
    {
        httpContext.Response.Write(String.Format("<p>There are {0} people.</p>", people.GetPeople().Count));
    }
}

public class CountPets : IContextComputingListener
{
    public void Execute(ContextRouter router, ContextItem item, Pets people, HttpContext httpContext)
    {
        httpContext.Response.Write(String.Format("<p>There are {0} pet types.</p>", people.GetPets().Count));
    }
}

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
        //contextRouter.AssociateType<HttpContext>("Test")
        //    .Register<HelloWorld>("Test")

        //contextRouter.Register<Wait1>("Wait1")
        //    .Register<Wait2>("Wait2")
        //    .Register<Wait2Again>("Wait2");

        contextRouter
            .AssociateType<HttpContext>("GetPage")
            .Register<HelloWorld>("GetPage")
            .Register<GetPeople>("GetPeople")
            .Register<GetPets>("GetPets")
            // TODO: param order dependency
            .TriggerOn<Render>("People", "Pets", "GetPage" )
            .TriggerOn<CountPeople>("People", "GetPage")
            .TriggerOn<CountPets>("Pets", "GetPage");
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


