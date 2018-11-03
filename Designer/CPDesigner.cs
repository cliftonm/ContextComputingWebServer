using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using ContextComputing;

namespace Designer
{
    public partial class CPDesigner : Form
    {
        public CPDesigner()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            ContextRouter otherContextRouter = Listeners.Listeners.InitializeContext();
            ContextRouter myContextRouter = InitializeMyContextRouter();

            WireUpEvents(myContextRouter, otherContextRouter);
            myContextRouter.Run();

            myContextRouter.Publish(nameof(ShowListeners), lbListeners);
            myContextRouter.Publish(nameof(ShowContexts), (otherContextRouter, lbContexts));
            myContextRouter.Publish(nameof(ShowTypeMaps), (otherContextRouter, lbContextTypeMaps));
        }

        protected ContextRouter InitializeMyContextRouter()
        {
            ContextRouter cr = new ContextRouter();
            cr
                .Register<ShowListeners>()
                .Register<ShowContexts>()
                .Register<ShowTypeMaps>()
                .Register<ListenerSelected>()
                .Register<ShowListenerSelection>()
                .Register<ShowListenerParameters>()
                .Register<ShowActiveListeners>();

            return cr;
        }

        protected void WireUpEvents(ContextRouter myContextRouter, ContextRouter otherContextRouter)
        {
            lbListeners.SelectedIndexChanged += (_, __) =>
                myContextRouter.Publish(
                    nameof(ListenerSelected), 
                    (lbListeners, lbParameters, tbListener));

            lbContexts.SelectedIndexChanged += (_, __) =>
                myContextRouter.Publish(
                    nameof(ShowActiveListeners), 
                    (otherContextRouter, lbActiveListeners, lbContexts.SelectedItem.ToString()));
        }

        public static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assy = Assembly.ReflectionOnlyLoad(args.Name);

            return assy;
        }
    }
}
