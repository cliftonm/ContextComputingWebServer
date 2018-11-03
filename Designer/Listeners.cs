using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using FlowSharpLib;

using ContextComputing;

namespace Designer
{
    // Type holder.
    public class ShowListenerInfo { }

    public class ShowListeners : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ListBox listBox)
        {
            var listeners = Model.GetListeners();

            listBox.BeginInvoke(() =>
            {
                listBox.Items.AddRange(listeners.OrderBy(l => l.Name).Select(l => l.Name).ToArray());
            });
        }
    }

    public class ShowContexts : IContextComputingListener
    {
        public  void Execute(ContextRouter router, ContextItem item, (ContextRouter router, ListBox listBox) data)
        {
            data.listBox.BeginInvoke(() => data.listBox.Items.AddRange(data.router.GetAllContexts().OrderBy(c => c).ToArray()));
        }
    }

    public class ShowTypeMaps : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (ContextRouter router, ListBox listBox) data)
        {
            data.listBox.BeginInvoke(() => data.listBox.Items.AddRange(data.router.GetTypeContexts().Select(tc => tc.type.Name + " => " + tc.context).ToArray()));
        }
    }

    public class ListenerSelected : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (ListBox listBoxListeners, ListBox listBoxParameters, ListBox listBoxPublishes, TextBox textBox) data)
        {
            data.listBoxListeners.BeginInvoke(() =>
            {
                string name = data.listBoxListeners.SelectedItem.ToString();
                router.Publish(nameof(ShowListenerInfo), (data.textBox, data.listBoxParameters, data.listBoxPublishes, name));
            });
        }
    }

    public class ShowListenerSelection : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (TextBox textBox, ListBox, ListBox, string name) data)
        {
            data.textBox.BeginInvoke(() => data.textBox.Text = data.name);
        }
    }

    public class ShowListenerParameters : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (TextBox, ListBox listBox, ListBox, string name) data)
        {
            var listeners = Model.GetListeners();
            var executors = Model.GetParameters(listeners, data.name);

            data.listBox.BeginInvoke(() =>
            {
                data.listBox.Items.Clear();
                data.listBox.Items.AddRange(executors.ToArray());
            });
        }
    }

    public class ShowPublishedContext : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (TextBox, ListBox, ListBox listBox, string name) data)
        {
            var listener = Model.GetListeners().Single(l => l.Name == data.name);

            data.listBox.BeginInvoke(() =>
            {
                data.listBox.Items.Clear();
                data.listBox.Items.AddRange(Model.GetContextsPublished(listener).ToArray());
            });
        }
    }

    public class ShowActiveListeners : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (ContextRouter router, ListBox listBox, string contextName) data)
        {
            var listenerTypes = data.router.GetListeners(data.contextName);

            data.listBox.BeginInvoke(() =>
            {
                data.listBox.Items.Clear();
                data.listBox.Items.AddRange(listenerTypes.Select(lt => lt.Name).ToArray());
            });
        }
    }

    public class DrawContext : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (ContextRouter router, BaseController canvasController) data)
        {
            var listeners = Model.GetListeners();
            int x = 10;
            int y = 10;

            // listeners are of type "ReflectOnlyType" so we use name so we can compare System.RuntimeType with ReflectionOnlyType.
            Dictionary<string, GraphicElement> listenerShapes = new Dictionary<string, GraphicElement>();

            listeners.OrderBy(l => l.Name).ForEach(l =>
            {
                Box box = new Box(data.canvasController.Canvas);
                box.DisplayRectangle = new Rectangle(x, y, 100, 30);
                box.Text = l.Name;
                listenerShapes[l.Name] = box;
                data.canvasController.AddElement(box);
                x += 130;

                if (x + 100 > data.canvasController.Canvas.Width)
                {
                    x = 10;
                    y += 50;
                }
            });

            var connectors = new List<GraphicElement>();

            listeners.ForEach(l =>
            {
                var publishes = Model.GetContextsPublished(l);
                var sourceElement = listenerShapes[l.Name];

                publishes.ForEach(context =>
                {
                    var listenerTypes = data.router.GetListeners(context);

                    listenerTypes.ForEach(lt =>
                    {
                        if (listenerShapes.TryGetValue(lt.Name, out GraphicElement targetElement))
                        {
                            Point p1 = sourceElement.DisplayRectangle.Center();
                            Point p2 = targetElement.DisplayRectangle.Center();
                            var connector = new DiagonalConnector(data.canvasController.Canvas, p1, p2);
                            connector.EndCap = AvailableLineCap.Arrow;
                            connector.BorderPen = new Pen(Color.Green);
                            connector.UpdateProperties();
                            data.canvasController.AddElement(connector);
                            connectors.Add(connector);

                            ConnectionPoint cp1 = new ConnectionPoint(GripType.Start, p1);
                            ConnectionPoint cp2 = new ConnectionPoint(GripType.End, p2);

                            Connection c1 = new Connection() { ToElement = connector, ToConnectionPoint = cp2, ElementConnectionPoint = cp1 };
                            Connection c2 = new Connection() { ToElement = connector, ToConnectionPoint = cp1, ElementConnectionPoint = cp2 };

                            sourceElement.Connections.Add(c2);
                            targetElement.Connections.Add(c1);

                            connector.SetConnection(GripType.Start, sourceElement);
                            connector.SetConnection(GripType.End, targetElement);
                        }
                    });
                });
            });

            data.canvasController.SelectElements(connectors);
            data.canvasController.Topmost();
            connectors.ForEach(c => data.canvasController.DeselectElement(c));

            data.canvasController.Canvas.Invalidate();
        }
    }
}
