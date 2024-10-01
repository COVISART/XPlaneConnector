namespace XPlaneNexus;

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


/// <summary>
/// Maintains a list of current XPlane instances discovered by listening for BECN packets emitted by Xplane. 
/// a periodic check is run to add and remove discovered or dropped XPlane instances
/// The Property changed signal is emitted when the list is updated
/// </summary>
public static class XPlaneDiscovery
{
    /// <summary>
    /// Multicast Address used by XPlane
    /// </summary>
    private const string MULTICAST_ADDRESS = "239.255.1.1";

    /// <summary>
    /// Mullticast port used by XPlane
    /// </summary>
    private const int BECN_PORT = 49707;

    /// <summary>
    /// The interval seconds that have elapsed since the last BECN was received that would indicate that the 
    /// xplane instance is inactive
    /// </summary>
    private const double PERIODIC_CHECK_SECONDS = 5;

    /// <summary>
    /// Subscribe to event for error and debug messages
    /// </summary>
    public static event Action<object, string>? OnLog;

    /// <summary>
    /// Signal emitted when new XPlane Running instance is discovered
    /// </summary>
    public static event Action<DiscoveredInstance>? OnXplaneRunningInstanceDiscovered;

    /// <summary>
    /// Signal is Emitted when an xplane instance goes inactive
    /// </summary>
    public static event Action<DiscoveredInstance>? OnXplaneRunningInstanceInactive;

    /// <summary>
    /// Can be Emitted when a property changed
    /// </summary>
    public static event PropertyChangedEventHandler? OnPropertyChanged;


    /// <summary>
    ///  A thread-safe dictionary that is used to hold a list of the xplane instances
    /// </summary>
    public static ConcurrentDictionary<IPAddress, DiscoveredInstance> RunningInstances { get; set; }
    = new ConcurrentDictionary<IPAddress, DiscoveredInstance>();

    /// <summary>
    /// UDPClient used as a SERVER to listen for UDP multicast broadcast packets
    /// </summary>
    private static UdpClient? _becnReceiver;



    /// <summary>
    /// Structure of the BECN sent over UDP by XPlane
    /// </summary>
    private struct BECN0structure
    {
        public byte Beacon_major_version;
        public byte Beacon_minor_version;
        public uint Application_host_id;
        public uint Version_number;
        public uint Role;
        public ushort Port;
        public string Computer_name;
    }




    /// <summary>
    /// Initializes a UDP client that will listen for XPlanes' UDP BECN packets.  When a new BECN packet is received it will
    /// be added to a Dictionary with a timestamp.  The list will be checked periodically for inactive instances
    /// </summary>
    public static async Task StartBeaconReceiverAsync()
    {
        // Set up a timer to periodically check for removed instances of XPlane
        var timer = new System.Threading.Timer(_ =>
        {
            RemoveInactiveInstancesFromDictionary();
        }, null, TimeSpan.FromSeconds(PERIODIC_CHECK_SECONDS), TimeSpan.FromSeconds(PERIODIC_CHECK_SECONDS));

        Debug.Print("Starting Beacon Receiver");

        try
        {
            _becnReceiver = new UdpClient
            {
                ExclusiveAddressUse = false
            };
            _becnReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDRESS));
            _becnReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, BECN_PORT));

            while (true)
            {
                var returnval = await Task.Run(() => _becnReceiver.ReceiveAsync());

                // Process the datagram
                var remoteIP = returnval.RemoteEndPoint.Address.ToString();
                var datagram = returnval.Buffer;

                // Check if the message prologue is BECN\0, a 'c-string'
                if (Encoding.ASCII.GetString(datagram, 0, 4) == "BECN")
                {
                    // Deserialize the beacon data into a becn_struct object
                    BECN0structure beacon;
                    var offset = 5; // skip over the BECN\0 message prologue
                    beacon.Beacon_major_version = datagram[offset];
                    beacon.Beacon_minor_version = datagram[offset + 1];
                    beacon.Application_host_id = BitConverter.ToUInt32(datagram, offset + 2);
                    beacon.Version_number = BitConverter.ToUInt32(datagram, offset + 6);
                    beacon.Role = BitConverter.ToUInt32(datagram, offset + 10);
                    beacon.Port = BitConverter.ToUInt16(datagram, offset + 14);
                    beacon.Computer_name = Encoding.ASCII.GetString(datagram, offset + 16, datagram.Length - 22);

                    // create an instance of the class that will hold the return data
                    var e = new DiscoveredInstance();
                    e.IPaddress = remoteIP;
                    e.ComputerName = beacon.Computer_name;
                    e.LastBECN_UTC = DateTime.UtcNow;

                    // Check for an existing instance in the Dictionary
                    var key = IPAddress.Parse(e.IPaddress);
                    if (RunningInstances.ContainsKey(key))
                    {
                        // Update timestamp
                        RunningInstances.AddOrUpdate(key, e, (key, oldvalue) =>
                        {
                            oldvalue.LastBECN_UTC = e.LastBECN_UTC;
                            return oldvalue;
                        });
                    }
                    else
                    {
                        // If the key isn't already in the dictionary, add it and emit the signal
                        RunningInstances.AddOrUpdate(key, e, (key, _) => e);
                        OnXplaneRunningInstanceDiscovered?.Invoke(e);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OnLog?.Invoke("StartBeaconReceiverAsync Error: ", ex.ToString());
            ex.Data.Add("user message", "Thrown from StartBeaconReceiver");
            throw;
        }
    }

    /// <summary>
    /// If a new beacon has not been received within the number of seconds defined in the Heartbeat
    /// field, remove the item from the RunningInstances dictionary and emit a signal.  This method will
    /// be called on a HEARTBEAT_SECONDS frequency
    /// </summary>
    private static void RemoveInactiveInstancesFromDictionary()
    {
        foreach (var kvp in RunningInstances)
        {
            if (kvp.Value.LastBECN_UTC.AddSeconds(PERIODIC_CHECK_SECONDS) < DateTime.UtcNow)
            {
                DiscoveredInstance removedInstance = kvp.Value;
                RunningInstances.TryRemove(kvp.Key, out _);
                OnXplaneRunningInstanceInactive?.Invoke(removedInstance);
            }
        }
    }
}


public class DiscoveredInstance : EventArgs
{
    public string? ComputerName { get; set; }
    public string? IPaddress { get; set; }
    public DateTime LastBECN_UTC { get; set; }
}



