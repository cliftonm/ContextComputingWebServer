using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;

using ContextComputing;

namespace Listeners
{
    public class GetPage { }
    public class Wait1 { }
    public class Wait2 { }
    public class GetPeople { }
    public class GetPets { }

    public static class Listeners
    {
        public static ContextRouter InitializeContext()
        {
            ContextRouter contextRouter = new ContextRouter();
            InitializeContext(contextRouter);
            AutoRegistration.AutoRegister<Listener>(contextRouter);

            return contextRouter;
        }

        public static void InitializeContext(ContextRouter contextRouter)
        {
            contextRouter.AssociateType<HttpContext, GetPage>();
            AutoRegistration.AutoRegister<Listener>(contextRouter);
        }
    }

    public class Listener : IContextComputingListener
    {
        [Listener]
        [Publishes(new string[] { nameof(Wait1), nameof(GetPeople), nameof(GetPets) })]
        public void HelloWorld(ContextRouter router, ContextItem item, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>{0} Hello World!</p>", DateTime.Now.ToString("ss.fff")));
            router.Publish<Wait1>(item.AsyncContext);
            router.Publish<GetPeople>(item.AsyncContext);
            router.Publish<GetPets>(item.AsyncContext);
        }

        [Listener]
        public void Render(ContextRouter router, ContextItem item, People people, Pets pets, [Context(nameof(GetPage))] HttpContext httpContext)
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

        [Listener]
        [Publishes(nameof(Wait2))]
        [DependentContexts(nameof(Wait1))]
        public void OnWait1(ContextRouter router, ContextItem item, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            Thread.Sleep(500);
            httpContext.Response.Write(String.Format("<p>{0} Wait 1</p>", DateTime.Now.ToString("ss.fff")));
            router.Publish<Wait2>(item.AsyncContext);
        }

        [Listener]
        [DependentContexts(nameof(Wait2))]
        public void OnWait2(ContextRouter router, ContextItem item, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            Thread.Sleep(500);
            httpContext.Response.Write(String.Format("<p>{0} Wait 2</p>", DateTime.Now.ToString("ss.fff")));
        }

        [Listener]
        [DependentContexts(nameof(Wait2))]
        public void OnWait2Again(ContextRouter router, ContextItem item, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>{0} Wait 2 again</p>", DateTime.Now.ToString("ss.fff")));
        }

        [Listener]
        [Publishes("People")]
        [DependentContexts(nameof(GetPeople))]
        public void OnGetPeople(ContextRouter router, ContextItem item)
        {
            // Simulate having queried people:
            var people = new People();
            // Simulate the query having taken some time.
            Thread.Sleep(250);
            router.Publish<People>(people, item.AsyncContext);
        }

        [Listener]
        [Publishes("Pets")]
        [DependentContexts(nameof(GetPets))]
        public void OnGetPets(ContextRouter router, ContextItem item)
        {
            // Simulate having queried people:
            var pets = new Pets();
            // Simulate the query having taken some time.
            Thread.Sleep(750);
            router.Publish<Pets>(pets, item.AsyncContext);
        }

        [Listener]
        public void Count(ContextRouter router, ContextItem item, People people, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>There are {0} people.</p>", people.GetPeople().Count));
        }

        [Listener]
        public void Count(ContextRouter router, ContextItem item, Pets pets, [Context(nameof(GetPage))] HttpContext httpContext)
        {
            httpContext.Response.Write(String.Format("<p>There are {0} pet types.</p>", pets.GetPets().Count));
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
}