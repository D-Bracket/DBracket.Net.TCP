using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DBracket.Net.TCP.DataSync
{
    public abstract class SyncObject : INotifyPropertyChanged
    {
        #region "----------------------------- Private Fields ------------------------------"
        internal string[] _syncPropertyValues;
        internal uint _syncMessageLength;
        #endregion



        #region "------------------------------ Constructor --------------------------------"

        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public void OnMySelfChanged(string value, [CallerMemberName] string cmn = "")
        {
            OnPropertyChanged(value, cmn);
        }

        public void OnPropertyChanged(string value, string propertyName)
        {
            try
            {
                if (DataSyncSource._propertyIndexLookupTable?.Count > 0 && DataSyncSource._propertyIndexLookupTable.ContainsKey(propertyName))
                {
                    var index = DataSyncSource._propertyIndexLookupTable[propertyName];
                    _syncPropertyValues[index] = value;
                    _syncMessageLength = CountSyncMessageLength();
                }
            }
            catch (Exception ex)
            { 
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        internal void InitObject(ulong id, PropertyInfo[] syncProperties)
        {
            ID = id.ToString();
            _syncPropertyValues = new string[syncProperties.Length];

            // Werte initialisieren
            foreach (var syncProperty in syncProperties)
            {
                var index = DataSyncSource._propertyIndexLookupTable[syncProperty.Name];
                var value = syncProperty.GetValue(this);
                string s;
                if (value is null)
                {
                    s = string.Empty;
                }
                else
                {
                    var tmp = value.ToString();
                    s = tmp is null ? string.Empty : tmp;
                }
                _syncPropertyValues[index] = s;
            }

            _syncMessageLength = CountSyncMessageLength();
        }

        internal void SetIndex(ulong id)
        {
            ID = id.ToString();
        }

        internal void SetIndex(string id)
        {
            ID = id;
        }

        internal uint CountSyncMessageLength()
        {
            uint length = (uint)ID.Length + (uint)DataSyncSource.ID_SEPERATOR.Length;
            foreach (var property in _syncPropertyValues)
            {
                length += (uint)property.Length + (uint)DataSyncSource.VALUE_SEPERATOR.Length;
            }

            // Remove last value seperator
            length -= (uint)DataSyncSource.VALUE_SEPERATOR.Length;

            // Add object seperator
            length += (uint)DataSyncSource.OBJECT_SEPERATOR.Length;

            return length;
        }

        internal void UpdateSyncMessage()
        {
            foreach (var syncProperty in _syncPropertyValues)
            {

            }
        }

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public string ID { get; private set; }
        public bool NeedsToBeDeleted { get; set; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
        #endregion
    }
}