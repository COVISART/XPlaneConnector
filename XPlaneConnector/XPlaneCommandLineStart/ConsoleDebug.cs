using System.Diagnostics;
using XPlaneConnector;


namespace XPlaneCommandLineStart
{
    public static class Program{
        public static void Main(string[] args){
            ConsoleDebug.RunAsync(args).GetAwaiter().GetResult();
        }
    }
    public class ConsoleDebug
    {
        public static async Task RunAsync(string[] args)
        {
            var connector = new XPlaneConnector.XPlaneConnector(ip: "192.168.29.220", xplanePort: 49000); // Default IP 127.0.0.1 Port 49000
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 5, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRef} - {value}"); // v is a string
            });

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 5, (e, v) =>
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {v}");
            });


            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftViewAcfTailnum, 5, (element, value) =>
            {
                Debug.Print($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRef} - {value}"); // v is a string
            });

            await connector.Start();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine(); // Program will wait until Enter is pressed
        }



        private static void OnCompassHeadingMethod(DataRefElement e, float val)
        {
            try
            {

                // Use call_deferred to update UI on the main thread
                //CallDeferred("UpdateUI", e.DataRef, val);
            }
            catch (Exception ex)
            {
                Debug.Print("Exception in OnCompassHeadingMethod: {0}", ex.ToString());
                throw;
            }
        }
    }
}

