# XPlaneConnector

## UPDATE TO VERSION 2.0 by DarwinIcesurfer

Written for .NET 8.0
Adds the ability to specify any dataref using its full name (looks like a path), frequency and buffer size for string datarefs

The Suggested method to get the datarefs is to use DataRefTool plugin for XPlane <https://datareftool.com/>.  Once the desired dataref
has been found, use the **edit button** to view the buffer size needed for stringDatarefs.

Subscribe to a string dataref, rather than the individual characters of the string.  In this version the string will be updated as soon
as the data is available from XPlane rather than waiting for a timer to reset.

Uses a single client instance to both send and receive data.

Adds an XPlane discovery function.  Defaults to the first detected instance of XPlane that is sending BECN.  Creates a List of Discovered
instances of XPlane that can be used to connect to the desired instance.  Use of this multicast function on Apple IOS devices requires
a 'multicast entitlement' from Apple Developer.

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
