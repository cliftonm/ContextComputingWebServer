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
        private ContextRouter otherContextRouter;
        private ContextRouter myContextRouter;

        public CPDesigner()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            otherContextRouter = Listeners.Listeners.InitializeContext();
            InitializeComponent();
            myContextRouter = InitializeMyContextRouter();
            myContextRouter.Run();

            myContextRouter.Publish(nameof(ShowListeners), lbListeners);
            myContextRouter.Publish(nameof(ShowContexts), (otherContextRouter, lbContexts));
            myContextRouter.Publish(nameof(ShowTypeMaps), (otherContextRouter, lbContextTypeMaps));

            lbListeners.SelectedIndexChanged += (_, __) => myContextRouter.Publish(nameof(ListenerSelected), (lbListeners, lbParameters, tbListener));
            lbContexts.SelectedIndexChanged += (_, __) => myContextRouter.Publish(nameof(ShowActiveListeners), (otherContextRouter, lbActiveListeners, lbContexts.SelectedItem.ToString()));
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

        public static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assy = Assembly.ReflectionOnlyLoad(args.Name);

            return assy;
        }
    }
}
