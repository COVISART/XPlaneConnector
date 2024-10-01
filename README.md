# XPlaneNexus

## VERSION 2.0

This project is a fork from Max Ferretti's XPlane Connector version 1.3

Targets .NET 8.0
Adds subscription to any dataref using its: full name (looks like a path)
Adds subscription to string-datarefs using:  full name, frequency, and buffer size for string datarefs.  An event is raised when the string has been updated

The Suggested method to get the dataref names is to use DataRefTool plugin for XPlane <https://datareftool.com/>.  Once the desired dataref has been found, use the **edit button** to view the buffer size for stringDatarefs.

Uses a single client instance to both send and receive data as XPlane 'sends the data right back to the IPaddress and port that requested the data'

Adds an XPlane discovery function.  Creates a List of Discovered instances of XPlane that can be used to connect to the desired instance.  Use of this multicast function on Apple IOS devices requires 'multicast entitlement' from Apple Developer.

Example Usage:

```C#
var connector = new XPlaneConnector("192.168.29.100,49000"); // Default IP 127.0.0.1 Port 49000
connector.Subscribe(
                "sim/cockpit2/gauges/indicators/compass_heading_deg_mag",
                frequency: 5,
                OnCompassHeadingUpdatedMethod);
connector.Subscribe(
                "sim/cockpit2/radios/indicators/gps_nav_id",
                frequency: 5,
                bufferSize: 150,
                (element, value) =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}" );
                });
```

Detailed example usage from a command line program:

```C#
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
            var  connector = new XPlaneConnector.XPlaneConnector(); //Default IP 127.0.0.1 Port 49000

            // If XPlanedDiscovery has found an instance on the network, connect to it
            if (!XPlaneDiscovery.RunningInstances.IsEmpty)
            {
                var ipaddress = XPlaneDiscovery.RunningInstances.First().Value.IPaddress;
                if (ipaddress != null)
                {
                    connector = new XPlaneConnector.XPlaneConnector(ipaddress); // Default IP 127.0.0.1 Port 49000
                }

            }


            //===========================================================================================================
            // Subscribe using the predefined datarefs using the version 1.3 syntax
            //===========================================================================================================
            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftViewAcfTailnum, 1, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}");
            });

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2RadiosIndicatorsNav1NavId, 1, (element, value) =>
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {element.DataRefPath} - {value}" );
            });

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsCompassHeadingDegMag, 5, OnCompassHeadingMethod);

            connector.Subscribe(XPlaneConnector.DataRefs.DataRefs.CockpitRadiosCom1FreqHz, 1, (e, v) =>
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


```

## UPDATE TO VERSION 1.3

Some bug fixes in this release and library separation.

All DataRefs and Commands definitions has been migrated to a separate library XPlaneConnector.DataRefs

In this way the library itself keep its size as low as possible, while DataRefs can be references only if needed.

DataRefs versioning is now consistent with X-Plane version for easier matching, and it will be easier to update DataRefs and Commands definitions separately from the Connector itself.

In the future further libraries can be added (i.e. Zibo DataRefs or other add-ons specific DataRefs).

## UPDATE TO VERSION 1.2

This version include support for .NET Standard 2.0 and .NET 4.6 in a single package

Read data and send commands to XPlane via UDP

XPlaneConnector can run on a raspberry or similar using .Net Core.
You can send commands and subscribe to DataRef.
An event OnDataRefReceived is fired every time the value of a subscribed DataRef changes.
Should XPlane crash and restart, this connector can detect that DataRefs aren't being updated and will automatically request a new subscription.

## Usage

NOTE: Every DataRef is always a float, even if the data type is different (int, bool, double, string, array).
So if you need a bool you will obtain a float that is either 0 or 1.

### Create the connector

The constructor takes the XPlane IP and port as parameters, default is 127.0.0.1 on port 49000

```C#
var connector = new XPlaneConnector(); // Default IP 127.0.0.1 Port 49000
var connector = new XPlaneConnector("192.168.0.100"); 
var connector = new XPlaneConnector("192.168.0.100", 49010); 
```

### Sending a command

Just pass the command.
A list of all the available commands has been created on
XPlaneConnector.Commands
Each command has a Description property with a brief description of its meaning.

```C#
connector.SendCommand(XPlaneConnector.Commands.ElectricalBattery1On);
```

### Subscribe to a DataRef

You can subscribe to as many DataRef you want.
In either way you have to call:

```C#
connector.Start();
```

In order to begin communication with X-Plane.
Subscribing to DataRef can happen before or after calling Start.

A list of all managed DataRefs has been created inside:

```C#
XPlaneConnector.DataRefs
```

Each DataRef has a Description property with a brief description of its meaning.

To obtain DataRef value use the DataRef event:
For DataRef "sim/cockpit/radios/com1_stdby_freq_hz" use XPlaneConnector.DataRefs.CockpitRadiosCom1FreqHz

```C#
connector.Subscribe(XPlaneConnector.DataRefs.CockpitRadiosCom1FreqHz, 5, (e, v) => {

    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRef} - {v}");
});
```

### Strings (NEW)

If you need a string (example: sim/aircraft/view/acf_tailnum) it is managed as an array of floats containing an ASCII code on each value.
Subscribing to sim/aircraft/view/acf_tailnum won't give you the tailnumber.
In order to get the complete string it's necessary to subscribe to each character individually.
Subscribing to sim/aircraft/view/acf_tailnum[0], sim/aircraft/view/acf_tailnum[1]... and so on (this DataRef is 40 byte long).
A new class StringDataRefElement has been created to automatically manage this process.
See below for usage.

```C#
// XPlaneConnector.DataRefs.AircraftViewAcfTailnum is a StringDataRef, in this case value is a string, not a float
connector.Subscribe(XPlaneConnector.DataRefs.AircraftViewAcfTailnum, 5, (element, value) =>
{

    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} - {e.DataRef} - {v}"); // v is a string
});
```

NOTE: You must have already subscribed to a DataRef using the Subscribe method.

### Press&Hold Commands (NEW 2020)

For commands that have a Press&Hold behavior like the Ignite command, multiple calls of the SendCommand method is required.
To simplify this, there's a new method StartCommand that handle the required code in a parallel Task.
It returns a CancellationTokenSource, to stop the Command just call Cancel on this token.

```C#

var token = connector.StartCommand(XPlaneConnector.Commands.EnginesEngageStarters);
// Do other things 
// ...
// When you want it to stop
token.Cancel();

```
