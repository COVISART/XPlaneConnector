using System;
using System.Diagnostics;
using System.Text;

namespace XPlaneNexus
{
    public class StringDataRefElement
    {
        private static readonly object lockElement = new object();
        private float[] _inProcessArray = new float[0];

        /// <summary>
        /// The full dataref name in a 'path' type format due to the '/' characters in the name
        /// <example>sim/cockpit2/radios/indicators/nav1_nav_id</example>
        /// </summary>
        public string? DataRefPath { get; set; }

        /// <summary>
        ///  Frequency at which XPlane will send the individual characters to fill the buffer
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// The defined buffer size to contain the dataref value.  The buffer may be longer than the string it contains
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// The string that was created once all characters have been received
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The time since the most recent character was processed
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// The timespan used to determine if the current buffer is 'stale'. 
        /// </summary>
        private TimeSpan AgeThreshold = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The number of Characters that have been processed since the start of the buffer filling task
        /// </summary>
        private int _charactersProcessed;

        /// <summary>
        /// Clients can subscribe to this Action which will emit a signal when the ENTIRE STRING is updated 
        /// </summary>
        public event Action<StringDataRefElement, string>? OnValueChange;

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        public StringDataRefElement()
        {
            _charactersProcessed = 0;
            Value = "";
        }

        /// <summary>
        /// Creates a new array to hold character values when the old array is stale, or  characters for a new string begin to 
        /// transfer.  It will also cause a signal to be emitted once the complete string has been received
        /// </summary>
        /// <param name="characterPosition">The position of the character received WITHIN the string that is being built</param>
        /// <param name="floatCharacter">Character that arrives from XPLane as a float</param>
        public void Update(int characterPosition, float floatCharacter)
        {
            lock (lockElement)
            {
                // if the currently being processed array is old, assume all the characters didn't get received and clear out the array and
                // reset character counter
                if ((DateTime.Now - LastUpdateTime) > AgeThreshold)
                {
                    _charactersProcessed = 0;
                    _inProcessArray = new float[BufferSize];
                    // The last UpdateTime will be updated when starting to fill the array and when filling is completed
                    // that way if it is only partially filled by the time it has aged-out, it will be reset
                    LastUpdateTime = DateTime.Now;
                    //Debug.Print("new string being created for {0}", this.DataRefPath);
                }

                // put the character into the array at the correct location and update the character counter
                _inProcessArray[characterPosition] = floatCharacter;
                _charactersProcessed++;
                //Debug.Print("[{0}]{1}", characterPosition, (char)floatCharacter);

                // if all the characters have been processed, convert the array to a string and update the value
                if (_charactersProcessed == BufferSize)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var code in _inProcessArray)
                    {
                        char character = (char)code;
                        if (character != 0)
                            builder.Append(character);
                    }

                    string recentlyReceivedString = builder.ToString();

                    // Only emit the signal for a value change if the recentlyReceivedString is different than the current value
                    if (recentlyReceivedString != Value)
                    {
                        Value = recentlyReceivedString;
                        OnValueChange?.Invoke(this, Value);
                    }

                    // reset characters processed this will allow waiting for a new string
                    _charactersProcessed = 0;
                    LastUpdateTime = DateTime.Now;
                    _inProcessArray = new float[BufferSize];
                }
            }
        }

        /// <summary>
        /// Create a String DataRefElement for each character that is a part of a string dataref
        /// </summary>
        /// <param name="datarefPath"></param><<example>sim/cockpit2/radios/indicators/nav1_nav_id</example>
        /// <param name="frequency">Frequency at which XPlane will send the characters for the string</param>
        /// <param name="stringLength">The defined string length for the dataref value</param>
        public StringDataRefElement(string datarefPath, int frequency = 1, int stringLength = 0)
        {
            try
            {
                if (datarefPath == null)
                    throw new ArgumentException("datarefPath is null");

                if (stringLength == 0)
                    throw new ArgumentException("string length for the dataref value cannot be zero");

                _charactersProcessed = 0;
                Value = "";
                DataRefPath = datarefPath;
                Frequency = frequency;
                BufferSize = stringLength;
                _inProcessArray = new float[BufferSize];
                LastUpdateTime = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
