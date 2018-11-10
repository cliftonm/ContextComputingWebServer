namespace ContextComputing
{
    public class LogInfo
    {
        public string Message { get; protected set; }

        public LogInfo(string message)
        {
            Message = message;
        }
    }
}
