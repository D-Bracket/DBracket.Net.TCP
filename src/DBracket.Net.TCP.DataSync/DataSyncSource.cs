using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace DBracket.Net.TCP.DataSync
{

    // strings in SyncObject of properties with complete length, updates when properties update
    // int array with starting point in message
    // pointer that parallel write chars in char buffer




    public class DataSyncSource
    {
        #region "----------------------------- Private Fields ------------------------------"C
        internal static string OBJECT_SEPERATOR = $"|;";
        internal static string ID_SEPERATOR = $";_";
        internal static string VALUE_SEPERATOR = $";*";

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
            _settings = settings;
            _client.SetServerIPAddress(settings.IPAddress.ToString());
            _client.SetPortNumber(settings.Port.ToString());
        }

        public async void StartSyncData()
        {
            await _client.ConnectToServer();
            _syncDataActive = true;
            Task.Run(() => SyncDataWithTarget());
        }

        public void StopSyncData()
        {
            _syncDataActive = false;
            _client.CloseAndDisconnect();
        }

        private string ID = "1";

        private void SyncDataWithTarget()
        {
            dataSet = new string[_syncObjectList.Count];
            uint syncMessageLength = 0;
            char[] syncMessage = null;

            while (_syncDataActive)
            {
                // If there is nothing to sync, do nothing
                if (_syncObjectList is null || _syncObjectList?.Count == 0)
                {
                    Task.Delay(100).Wait();
                    continue;
                }

                // Wait until the current List updates are done
                while (_syncObjectList.IsUpdating)
                {
                    if (!_syncDataActive)
                    {
                        return;
                    }
                    Task.Delay(10).Wait();
                }

                // Start new Cycle
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
                if (syncMessage is null || syncMessageLength != syncMessage?.Length)
                {
                    syncMessage = new char[syncMessageLength];
                }

                uint length = 0;
                for (int syncObjectNumber = 0; syncObjectNumber < _syncObjectList.Count; syncObjectNumber++)
                {
                    var syncObject = (SyncObject)_syncObjectList[syncObjectNumber];
                    Test(syncObjectNumber, syncObject, length);
                    length += syncObject._syncMessageLength;
                }
                //foreach (SyncObject syncObject in _syncObjectList)
                //{
                //    Test(syncObject, length);
                //    length += syncObject._syncMessageLength;
                //}

                //var t = new string(syncMessage);
                //var tmp2 = 0;

                Debug.WriteLine($"time to process data: {_swCycle.Elapsed}");


                // Send data
                if (_client.IsConnected)
                {
                    _client.SendToServer(syncMessage).Wait();
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


                unsafe void Test(int syncObjectNumber, SyncObject syncObject, uint startPoint)
                {
                    if (syncObject.ID == "102")
                    {

                    }

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
                                if (y < syncObject._syncPropertyValues.Length-1)
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



                //string dataToSend = string.Empty;

                //// Checken ob sich die Größe des dataSets geändert hat
                //if (dataSet.Length != _syncObjectList.Count)
                //{
                //    // Größe hat sich geändert dataset neu initialisieren
                //    dataSet = new string[_syncObjectList.Count];
                //}

                //// Build data
                //Parallel.For(0, _syncObjectList.Count, currentNumber =>
                //{
                //    // Check if object is indexed
                //    if (currentNumber < _syncObjectList.Count)
                //    {
                //        var thing = (SyncObject)_syncObjectList[currentNumber];
                //        if (thing?.ID == "0")
                //        {
                //            var index = Interlocked.Increment(ref _currentIdentifier);
                //            thing.InitObject(index, _propInfos);
                //        }

                //        // Get Property values  
                //        dataSet[currentNumber] = $"{thing.ID}{IDSEPERATOR}";
                //        foreach (var prop in _propInfos)
                //        {
                //            dataSet[currentNumber] = $"{dataSet[currentNumber]}{prop.GetValue(thing)},";
                //            //dataSet[currentNumber] += prop.GetValue(_syncObjectList[currentNumber]) + ",";
                //        }
                //        dataSet[currentNumber] = dataSet[currentNumber].Remove(dataSet[currentNumber].Length - 1, 1); /// OPT - hier wird ein neuer String erstellt
                //    }
                //});

                //var numberOfTasks = _syncObjectList.Count / 10;
                //var currentNumberOfTasks = numberOfTasks - 1;
                //var dataSetNumber = _syncObjectList.Count;
                //_syncObjectList.IsSourceUpdating = false;

                //// Daten komprimieren
                //while (true)
                //{

                //    // Alles Tasks bis auf den letzten ausführen
                //    Parallel.For(0, currentNumberOfTasks + 1, currentNumber => // Muss +1 sein?
                //    {
                //        var start = currentNumber * 10;
                //        var end = currentNumber * 10 + 10;

                //        for (int i = start + 1; i < end; i++)
                //        {
                //            dataSet[start] += SEPERATOR + dataSet[i];
                //        }
                //    });


                //    // Den letzten Task ausführen
                //    currentNumberOfTasks++;
                //    for (int i = (currentNumberOfTasks * 10) + 1; i < dataSetNumber; i++)
                //    {
                //        dataSet[currentNumberOfTasks * 10] += SEPERATOR + dataSet[i];
                //    }


                //    // Daten sortieren
                //    var sortRange = currentNumberOfTasks == 1 ? 1 : dataSet.Length % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                //    var multiplier = dataSet.Length > 10 ? 10 : 1;
                //    for (int i = 0; i < sortRange; i++)
                //    {
                //        dataSet[i] = dataSet[i * multiplier];
                //    }

                //    if (currentNumberOfTasks / 10 > 9 == false)
                //    {
                //        break;
                //    }
                //    dataSetNumber = currentNumberOfTasks + 1;
                //    currentNumberOfTasks = currentNumberOfTasks / 10 - 1;
                //}


                //var range = currentNumberOfTasks == 1 ? 1 : dataSet.Length % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                //for (int i = 0; i < range; i++)
                //{
                //    dataToSend += dataSet[i] + SEPERATOR;
                //}
                //dataToSend = dataToSend.Remove(dataToSend.Length - SEPERATOR.Length, SEPERATOR.Length);
                //Debug.WriteLine($"time to process data: {_swCycle.Elapsed}");


                //// Send data
                //if (_client.IsConnected)
                //{
                //    _client.SendToServer(dataToSend).Wait();
                //}

                //Debug.WriteLine($"time to process and send data: {_swCycle.Elapsed}");
                //while (_swCycle.ElapsedMilliseconds < _settings.UpdateCycleTimeMs)
                //{
                //    var waitMs = _settings.UpdateCycleTimeMs - (int)_swCycle.ElapsedMilliseconds;
                //    Task.Delay(waitMs).Wait();
                //}

                //CycleTime = _sw.Elapsed;
                //Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));
                //Debug.WriteLine($"Complete cycle time: {_sw.Elapsed}");
            }
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
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleConnectionStateChanged(bool newState)
        {
            IsConnectedToTarget = newState;
            Task.Run(() => ConncetionStateChanged?.Invoke(newState));
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