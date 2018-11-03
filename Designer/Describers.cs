using System.Linq;
using System.Windows.Forms;

using ContextComputing;

namespace Designer
{
    public class ShowListeners : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, ListBox listBox)
        {
            var listeners = Model.GetListeners();
            listBox.BeginInvoke(() => listBox.Items.AddRange(listeners.OrderBy(l => l.Name).Select(l => l.Name).ToArray()));
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
        public void Execute(ContextRouter router, ContextItem item, (ListBox listBoxListeners, ListBox listBoxParameters, TextBox textBox) data)
        {
            data.listBoxListeners.BeginInvoke(() =>
            {
                string name = data.listBoxListeners.SelectedItem.ToString();
                router.Publish(nameof(ShowListenerSelection), (data.textBox, name));
                router.Publish(nameof(ShowListenerParameters), (data.listBoxParameters, name));
            });
        }
    }

    public class ShowListenerSelection : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (TextBox textBox, string name) data)
        {
            data.textBox.BeginInvoke(() => data.textBox.Text = data.name);
        }
    }

    public class ShowListenerParameters : IContextComputingListener
    {
        public void Execute(ContextRouter router, ContextItem item, (ListBox listBox, string name) data)
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
}
