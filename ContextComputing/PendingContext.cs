namespace ContextComputing
{
    public class PendingContext
    {
        public string ContextName { get; protected set; }
        public object Data { get; protected set; }
        public bool Posted { get; protected set; }
        public bool IsStatic { get; protected set; }
        public virtual bool IsDependentContext { get { return false; } }

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

    public class DependentPendingContext : PendingContext
    {
        public override bool IsDependentContext { get { return true; } }

        public DependentPendingContext(string contextName) : base(contextName)
        {
        }
    }
}
