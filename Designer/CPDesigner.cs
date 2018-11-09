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
            myContextRouter.OnException += (_, cei) => MessageBox.Show(cei.Exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            WireUpEvents(myContextRouter);
            myContextRouter.Run();

            Shown += (_, __) => FormShown(myContextRouter, otherContextRouter);
            canvasController = InitializeFlowSharp();
        }

        protected BaseController InitializeFlowSharp()
        {
            ServiceManager sm = new ServiceManager();
            sm.RegisterSingleton<ISemanticProcessor, SemanticProcessor>();
            sm.RegisterSingleton<IFlowSharpCanvasService, FlowSharpCanvasService.FlowSharpCanvasService>();
            sm.RegisterSingleton<IFlowSharpMouseControllerService, FlowSharpMouseControllerService.FlowSharpMouseControllerService>();
            sm.FinishSingletonInitialization();

            sm.Get<IFlowSharpCanvasService>().CreateCanvas(pnlDiagram);
            canvasController = sm.Get<IFlowSharpCanvasService>().ActiveController;
            var canvas = canvasController.Canvas;
            sm.Get<IFlowSharpMouseControllerService>().Initialize(canvasController);
            canvas.EndInit();

            // TODO: Occasionally we get an "object in use" exception if the mouse is in the canvas area and moving on initialization.

            return canvasController;
        }

        /*
        // If we want the canvas background to move as well, we need this mouse handler:
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
            myContextRouter.Publish<ListenerListBox>(lbListeners, isStatic: true);
            myContextRouter.Publish<ListenerTextBox>(tbListener, isStatic: true);
            myContextRouter.Publish<ContextListBox>(lbContexts, isStatic: true);
            myContextRouter.Publish<ParametersListBox>(lbParameters, isStatic: true);
            myContextRouter.Publish<PublishesListBox>(lbPublishes, isStatic: true);
            myContextRouter.Publish<ActiveListenersListBox>(lbActiveListeners, isStatic: true);
            myContextRouter.Publish<ContextTypeMapsListBox>(lbContextTypeMaps, isStatic: true);
            myContextRouter.Publish<OtherContextRouter>(otherContextRouter, isStatic: true);
            myContextRouter.Publish<CanvasController>(canvasController, isStatic: true);
            myContextRouter.Publish<StartingListener>("HelloWorld");
        }

        protected ContextRouter InitializeMyContextRouter()
        {
            // Remember, trigger parameters must be in the order of the parameters in the Execute handler.

            ContextRouter cr = new ContextRouter();
            cr
                .TriggerOn<ShowListeners, ListenerListBox>()
                .TriggerOn<ShowContexts, OtherContextRouter, ContextListBox>()
                .TriggerOn<ShowTypeMaps, OtherContextRouter,  ContextTypeMapsListBox>()
                .TriggerOn<DrawContext, OtherContextRouter, CanvasController, StartingListener>()
                .TriggerOn<ShowListenerSelection, ListenerTextBox, SelectedListener>()
                .TriggerOn<ShowPublishedContext, PublishesListBox, SelectedListener>()
                .TriggerOn<ShowListenerParameters, ParametersListBox, SelectedListener>()
                .TriggerOn<ShowActiveListeners, OtherContextRouter, ActiveListenersListBox, SelectedContext>()
                ;

            return cr;
        }

        // Using tuples gets ugly because we don't have any context of which listbox is which.
        // Consider using a class as a container, or a container for each listbox so we have strongly-typed parameters.

        protected void WireUpEvents(ContextRouter myContextRouter)
        {
            lbListeners.SelectedIndexChanged += (_, __) => myContextRouter.Publish<SelectedListener>(lbListeners.SelectedItem.ToString());
            lbContexts.SelectedIndexChanged += (_, __) => myContextRouter.Publish<SelectedContext>(lbContexts.SelectedItem.ToString());
        }

        public static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assy = Assembly.ReflectionOnlyLoad(args.Name);

            return assy;
        }
    }
}
