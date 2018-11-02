using System;
using System.IO;
using System.Web;

public class Module : IHttpModule
{
    public void Init(HttpApplication application)
    {
        // File.Delete(@"c:\temp\out.txt");
        //application.BeginRequest += new EventHandler(BeginRequest);
        //application.EndRequest += new EventHandler(EndRequest);
    }

    public void Dispose()
    {
    }

    /*
    private void BeginRequest(object sender, EventArgs e)
    {
        HttpApplication application = (HttpApplication)sender;
        HttpContext context = application.Context;
        File.AppendAllText(@"c:\temp\out.txt", "BeginRequest: " + context.Request.Url + "\r\n");
        context.Response.ContentType = "text/html";
        context.Response.Write("<h1><font color=red>HelloWorldModule: Beginning of Request</font></h1><hr>");
    }

    private void EndRequest(object sender, EventArgs e)
    {
        HttpApplication application = (HttpApplication)sender;
        HttpContext context = application.Context;
        File.AppendAllText(@"c:\temp\out.txt", "EndRequest: " + context.Request.Url + "\r\n");
        context.Response.Write("<hr><h2><font color=blue>HelloWorldModule: End of Request</font></h2>");
    }
    */
}