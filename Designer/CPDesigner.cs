using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;
using FlowSharpLib;
using Clifton.Core.Services.SemanticProcessorService;

using FlowSharpServiceInterfaces;

using ContextComputing;

namespace Designer
{
    public partial class CPDesigner : Form
    {
        protected BaseController canvasController;

        public CPDesigner()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            ContextRouter otherContextRouter = Listeners.Listeners.InitializeContext();
            ContextRouter myContextRouter = InitializeMyContextRouter();

            WireUpEvents(myContextRouter, otherContextRouter);
            myContextRouter.Run();

            Shown += (_, __) => FormShown(myContextRouter, otherContextRouter);

            ServiceManager sm = new ServiceManager();
            sm.RegisterSingleton<ISemanticProcessor, SemanticProcessor>();
            sm.RegisterSingleton<IFlowSharpCanvasService, FlowSharpCanvasService.FlowSharpCanvasService>();
            sm.RegisterSingleton<IFlowSharpMouseControllerService, FlowSharpMouseControllerService.FlowSharpMouseControllerService>();
            sm.FinishSingletonInitialization();

            sm.Get<IFlowSharpCanvasService>().CreateCanvas(pnlDiagram);
            canvasController = sm.Get<IFlowSharpCanvasService>().ActiveController;
            var canvas = canvasController.Canvas;
            canvas.EndInit();

            sm.Get<IFlowSharpMouseControllerService>().Initialize(canvasController);
        }

        /*
        protected void OnMouseMove(object sender, MouseEventArgs args)
				// Conversely, we redraw the grid and invalidate, which forces all the elements to redraw.
				//canvas.Drag(delta);
				//elements.ForEach(el => el.Move(delta));
				//canvas.Invalidate();
        */

        protected void FormShown(ContextRouter myContextRouter, ContextRouter otherContextRouter)
        {
            // Publish after the form is shown, otherwise the InvokeRequired will return false even though
            // we're handling the publish on a separate thread.
            myContextRouter.Publish(nameof(ShowListeners), lbListeners);
            myContextRouter.Publish(nameof(ShowContexts), (otherContextRouter, lbContexts));
            myContextRouter.Publish(nameof(ShowTypeMaps), (otherContextRouter, lbContextTypeMaps));
            myContextRouter.Publish(nameof(DrawContext), (otherContextRouter, canvasController));
        }

        protected ContextRouter InitializeMyContextRouter()
        {
            ContextRouter cr = new ContextRouter();
            cr
                .Register<ShowListeners>()
                .Register<ShowContexts>()
                .Register<ShowTypeMaps>()
                .Register<ListenerSelected>()
                .Register<ShowPublishedContext>(nameof(ShowListenerInfo))
                .Register<ShowListenerSelection>(nameof(ShowListenerInfo))
                .Register<ShowListenerParameters>(nameof(ShowListenerInfo))
                .Register<ShowActiveListeners>()
                .Register<DrawContext>();

            return cr;
        }

        // Using tuples gets ugly because we don't have any context of which listbox is which.
        // Consider using a class as a container, or a container for each listbox so we have strongly-typed parameters.

        protected void WireUpEvents(ContextRouter myContextRouter, ContextRouter otherContextRouter)
        {
            lbListeners.SelectedIndexChanged += (_, __) =>
                myContextRouter.Publish(
                    nameof(ListenerSelected),
                    (lbListeners, lbParameters, lbPublishes, tbListener));

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
