using System.Diagnostics;
using XPlaneConnector;


namespace XPlaneCommandLineStart
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ConsoleDebug.RunAsync(args).GetAwaiter().GetResult();
        }
    }
    public class ConsoleDebug
    {
        public static async Task RunAsync(string[] args)
        {
            var connector = new XPlaneConnector.XPlaneConnector(ip: "192.168.29.220", xplanePort: 49000); // Default IP 127.0.0.1 Port 49000
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 1, (element, value) =>
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
            });

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 1, (e, v) =>
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {v}");
            });


            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftViewAcfTailnum, 5, (element, value) =>
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}"); 
            });

            await connector.Start();
        }



        private static void OnCompassHeadingMethod(DataRefElement e, float val)
        {
            try
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {val}");
            }
            catch (Exception ex)
            {
                Debug.Print("Exception in OnCompassHeadingMethod: {0}", ex.ToString());
                throw;
            }
        }
    }
}

