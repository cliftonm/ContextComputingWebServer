using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

public class AsyncServer : IAsyncResult
{
    public bool IsCompleted { get; protected set; } = false;
    public WaitHandle AsyncWaitHandle => null;
    public object AsyncState { get; protected set; }
    public bool CompletedSynchronously => false;

    public AsyncServer ServeRequest((HttpContext context, AsyncCallback callback, Object extraData) content)
    {
        ThreadPool.QueueUserWorkItem(new WaitCallback(StartAsyncTask), content);

        return this;
    }

    private async void StartAsyncTask(object state)
    {
        (HttpContext context, AsyncCallback callback, Object extraData) content = (ValueTuple<HttpContext, AsyncCallback, Object>)state;

        await Task.Run(() =>
        {
            content.context.Response.Write("<p>Hello from the handler!</p>");

        });

        IsCompleted = true;
        content.callback(this);
    }
}