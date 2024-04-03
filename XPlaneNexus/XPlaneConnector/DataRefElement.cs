using System;

namespace XPlaneConnector
{
    public class DataRefElement
    {
        /// <summary>
        /// Used to prevent other threads from accessing class during critical operations, like setting unique Id
        /// </summary>
        private static object lockElement = new object();

        /// <summary>
        /// Running static counter used in creating unique Id
        /// </summary>
        private static int current_id = 0;

        /// <summary>
        /// Unique Id assigned for each element
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The dataref name.  'Path' is used because with the '/' characters used in the name, it looks like a path
        /// </summary>
        public string? DataRefPath { get; set; }

        /// <summary>
        /// Character position within the buffer.  Assigned only to datarefs returning a character of a string
        /// </summary>
        public int? CharacterPosition { get; set; }

        /// <summary>
        /// The number of updates per second requested from XPlane
        /// </summary>
        public int Frequency { get; set; } = 5;

        /// <summary>
        /// Since float cannot be null, IsInitialized is used to determine if the Value has been updated since creation
        /// Use is deprecated as of darwinIcesurfer version 2.0
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Hold a timestamp for the last time the value was refreshed
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Used to hold descriptive information about the units.  This is mostly used with the predefined XplaneConnector.DataRefs
        /// </summary>
        public string? Units { get; set; }

        /// <summary>
        /// Used to hold descriptive information about the returned value.  This is mostly used with the predefined XplaneConnector.DataRefs
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The most recent Value for this dataref returned from XPlane
        /// </summary>
        public float Value { get; set; } = float.MinValue;

        /// <summary>
        /// Subscribe to this EventAction to be notified when the value changes
        /// </summary>
        public event Action<DataRefElement, float>? OnValueChange;

        /// <summary>
        /// CONSTRUCTOR.  Assigns the unique Id to this datarefElement
        /// </summary>
        public DataRefElement()
        {
            lock (lockElement)
            {
                Id = ++current_id;
            }
            IsInitialized = false;
        }

        /// <summary>
        /// Use this method to clear ALL subscriptions.  Used mainly during the unsubscribe operation
        /// </summary>
        public void ClearSubscriptions()
        {
            OnValueChange = null;
        }

        /// <summary>
        /// Return Time since most recent refresh from XPlane
        /// </summary>
        public TimeSpan Age
        {
            get
            {
                return (DateTime.Now - LastUpdate);
            }
        }

        /// <summary>
        /// Updates a single value.  In the case of a string dataref, this is one character,  
        /// </summary>
        /// <param name="value">A float value that can be converted into the appropriate numeric value or character</param>
        public void Update(float value)
        {
            LastUpdate = DateTime.Now;

            // If this element is part of a string array that is being returned, all the characters received from
            // Xplane must be passed on to the method that assemble the characters into a string,  regardless if they have changed or not
            // If CharacterPosition has a value assigned to it, it is part of a string
            if (CharacterPosition.HasValue)
            {
                Value = value;
                IsInitialized = true;
                OnValueChange?.Invoke(this, Value);
            }

            else if (value != Value)
            {
                Value = value;
                IsInitialized = true;
                OnValueChange?.Invoke(this, Value);
            }
        }
    }
}
