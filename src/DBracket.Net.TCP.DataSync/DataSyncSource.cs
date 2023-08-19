using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace DBracket.Net.TCP.DataSync
{
    public class DataSyncSource
    {
        // ToDo:
        //      - Set buffer zero after every cycle

        #region "----------------------------- Private Fields ------------------------------"C
        internal static string OBJECT_SEPERATOR = $"|;";
        internal static char[] C_OBJECT_SEPERATOR = new char[] { '|', ';' };
        internal static string ID_SEPERATOR = $";_";
        internal static char[] C_ID_SEPERATOR = new char[] { ';', '_' };
        internal static string VALUE_SEPERATOR = $";*";
        internal static char[] C_VALUE_SEPERATOR = new char[] { ';', '*' };
        internal static char[] C_MESSAGE_END_SEPERATOR = new char[] { '|', '|' };

        private ulong _currentIdentifier = 1;

        private Client _client = new Client();

        private Stopwatch _sw = Stopwatch.StartNew();
        private Stopwatch _swCycle = Stopwatch.StartNew();
        private DataSyncSourceSettings _settings;

        private bool _syncDataActive = false;
        string[] dataSet;

        private ISyncList _syncObjectList;
        private Type _syncObjectType;
        private PropertyInfo[] _propInfos;
        internal static Dictionary<string, int> _propertyIndexLookupTable = new();

        private string ID = "1";
        private bool _receiverHandled = true;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncSource(DataSyncSourceSettings settings, ISyncList syncObject)
        {
            if (settings == null)
                throw new ArgumentNullException();
            if (syncObject == null)
                throw new ArgumentNullException();

            _syncObjectList = syncObject;
            _settings = settings;

            Init(settings.IPAddress, settings.Port);
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public void SetSettings(DataSyncSourceSettings settings)
        {
            // ToDo
            //      - Stop current sync operation
            _settings = settings;
            _client.SetServerIPAddress(settings.IPAddress.ToString());
            _client.SetPortNumber(settings.Port.ToString());
            _client.NewMessageReceived += HandleMessageReceived;
        }

        public void StartSyncData()
        {
            _client.ConnectToServer().Wait();
            _syncDataActive = true;
            Task.Run(() => SyncDataWithTarget());
        }

        public void StopSyncData()
        {
            _syncDataActive = false;
            _client.CloseAndDisconnect();
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void Init(IPAddress address, int port)
        {
            // Get object infos
            _syncObjectType = _syncObjectList.GetType().GenericTypeArguments[0];
            _propInfos = _syncObjectType.GetProperties().Where(x => x.GetCustomAttribute<SyncPropertyAttribute>() is not null).ToArray();
            for (int index = 0; index < _propInfos.Length; index++)
            {
                _propertyIndexLookupTable.Add(_propInfos[index].Name, index);
            }

            // Initialize id's of the objects
            foreach (SyncObject syncObject in _syncObjectList)
            {
                syncObject.InitObject(_currentIdentifier++, _propInfos);
            }

            // Initialize the client
            _client = new Client();
            _client.ConnectionChanged += HandleConnectionStateChanged;
            _client.SetServerIPAddress(address.ToString());
            _client.SetPortNumber(port.ToString());
        }

        private void SyncDataWithTarget()
        {
            dataSet = new string[_syncObjectList.Count];
            uint syncMessageLength = 0;
            char[] syncMessage = null;

            try
            {

                while (_syncDataActive)
                {
                    // If there is nothing to sync, do nothing
                    if (_syncObjectList is null || _syncObjectList?.Count == 0)
                    {
                        Task.Delay(100).Wait();
                        continue;
                    }

                    // Wait until the current List updates are done
                    while (_syncObjectList.IsUpdating || !_receiverHandled)
                    {
                        if (!_syncDataActive)
                        {
                            return;
                        }
                        Task.Delay(10).Wait();
                    }

                    // Start new Cycle
                    _receiverHandled = false;
                    _syncObjectList.IsSourceUpdating = true;
                    _sw.Restart();
                    _swCycle.Restart();


                    // Calculate the length of the message
                    syncMessageLength = 0;
                    foreach (SyncObject syncObject in _syncObjectList)
                    {
                        syncMessageLength += syncObject._syncMessageLength;
                    }

                    // Init message array
                    if (syncMessage is null || (syncMessageLength + 12) > syncMessage?.Length)
                    {
                        syncMessage = new char[syncMessageLength + 12];
                    }

                    uint currentPosition = 12;
                    // ToDo:
                    //      - Parallel?
                    for (int syncObjectNumber = 0; syncObjectNumber < _syncObjectList.Count; syncObjectNumber++)
                    {
                        var syncObject = (SyncObject)_syncObjectList[syncObjectNumber];
                        AddSyncObjectToMessage(syncObjectNumber, syncObject, currentPosition);
                        currentPosition += syncObject._syncMessageLength;
                    }

                    AddMessageLengthToMessage((uint)(syncMessage.Length - 34 + 2));
                    AddMessageEndSymbol();

                    Debug.WriteLine($"time to process data: {_swCycle.Elapsed}");


                    // Send data
                    if (_client.IsConnected)
                    {
                        try
                        {
                            _client.SendToServer(syncMessage).Wait();
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    Debug.WriteLine($"time to process and send data: {_swCycle.Elapsed}");
                    while (_swCycle.ElapsedMilliseconds < _settings.UpdateCycleTimeMs)
                    {
                        var waitMs = _settings.UpdateCycleTimeMs - (int)_swCycle.ElapsedMilliseconds;
                        Task.Delay(waitMs).Wait();
                    }

                    CycleTime = _sw.Elapsed;
                    Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));
                    Debug.WriteLine($"Complete cycle time: {_sw.Elapsed}");


                    unsafe void AddSyncObjectToMessage(int syncObjectNumber, SyncObject syncObject, uint startPoint)
                    {
                        var bufferPtrOffset = startPoint;
                        fixed (char* buffer = syncMessage)
                        {
                            #region Add the id
                            fixed (char* idPtr = syncObject.ID)
                            {
                                for (int i = 0; i < syncObject.ID.Length; i++)
                                {
                                    *(buffer + bufferPtrOffset) = *(idPtr + i);
                                    bufferPtrOffset++;
                                }
                            }
                            fixed (char* idSeperatorPtr = ID_SEPERATOR)
                            {
                                for (int i = 0; i < ID_SEPERATOR.Length; i++)
                                {
                                    *(buffer + bufferPtrOffset) = *(idSeperatorPtr + i);
                                    bufferPtrOffset++;
                                }
                            }
                            #endregion


                            #region Add Property values
                            for (int y = 0; y < syncObject._syncPropertyValues.Length; y++)
                            {
                                fixed (char* syncPropertyValuePtr = syncObject._syncPropertyValues[y])
                                {
                                    // Add the Value
                                    for (int i = 0; i < syncObject._syncPropertyValues[y].Length; i++)
                                    {
                                        *(buffer + bufferPtrOffset) = *(syncPropertyValuePtr + i);
                                        bufferPtrOffset++;
                                    }


                                    // Check if the last value was not the last value
                                    if (y < syncObject._syncPropertyValues.Length - 1)
                                    {
                                        // Not the last value, insert Seperator
                                        fixed (char* valueSeperatorPtr = VALUE_SEPERATOR)
                                        {
                                            for (int i = 0; i < VALUE_SEPERATOR.Length; i++)
                                            {
                                                *(buffer + bufferPtrOffset) = *(valueSeperatorPtr + i);
                                                bufferPtrOffset++;
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion


                            #region Add the object Seperator
                            // Check if the last value was not the last value
                            if (syncObjectNumber < _syncObjectList.Count - 1)
                            {
                                // Not the last value, insert Seperator
                                fixed (char* objectSeperatorPtr = OBJECT_SEPERATOR)
                                {
                                    for (int i = 0; i < OBJECT_SEPERATOR.Length; i++)
                                    {
                                        *(buffer + bufferPtrOffset) = *(objectSeperatorPtr + i);
                                        bufferPtrOffset++;
                                    }
                                }
                            }
                            #endregion
                        }
                    }

                    unsafe void AddMessageLengthToMessage(uint length)
                    {
                        string messageLength = length.ToString();
                        fixed (char* syncMessagePtr = syncMessage)
                        {
                            fixed (char* strLengthPtr = messageLength)
                            {
                                for (int i = 0; i < messageLength.Length; i++)
                                {
                                    *(syncMessagePtr + i) = *(strLengthPtr + i);
                                }
                            }
                        }

                        for (int i = messageLength.Length; i < 12; i++)
                        {
                            syncMessage[i] = '*';
                        }
                    }

                    void AddMessageEndSymbol()
                    {
                        syncMessage[syncMessage.Length - 2] = '|';
                        syncMessage[syncMessage.Length - 1] = '|';
                    }
                }

            }
            catch (Exception e)
            {

            }
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleConnectionStateChanged(bool newState)
        {
            IsConnectedToTarget = newState;
            _receiverHandled = true;
            Task.Run(() => ConncetionStateChanged?.Invoke(newState));
        }

        private void HandleMessageReceived(string message)
        {
            if (message == "d")
            {
                _receiverHandled = true;
            }
        }
        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public TimeSpan CycleTime { get; private set; }
        public int TargetCycleTimeMs { get => _settings.UpdateCycleTimeMs; }
        public bool IsConnectedToTarget { get; private set; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event ConnectionStateChangedHandler ConncetionStateChanged;
        public delegate void ConnectionStateChangedHandler(bool state);

        public event CycleTimeChangedHandler CycleTimeChanged;
        public delegate void CycleTimeChangedHandler(TimeSpan time);
        #endregion
        #endregion
    }
}