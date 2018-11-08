namespace ContextComputing
{
    public class PendingContext
    {
        public string ContextName { get; protected set; }
        public object Data { get; protected set; }
        public bool Posted { get; protected set; }
        public bool IsStatic { get; protected set; }

        public PendingContext(string contextName)
        {
            ContextName = contextName;
        }

        public void Post(object data, bool isStatic)
        {
            Data = data;
            Posted = true;
            IsStatic = isStatic;
        }

        public void Clear()
        {
            if (!IsStatic)
            {
                Data = null;
                Posted = false;
            }
        }
    }
}
