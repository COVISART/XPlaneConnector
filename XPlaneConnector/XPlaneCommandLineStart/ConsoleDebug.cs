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
            //var connector = new XPlaneConnector.XPlaneConnector(ip: "192.168.29.220", xplanePort: 49000); // Default IP 127.0.0.1 Port 49000
            var connector = new XPlaneConnector.XPlaneConnector(); // Default IP 127.0.0.1 Port 49000

            // connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftViewAcfTailnum, 1, (element, value) =>
            // {
            //     Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
            // });

            // connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 1, (element, value) =>
            // {
            //     Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}" );
            // });

            // //connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);

            // connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 1, (e, v) =>
            // {
            //     Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {v}" );
            // });


            //------------------------------------
            // Test for the new methods which accept a dataref name (path) rather than the precompiled dataref element
            //------------------------------------
            connector.Subscribe(
                "sim/aircraft/view/acf_tailnum",
                frequency: 5,
                bufferSize: 40,
                (element, value) =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
                });

            connector.Subscribe(
                "sim/cockpit2/radios/indicators/gps_nav_id",
                frequency: 5,
                bufferSize: 150,
                (element, value) =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}" );
                });

            connector.Subscribe(
                "sim/cockpit2/gauges/indicators/compass_heading_deg_mag",
                frequency: 5,
                OnCompassHeadingMethod);

            connector.Subscribe(
                "sim/cockpit2/autopilot/altitude_readout_preselector",
                frequency: 5,
                (element, value) =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
                });

            await connector.Start();

        }



        private static void OnCompassHeadingMethod(DataRefElement e, float val)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {val}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in OnCompassHeadingMethod: {0}\n", ex.ToString());
                throw;
            }
        }
    }
}

