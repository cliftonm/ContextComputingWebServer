namespace ContextComputing
{
    public class ContextItem
    {
        public string Context { get; protected set; }
        public object Data { get; protected set; }
        public object AsyncContext { get; protected set; }

        public ContextItem(string context, object data, object asyncContext)
        {
            Context = context;
            Data = data;
            AsyncContext = asyncContext;
        }
    }
}
