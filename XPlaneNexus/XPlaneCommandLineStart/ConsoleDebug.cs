using System.Diagnostics;
using XPlaneNexus;


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

        private const float TESTING_UNSUBSCRIBE_TIMEOUT = 30;
        public static async Task RunAsync(string[] args)
        {
            // Discover XPlane instances and subscribe to some of the signals
            _ = XPlaneDiscovery.StartBeaconReceiverAsync();
            XPlaneDiscovery.OnXplaneRunningInstanceDiscovered += (value) => Debug.Print("XPlaneDiscovered {0} {1}\n", value.ComputerName, value.IPaddress);
            XPlaneDiscovery.OnXplaneRunningInstanceInactive += (value) => Debug.Print("Xplane instance removed {0} {1}\n", value.ComputerName, value.IPaddress);

            // wait until at least one instance of XPlane is discovered
            while (XPlaneDiscovery.RunningInstances.Count == 0)
            {
                Thread.Sleep(1000);
            }

            // connect to the loopback default connector
            var  connector = new XPlaneConnector(); //Default IP 127.0.0.1 Port 49000

            // If XPlanedDiscovery has found an instance on the network, connect to it
            if (!XPlaneDiscovery.RunningInstances.IsEmpty)
            {
                var ipaddress = XPlaneDiscovery.RunningInstances.First().Value.IPaddress;
                if (ipaddress != null)
                {
                    connector = new XPlaneConnector(ipaddress); // Default IP 127.0.0.1 Port 49000
                }

            }


            //===========================================================================================================
            // Subscribe using the predefined datarefs using the version 1.3 syntax
            //===========================================================================================================
            connector.Subscribe(XPlaneNexus.DataRefs.DataRefs.AircraftViewAcfTailnum, 1, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
            });

            connector.Subscribe(XPlaneNexus.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 1, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}" );
            });

            connector.Subscribe(XPlaneNexus.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);

            connector.Subscribe(XPlaneNexus.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 1, (e, v) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRefPath} - {v}" );
            });


            //===========================================================================================================
            // Subscribe using the Syntax introduced in version 2.0 which accept a dataref name (path) 
            // rather than the precompiled dataref element
            //===========================================================================================================
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
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
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

            // Set up a timers to trigger unsubscribe actions after XX seconds
            // This is required before the connector is started as it runs perpetually once started
            var timer = new System.Threading.Timer(_ =>
            {
                connector.Unsubscribe("sim/cockpit2/gauges/indicators/compass_heading_deg_mag", OnCompassHeadingMethod);
                Debug.Print("Unsubscribed from sim/cockpit2/gauges/indicators/compass_heading_deg_mag\n");
            }, null, TimeSpan.FromSeconds(TESTING_UNSUBSCRIBE_TIMEOUT), TimeSpan.Zero);


            var timer2 = new System.Threading.Timer(_ =>
            {
                connector.Unsubscribe(
                    "sim/cockpit2/autopilot/altitude_readout_preselector",
                    (element, value) =>
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
                    });
                Debug.Print("Unsubscribed from sim/cockpit2/autopilot/altitude_readout_preselector\n");
            }, null, TimeSpan.FromSeconds(TESTING_UNSUBSCRIBE_TIMEOUT * 2), TimeSpan.Zero);

            float obs_heading = 150;
            var timer3 = new System.Threading.Timer(_ =>
            {
                connector.SetDataRefValue("sim/cockpit/radios/nav1_obs_degm", obs_heading++);
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // Start the connector -- will run until
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

