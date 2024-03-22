using System.Diagnostics;
using XPlaneConnector;


namespace XPlaneCommandLineStart
{
    public class ConsoleDebug
    {
        public static void Main(string[] args)
        {
            var connector = new XPlaneConnector.XPlaneConnector(ip: "192.168.29.220", xplanePort: 49000); // Default IP 127.0.0.1 Port 49000
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 5, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRef} - {value}"); // v is a string
            });

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 5, (e, v) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRef} - {v}");
            });


            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftViewAcfTailnum, 5, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRef} - {value}"); // v is a string
            });

            connector.Start();


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

