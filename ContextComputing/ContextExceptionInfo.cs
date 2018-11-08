using System;

namespace ContextComputing
{
    public class ContextExceptionInfo
    {
        public Exception Exception { get; protected set; }
        public object Data { get; protected set; }
        public object AsyncContext { get; protected set; }

        public ContextExceptionInfo(Exception ex, object data, object asyncContext)
        {
            Exception = ex;
            Data = data;
            AsyncContext = asyncContext;
        }
    }
}
