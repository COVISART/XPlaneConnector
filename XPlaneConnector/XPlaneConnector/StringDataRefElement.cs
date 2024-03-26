using System;
using System.Diagnostics;
using System.Text;

namespace XPlaneConnector
{
    public class StringDataRefElement
    {
        private static readonly object lockElement = new object();
        private float[] _inProcessArray = new float[0];

        /// <summary>
        /// The full dataref path
        /// <example>sim/cockpit2/radios/indicators/nav1_nav_id</example>
        /// </summary>
        public string? DataRefPath { get; set; }

        /// <summary>
        ///  Frequency at which XPlane will send the characters for the string
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// The defined string length for the dataref value
        /// </summary>
        public int StringLength { get; set; }

        /// <summary>
        /// The value that will be retained in this class once all characters have been received
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The time since the most recent character was processed
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// After a threshold, the string will be reset
        /// </summary>
        private TimeSpan AgeThreshold = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The number of Characters that have been processed since it was determined that a new value is being transmitted
        /// </summary>
        private int CharactersProcessed;

        /// <summary>
        /// Returns true once all the characters needed to complete the string have been received
        /// </summary>
        public bool IsStringFillingComplete
        {
            get
            {
                return CharactersProcessed >= StringLength;
            }
        }
        
        /// <summary>
        /// Clients can subscribe to this Action which will emit a signal when the entire string is updated 
        /// </summary>
        public event Action<StringDataRefElement, string>? OnValueChange;

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
                    CharactersProcessed = 0;
                    _inProcessArray = new float[StringLength];
                    // The last UpdateTime will be updated when starting to fill the array and when filling is completed
                    // that way if it is only partially filled by the time it has aged-out, it will be reset
                    LastUpdateTime = DateTime.Now;
                }

                // put the character into the array at the correct location and update the character counter
                if (floatCharacter > 0)
                {
                    _inProcessArray[characterPosition] = floatCharacter;
                    CharactersProcessed++;
                }

                // if all the characters have been processed, convert the array to a string and update the value
                if (CharactersProcessed == StringLength)
                {
                    LastUpdateTime = DateTime.Now;
                    StringBuilder builder = new StringBuilder();
                    foreach (var code in _inProcessArray)
                    {
                        char character = (char)code;
                        builder.Append(character);
                    }

                    string recentlyReceivedString = builder.ToString();

                    // Only emit the signal for a value change if the recentlyReceivedString is different than the current value
                    if (recentlyReceivedString != Value)
                    {
                        Value = recentlyReceivedString;
                        OnValueChange?.Invoke(this, Value);
                    }
                }
            }
        }

        public StringDataRefElement()
        {
            CharactersProcessed = 0;
            Value = "";
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

                CharactersProcessed = 0;
                Value = "";
                DataRefPath = datarefPath;
                Frequency = frequency;
                StringLength = stringLength;
                _inProcessArray = new float[StringLength];
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
