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
        public string? DataRef { get; set; }

        /// <summary>
        ///  Frequency at which XPlane will send the characters for the string
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// The defined string length for the dataref value
        /// </summary>
        public int StringLength { get; set; }

        /// <summary>
        /// Holds the string as it is being built, and value that is returned once all characters have been received
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The time since the most recent character was processed
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// The current Value must be older than the AgeThreshold before it will be processed as a new string
        /// </summary>
        private TimeSpan AgeThreshold = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The number of Characters that have been processed since it was determined that a new value was received
        /// </summary>
        private int CharactersProcessed;

        public bool IsStringFillingComplete
        {
            get
            {
                return CharactersProcessed >= StringLength;
            }
        }
        public event Action<StringDataRefElement,string>?  OnValueChange;

        /// <summary>
        /// When a new character is received, it will come with an index (into all the datarefs that have been created) and the character.  The Update
        /// method collates the individual characters into a string 
        /// </summary>
        /// <param name="index">The dataElement index (unique for all UDP datarefs that is returned)</param>
        /// <param name="character">Character to be added to the string</param>
        public void Update(int index, float floatCharacter)
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
                    _inProcessArray[index] = floatCharacter;
                    CharactersProcessed++;
                }

                // if all the characters have been processed, convert the array to a string and update the value
                if(CharactersProcessed == StringLength){
                    LastUpdateTime = DateTime.Now;
                    StringBuilder builder = new StringBuilder();
                    foreach (var code in _inProcessArray)
                    {
                        char character = (char)code;
                        builder.Append(character);
                    }

                    string recentlyReceivedString = builder.ToString();

                    // Only emit the signal for a value change if the recentlyReceivedString is different than the current value
                    if(recentlyReceivedString != Value){
                        Value = recentlyReceivedString;
                        OnValueChange?.Invoke(this,Value);
                    }
                }

                // var fireEvent = !IsStringFillingComplete;

                // if (!IsStringFillingComplete)
                //     CharactersProcessed++;

                // if (intCharacter > 0)
                // {
                //     if (Value.Length <= index)
                //         Value = Value.PadRight(index + 1, ' ');

                //     var current = Value[index];
                //     if (current != intCharacter)
                //     {
                //         Value = Value.Remove(index, 1).Insert(index, character.ToString());
                //         fireEvent = true;
                //     }
                // }

                // if (IsStringFillingComplete && fireEvent)
                // {
                //     OnValueChange?.Invoke(this, Value);
                //     CharactersProcessed = 0;
                // }
            }
        }

        public StringDataRefElement()
        {
            CharactersProcessed = 0;
            Value = "";
        }


        /// <summary>
        /// Create a String DataRefElement with parameters instead of a Dataref
        /// </summary>
        /// <param name="datarefPath"></param><<example>sim/cockpit2/radios/indicators/nav1_nav_id</example>
        /// <param name="frequency">Frequency at which XPlane will send the characters for the string</param>
        /// <param name="stringLength">The defined string length for the dataref value</param>
        public StringDataRefElement(string datarefPath, int frequency = 5, int stringLength = 0)
        {
            try
            {
                if (datarefPath == null)
                    throw new ArgumentException("datarefPath is null");

                if (stringLength == 0)
                    throw new ArgumentException("string length for the dataref value can not be zero");

                CharactersProcessed = 0;
                Value = "";
                DataRef = datarefPath;
                Frequency = frequency;
                StringLength = stringLength;
                _inProcessArray = new float[StringLength];
                LastUpdateTime = DateTime.MinValue;
            }
            catch(Exception ex){
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
