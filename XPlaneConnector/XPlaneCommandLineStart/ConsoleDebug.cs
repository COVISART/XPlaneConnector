using XPlaneConnector;

namespace XPlaneCommandLineStart
{
    public static class ConsoleDebug
    {
        public static void Main(string[] args)
        {
            var connector = new XPlaneConnector.XPlaneConnector(); // Default IP 127.0.0.1 Port 49000
            connector.Start();
        

            // Additional code here...
        }
    }
}

