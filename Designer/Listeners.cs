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
    public class ListenerTextBox { }
    public class LogTextBox { }
    public class ShowListenerInfo { }
    public class ListenerListBox { }
    public class ContextListBox { }
    public class ParametersListBox { }
    public class PublishesListBox { }
    public class OtherContextRouter { }
    public class ContextTypeMapsListBox { }
    public class StartingListener { }
    public class SelectedListener { }
    public class SelectedContext { }
    public class ActiveListenersListBox { }
    public class Startup { }

    public class Listener : IContextComputingListener
    {
        const string CRLF = "\r\n";

        [Listener]
        public void Log(ContextRouter router, ContextItem item, [Context(nameof(LogTextBox))] TextBox textBox, LogInfo info)
        {
            textBox.BeginInvoke(() =>
            {
                textBox.AppendText(info.Message + CRLF);
            });
        }

        [Listener]
        public void ShowListeners(ContextRouter router, ContextItem item, 
            [Context(nameof(OtherContextRouter))]   ContextRouter otherRouter, 
            [Context(nameof(ListenerListBox))]      ListBox listBox)
        {
            var listeners = otherRouter.GetAllListeners();

            listBox.BeginInvoke(() =>
            {
                listBox.Items.AddRange(listeners.Select(l => l.Name).OrderBy(n => n).ToArray());
            });
        }

        [Listener]
        public void ShowContexts(ContextRouter router, ContextItem item,
            [Context(nameof(OtherContextRouter))]   ContextRouter otherRouter, 
            [Context(nameof(ContextListBox))]       ListBox listBox)
        {
            listBox.BeginInvoke(() => listBox.Items.AddRange(otherRouter.GetAllContexts().OrderBy(c => c).ToArray()));
        }

        [Listener]
        public void ShowTypeMaps(ContextRouter router, ContextItem item,
            [Context(nameof(OtherContextRouter))]       ContextRouter otherRouter, 
            [Context(nameof(ContextTypeMapsListBox))]   ListBox listBox)
        {
            listBox.BeginInvoke(() => listBox.Items.AddRange(otherRouter.GetTypeContexts().Select(tc => tc.type.Name + " => " + tc.context).ToArray()));
        }

        [Listener]
        public void ShowListenerSelection(ContextRouter router, ContextItem item, 
            [Context(nameof(ListenerTextBox))]      TextBox textBox, 
            [Context(nameof(SelectedListener))]     string name)
        {
            textBox.BeginInvoke(() => textBox.Text = name);
        }

        [Listener]
        public void ShowListenerParameters(ContextRouter router, ContextItem item,
            [Context(nameof(OtherContextRouter))]   ContextRouter otherRouter, 
            [Context(nameof(ParametersListBox))]    ListBox listBox,
            [Context(nameof(SelectedListener))]     string name)
        {
            var listener = otherRouter.GetAllListeners().Single(l=>l.Name == name);

            listBox.BeginInvoke(() =>
            {
                listBox.Items.Clear();
                listBox.Items.AddRange(listener.GetParameters().ToArray());
            });
        }

        [Listener]
        public void ShowPublishedContext(ContextRouter router, ContextItem item,
            [Context(nameof(OtherContextRouter))]   ContextRouter otherRouter,
            [Context(nameof(PublishesListBox))]     ListBox listBox,
            [Context(nameof(SelectedListener))]     string name)
        {
            // var listener = Model.GetListeners().Single(l => l.Name == name);
            var listener = otherRouter.GetAllListeners().Single(l => l.Name == name);

            listBox.BeginInvoke(() =>
            {
                listBox.Items.Clear();
                listBox.Items.AddRange(listener.GetContextsPublished().ToArray());
            });
        }

        [Listener]
        public void ShowActiveListeners(ContextRouter router, ContextItem item,
            [Context(nameof(OtherContextRouter))]         ContextRouter otherRouter,
            [Context(nameof(ActiveListenersListBox))]     ListBox listBox,
            [Context(nameof(SelectedContext))]            string contextName)
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
            List<((int x, int y) p, CCListener listener, Direction dir)> placedListeners = new List<((int, int), CCListener, Direction)>();
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

            (int, int, Direction) GetFreeCell(CCListener forListener, (int x, int y) srcCell, List<(int x, int y)> requiredCells, (int x, int y, Direction dir)[] tryPoints, IEnumerable<(int x, int y)> occupiedPoints)
            {
                (int, int, Direction) p = (0, 0, Direction.None);
                bool found = false;

                foreach (var pointToTry in tryPoints)
                {
                    int x = pointToTry.x + srcCell.x;
                    int y = pointToTry.y + srcCell.y;

                    foreach (var rp in requiredCells)
                    {
                        if (!occupiedPoints.Any(oc => oc.x == x + rp.x && oc.y == y + rp.y))
                        {
                            p = (x, y, pointToTry.dir);
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
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

                        foreach (var rp in requiredCells)
                        {
                            if (!occupiedPoints.Any(oc => oc.x == x + rp.x && oc.y == y + rp.y))
                            {
                                p = (x, y, Direction.Center);
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                if (!found)
                {
                    throw new Exception("Unable to find a free cell for type " + forListener.Name);
                }

                return p;
            }

            void PlaceTargetListeners((int x, int y) sourceCell, CCListener sourceListener, Direction sourceListenerDir)
            {
                var publishes = sourceListener.GetContextsPublished();

                publishes.ForEach(context =>
                {
                    var targetListeners = otherRouter.GetListeners(context);
                    
                    // ToList because we're adding to placedListeners as we iterate targetListeners, so we need to capture
                    // this way the list looks now.
                    var notPlacedListeners = targetListeners.Where(tl => !placedListeners.Any(pl => pl.listener == tl)).ToList();

                    notPlacedListeners.ForEach(tl =>
                    {
                        (int x, int y, Direction dir) p = (0, 0, Direction.None);
                        List<(int x, int y)> requiredCells = new List<(int x, int y)>();

                        box = new Box(canvasController.Canvas);
                        box.Text = tl.Name;
                        listenerShapes[tl.Name] = box;
                        requiredCells.Add((0, 0));

                        // TODO: FIX KLUDGE
                        var boxTextSize = box.TextSize;

                        /*
                        if (boxTextSize.Width > 100)
                        {
                            requiredCells.Add((-1, 0));
                            requiredCells.Add((1, 0));
                        }
                        */

                        switch (sourceListenerDir)
                        {
                            case Direction.Center:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, -1, Direction.Up), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (0, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, requiredCells, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Up:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, -1, Direction.Up), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (1, 0, Direction.Right), (-1, 1, Direction.Left), (1, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, requiredCells, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Down:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (0, 1, Direction.Down), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Left), (1, -1, Direction.Right), (-1, 0, Direction.Left), (0, 1, Direction.Right) };
                                    p = GetFreeCell(tl, sourceCell, requiredCells, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Left:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (-1, 0, Direction.Left), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Up), (1, -1, Direction.Up), (0, -1, Direction.Up), (0, 1, Direction.Down) };
                                    p = GetFreeCell(tl, sourceCell, requiredCells, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }

                            case Direction.Right:
                                {
                                    (int, int, Direction)[] tryPoints = new(int, int, Direction)[] { (1, 0, Direction.Right), (-1, 1, Direction.Down), (1, 1, Direction.Down), (-1, -1, Direction.Up), (1, -1, Direction.Up), (0, -1, Direction.Up), (0, 1, Direction.Down) };
                                    p = GetFreeCell(tl, sourceCell, requiredCells, tryPoints, occupiedCells.Select(oc => oc.p));
                                    break;
                                }
                        }

                        placedListeners.Add(((p.x, p.y), tl, p.dir));
                        occupiedCells.Add(((p.x, p.y), box));

                        /*
                        // TODO: FIX KLUDGE
                        if (boxTextSize.Width > 100)
                        {
                            occupiedCells.Add(((p.x - 1, p.y), null));
                            occupiedCells.Add(((p.x + 1, p.y), null));
                        }
                        */

                        // PlaceTargetListeners((p.x, p.y), tl, p.dir);
                    });

                    // Or this if we don't want to place listeners recursively as they are dropped
                    // but instead recurse after all listeners at this level are dropped.
                    notPlacedListeners.ForEach(tl =>
                    {
                        var placed = placedListeners.Single(pl => pl.listener == tl);
                        PlaceTargetListeners(placed.p, tl, placed.dir);
                    });
                });
            }

            void PlaceShapes()
            {
                int cx = canvasController.Canvas.Width / 2;
                int cy = canvasController.Canvas.Height / 2;

                occupiedCells.Where(oc => oc.el != null).ForEach(oc =>
                {
                    int x = oc.p.x * 150 + cx;
                    int y = oc.p.y * 75 + cy;
                    oc.el.DisplayRectangle = new Rectangle(x, y, (oc.el.TextSize.Width + 10).to_i(), 30);
                    // oc.el.DisplayRectangle = new Rectangle(x, y, 100, 30);
                });
            }

            void GetConnectionAnchorPoints(GraphicElement sourceElement, GraphicElement targetElement, out Point p1, out Point p2)
            {
                if (sourceElement.DisplayRectangle.Y < targetElement.DisplayRectangle.Y)
                {
                    p1 = sourceElement.DisplayRectangle.BottomMiddle();
                    p2 = targetElement.DisplayRectangle.TopMiddle();
                }
                else if (sourceElement.DisplayRectangle.Y > targetElement.DisplayRectangle.Y)
                {
                    p1 = sourceElement.DisplayRectangle.TopMiddle();
                    p2 = targetElement.DisplayRectangle.BottomMiddle();
                }
                else if (sourceElement.DisplayRectangle.X < targetElement.DisplayRectangle.X)
                {
                    p1 = sourceElement.DisplayRectangle.RightMiddle();
                    p2 = targetElement.DisplayRectangle.LeftMiddle();
                }
                else
                {
                    p1 = sourceElement.DisplayRectangle.LeftMiddle();
                    p2 = targetElement.DisplayRectangle.RightMiddle();
                }
            }

            // var listeners = Model.GetListeners();
            var listeners = otherRouter.GetAllListeners();

            // Start with entry point type.
            CCListener listener = listeners.Single(l => l.Name == startingListenerName);
            Direction dir = Direction.Center;
            placedListeners.Add(((0, 0), listener, Direction.Center));
            box = new Box(canvasController.Canvas);
            box.Text = listener.Name;
            listenerShapes[listener.Name] = box;
            occupiedCells.Add(((0, 0), box));

            // TODO: FIX KLUDGE
            var size = box.TextSize;
            if (size.Width > 100)
            {
                occupiedCells.Add(((-1, 0), null));
                occupiedCells.Add(((1, 0), null));
            }

            PlaceTargetListeners((0, 0), listener, dir);
            PlaceShapes();

            occupiedCells.ForEach(oc => canvasController.AddElement(oc.el));

            var connectors = new List<GraphicElement>();

            listeners.ForEach(l =>
            {
                var publishes = l.GetContextsPublished();

                if (listenerShapes.TryGetValue(l.Name, out GraphicElement sourceElement))
                {
                    publishes.ForEach(context =>
                    {
                        var listenerTypes = otherRouter.GetListeners(context);

                        listenerTypes.ForEach(lt =>
                        {
                            if (listenerShapes.TryGetValue(lt.Name, out GraphicElement targetElement))
                            {
                                GetConnectionAnchorPoints(sourceElement, targetElement, out Point p1, out Point p2);
                                var connector = new DiagonalConnector(canvasController.Canvas, p1, p2);
                                connector.Text = context; // String.Join(", ", publishes); // context;
                                connector.TextAlign = ContentAlignment.MiddleCenter;
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
        }
    }
}
