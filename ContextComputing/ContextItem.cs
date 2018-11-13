namespace ContextComputing
{
    public class ContextItem
    {
        public string Context { get; protected set; }
        public object Data { get; protected set; }
        public AsyncContext AsyncContext { get; protected set; }

        public ContextItem(string context, object data, AsyncContext asyncContext)
        {
            Context = context;
            Data = data;
            AsyncContext = asyncContext;
        }
    }
}
