using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Clifton.Core.ExtensionMethods;
using FlowSharpLib;

using ContextComputing;

namespace Designer
{
    // Type holder.
    public class ShowListenerInfo { }
    public class ListenerListBox { }
    public class ListenerTextBox { }
    public class ContextListBox { }
    public class ParametersListBox { }
    public class PublishesListBox { }
    public class OtherContextRouter { }
    public class ContextTypeMapsListBox { }
    public class StartingListener { }
    public class SelectedListener { }
    public class SelectedContext { }
    public class ActiveListenersListBox { }

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
        public  void Execute(ContextRouter router, ContextItem item, ContextRouter otherRouter, ListBox listBox)
        {
            listBox.BeginInvoke(() => listBox.Items.AddRange(otherRouter.GetAllContexts().OrderBy(c => c).ToArray()));
        }
    }

    public class ShowTypeMaps : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ContextRouter otherRouter, ListBox listBox)
        {
            listBox.BeginInvoke(() => listBox.Items.AddRange(otherRouter.GetTypeContexts().Select(tc => tc.type.Name + " => " + tc.context).ToArray()));
        }
    }

    /*
    public class ListenerSelected : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ListBox listBoxListeners, ListBox listBoxParameters, ListBox listBoxPublishes, TextBox textBox, string name)
        {
            listBoxListeners.BeginInvoke(() =>
            {
                router.Publish(nameof(ShowListenerInfo), (textBox, listBoxParameters, listBoxPublishes, name));
            });
        }
    }
    */

    public class ShowListenerSelection : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, TextBox textBox, string name)
        {
            textBox.BeginInvoke(() => textBox.Text = name);
        }
    }

    public class ShowListenerParameters : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ListBox listBox, string name)
        {
            var listeners = Model.GetListeners();
            var executors = Model.GetParameters(listeners, name);

            listBox.BeginInvoke(() =>
            {
                listBox.Items.Clear();
                listBox.Items.AddRange(executors.ToArray());
            });
        }
    }

    public class ShowPublishedContext : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ListBox listBox, string name)
        {
            var listener = Model.GetListeners().Single(l => l.Name == name);

            listBox.BeginInvoke(() =>
            {
                listBox.Items.Clear();
                listBox.Items.AddRange(Model.GetContextsPublished(listener).ToArray());
            });
        }
    }

    public class ShowActiveListeners : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ContextRouter otherRouter, ListBox listBox, string contextName)
        {
            var listenerTypes = otherRouter.GetListeners(contextName);

            listBox.BeginInvoke(() =>
            {
                listBox.Items.Clear();
                listBox.Items.AddRange(listenerTypes.Select(lt => lt.Name).ToArray());
            });
        }
    }

    public class DrawContext : IContextComputingListener
    {
        enum Direction
        {
            None,
            Center,
            Up,
            Down,
            Left,
            Right,
        }

        // Directions to try depending on the number of target listeners.

        public void Execute(ContextRouter router, ContextItem item, ContextRouter otherRouter, CanvasController canvasController, string startingListenerName)
        {
            Box box;
            List<((int x, int y) p, Type type, Direction dir)> placedListeners = new List<((int, int), Type, Direction)>();
            List<((int x, int y) p, GraphicElement el)> occupiedCells = new List<((int x, int y), GraphicElement)>();
            // listeners are of type "ReflectOnlyType" so we use name so we can compare System.RuntimeType with ReflectionOnlyType.
            Dictionary<string, GraphicElement> listenerShapes = new Dictionary<string, GraphicElement>();

            // https://stackoverflow.com/questions/398299/looping-in-a-spiral
            IEnumerable<(int x, int y)> Spiral(int X, int Y)
            {
                List<(int, int)> cells = new List<(int, int)>();
                int x, y, dx, dy;
                x = y = dx = 0;
                dy = -1;
                int tm = Math.Max(X, Y);
                int maxI = tm * tm;

                for (int i = 0; i < maxI; i++)
                {
                    if ((-X / 2 <= x) && (x <= X / 2) && (-Y / 2 <= y) && (y <= Y / 2))
                    {
                        yield return (x, y);
                    }

                    if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
                    {
                        tm = dx;
                        dx = -dy;
                        dy = tm;
                    }

                    x += dx;
                    y += dy;
                }
            }

            (int, int, Direction) GetFreeCell(Type forType, (int x, int y) srcCell, (int x, int y, Direction dir)[] tryPoints, IEnumerable<(int x, int y)> occupiedPoints)
            {
                (int, int, Direction) p = (0, 0, Direction.None);
                bool found = false;

                foreach (var pointToTry in tryPoints)
                {
                    int x = pointToTry.x + srcCell.x;
                    int y = pointToTry.y + srcCell.y;

                    if (!occupiedPoints.Any(oc => oc.x == x && oc.y == y))
                    {
                        p = (x, y, pointToTry.dir);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Spiral out in a 10x10 grid centeered around the current location to find a free cell.
                    foreach (var pointToTry in Spiral(10, 10))
                    {
                        int x = pointToTry.x + srcCell.x;
                        int y = pointToTry.y + srcCell.y;

                        if (!occupiedPoints.Any(oc => oc.x == x && oc.y == y))
                        {
                            p = (x, y, Direction.Center);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    throw new Exception("Unable to find a free cell for type " + forType.Name);
                }

                return p;
            }

            void PlaceTargetListeners((int x, int y) sourceCell, Type sourceListener, Direction sourceListenerDir)
            {
                var publishes = Model.GetContextsPublished(sourceListener);

                publishes.ForEach(context =>
                {
                    var targetListeners = otherRouter.GetListeners(context);
                    
                    // ToList because we're adding to placedListeners as we iterate targetListeners, so we need to capture
                    // this way the list looks now.
                    var notPlacedListeners = targetListeners.Where(tl => !placedListeners.Any(pl => pl.type == tl)).ToList();

                    notPlacedListeners.ForEach(tl =>
                    {
                        (int x, int y, Direction dir) p = (0, 0, Direction.None);

                        switch (sourceListenerDir)
                        {
                            case Direction.Center:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, -1, Direction.Up), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (0, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Up:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, -1, Direction.Up), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (1, 0, Direction.Right), (-1, 1, Direction.Left), (1, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Down:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, 1, Direction.Down), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (0, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Left:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (-1, 0, Direction.Left), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Up), (1, -1, Direction.Up), (0, -1, Direction.Up), (0, 1, Direction.Down) };
                                    p = GetFreeCell(tl, sourceCell, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Right:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (1, 0, Direction.Right), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Up), (1, -1, Direction.Up), (0, -1, Direction.Up), (0, 1, Direction.Down) };
                                    p = GetFreeCell(tl, sourceCell, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }
                        }

                        placedListeners.Add(((p.x, p.y), tl, p.dir));
                        box = new Box(canvasController.Canvas);
                        box.Text = tl.Name;
                        listenerShapes[tl.Name] = box;
                        occupiedCells.Add(((p.x, p.y), box));
                        // PlaceTargetListeners((p.x, p.y), tl, p.dir);
                    });

                    notPlacedListeners.ForEach(tl =>
                    {
                        var placed = placedListeners.Single(pl => pl.type.Name == tl.Name);
                        PlaceTargetListeners(placed.p, tl, placed.dir);
                    });
                });
            }

            void PlaceShapes()
            {
                int cx = canvasController.Canvas.Width / 2;
                int cy = canvasController.Canvas.Height / 2;

                occupiedCells.ForEach(oc =>
                {
                    int x = oc.p.x * 150 + cx;
                    int y = oc.p.y * 75 + cy;
                    oc.el.DisplayRectangle = new Rectangle(x, y, 100, 30);
                });
            }

            var listeners = Model.GetListeners();

            // Start with entry point type.
            Type t = listeners.Single(l => l.Name == startingListenerName);
            Direction dir = Direction.Center;
            placedListeners.Add(((0, 0), t, Direction.Center));
            box = new Box(canvasController.Canvas);
            box.Text = t.Name;
            listenerShapes[t.Name] = box;
            occupiedCells.Add(((0, 0), box));
            PlaceTargetListeners((0, 0), t, dir);
            PlaceShapes();

            occupiedCells.ForEach(oc => canvasController.AddElement(oc.el));

            var connectors = new List<GraphicElement>();

            listeners.ForEach(l =>
            {
                var publishes = Model.GetContextsPublished(l);

                if (listenerShapes.TryGetValue(l.Name, out GraphicElement sourceElement))
                {
                    publishes.ForEach(context =>
                    {
                        var listenerTypes = otherRouter.GetListeners(context);

                        listenerTypes.ForEach(lt =>
                        {
                            if (listenerShapes.TryGetValue(lt.Name, out GraphicElement targetElement))
                            {
                                Point p1 = sourceElement.DisplayRectangle.Center();
                                Point p2 = targetElement.DisplayRectangle.Center();
                                var connector = new DiagonalConnector(canvasController.Canvas, p1, p2);
                                connector.EndCap = AvailableLineCap.Arrow;
                                connector.BorderPen = new Pen(Color.Green);
                                connector.UpdateProperties();
                                canvasController.AddElement(connector);
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
                }
            });

            canvasController.SelectElements(connectors);
            canvasController.Topmost();
            connectors.ForEach(c => canvasController.DeselectElement(c));

            canvasController.Canvas.Invalidate();
        }
    }
}
