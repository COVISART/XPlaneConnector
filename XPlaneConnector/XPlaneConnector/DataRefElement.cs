using System;

namespace XPlaneConnector
{
    public class DataRefElement
    {
        private static object lockElement = new object();
        private static int current_id = 0;
        public int Id { get; set; }
        public string? DataRefPath { get; set; }
        /// <summary>
        /// Character position within the string.  Assigned only to datarefs returning a string value
        /// </summary>
        public int? CharacterPosition { get; set; }
        public int Frequency { get; set; } = 5;
        public bool IsInitialized { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
        public string? Units { get; set; }
        public string? Description { get; set; }
        public float Value { get; set; } = float.MinValue;
        public event Action<DataRefElement, float>? OnValueChange;

        public DataRefElement()
        {
            lock (lockElement)
            {
                Id = ++current_id;
            }
            IsInitialized = false;
        }

        public void ClearSubscriptions(){
            OnValueChange = null;
        }

        public TimeSpan Age
        {
            get
            {
                return (DateTime.Now - LastUpdate);
            }
        }

        /// <summary>
        /// Updates a single value.  In the case of a string dataref, this is one character,  If the value is numeric, it will be converted
        /// from the float that is passed in
        /// </summary>
        /// <param name="id">unique id for this dataref for THIS SESSION with xplane</param>
        /// <param name="value">A float value that can be converted into the appropriate numeric value or character</param>
        /// <returns></returns>
        public void Update(float value)
        {
            LastUpdate = DateTime.Now;

            if (value != Value)
            {
                Value = value;
                IsInitialized = true;
                OnValueChange?.Invoke(this, Value);
            }
        }
    }
}
