using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;

using ContextComputing;

namespace Listeners
{
    public class GetPage { }

    public static class Listeners
    {
        public static ContextRouter InitializeContext()
        {
            ContextRouter contextRouter = new ContextRouter();
            InitializeContext(contextRouter);

            return contextRouter;
        }

        public static void InitializeContext(ContextRouter contextRouter)
        {
            contextRouter.Register<Wait1>()
                .Register<Wait2>()
                .Register<Wait2Again, Wait2>();

            contextRouter
               .AssociateType<HttpContext, GetPage>()
                .Register<HelloWorld, GetPage>()
                .Register<GetPeople>()
                .Register<GetPets>()
                // TODO: param order dependency
                .TriggerOn<Render, People, Pets, GetPage>()
                .TriggerOn<Count, People, GetPage>()
                .TriggerOn<Count, Pets, GetPage>();
        }
    }

    public class HelloWorld : IContextComputingListener
    {
        [Publishes("Wait1, GetPeople, GetPets")]
        public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>{0} Hello World!</p>", DateTime.Now.ToString("ss.fff")));
            router.Publish<Wait1>(httpContext, item.AsyncContext);
            router.Publish<GetPeople>(null, item.AsyncContext);
            router.Publish<GetPets>(null, item.AsyncContext);
        }
    }

    public class Wait1 : IContextComputingListener
    {
        [Publishes("Wait2")]
        public void Execute(ContextRouter router, ContextItem item, HttpContext httpContext)
        {
            Thread.Sleep(500);
            httpContext.Response.Write(String.Format("<p>{0} Wait 1</p>", DateTime.Now.ToString("ss.fff")));
            router.Publish<Wait2>(httpContext, item.AsyncContext);
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
        [Publishes("People")]
        public void Execute(ContextRouter router, ContextItem item)
        {
            // Simulate having queried people:
            var people = new People();
            // Simulate the query having taken some time.
            Thread.Sleep(250);
            router.Publish<People>(people, item.AsyncContext);
        }
    }

    public class GetPets : IContextComputingListener
    {
        [Publishes("Pets")]
        public void Execute(ContextRouter router, ContextItem item)
        {
            // Simulate having queried people:
            var pets = new Pets();
            // Simulate the query having taken some time.
            Thread.Sleep(750);
            router.Publish<Pets>(pets, item.AsyncContext);
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

    public class Count : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, People people, HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>There are {0} people.</p>", people.GetPeople().Count));
        }

        public void Execute(ContextRouter router, ContextItem item, Pets pets, HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>There are {0} pet types.</p>", pets.GetPets().Count));
        }
    }
}