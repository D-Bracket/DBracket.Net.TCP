using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DBracket.Net.TCP.DataSync
{
    public class DataSyncSource
    {
        #region "----------------------------- Private Fields ------------------------------"C
        internal static string SEPERATOR = $"|;";
        internal static string IDSEPERATOR = $";_";

        private ulong _currentIdentifier = 1;

        private Client _client = new Client();

        private Stopwatch _sw = Stopwatch.StartNew();
        private Stopwatch _swCycle = Stopwatch.StartNew();
        private List<PropertyInfo> _propInfos = new List<PropertyInfo>();
        private DataSyncSourceSettings _settings;

        private bool _syncDataActive = false;
        string[] dataSet;

        private ISyncList _syncObjectList;
        private Type _syncObjectType;

        //internal bool _isReadingValues;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncSource(DataSyncSourceSettings settings, ISyncList syncObject)
        {
            if (settings == null)
                throw new ArgumentNullException();
            if (syncObject == null)
                throw new ArgumentNullException();

            _syncObjectList = syncObject;
            //_syncObjectList.SyncSource = this;
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
            //int loop = 0;

            while (_syncDataActive)
            {
                if (_syncObjectList is null || _syncObjectList?.Count == 0)
                {
                    Task.Delay(100).Wait();
                    continue;
                }

                while (_syncObjectList.IsUpdating)
                {
                    if (!_syncDataActive)
                    {
                        return;
                    }
                    Task.Delay(10).Wait();
                }

                _syncObjectList.IsSourceUpdating = true;
                //loop++;
                _sw.Restart();
                _swCycle.Restart();
                //Debug.WriteLine($"loop: {loop}");
                string dataToSend = string.Empty;

                // Checken ob sich die Größe des dataSets geändert hat
                if (dataSet.Length != _syncObjectList.Count)
                {
                    // Größe hat sich geändert dataset neu initialisieren
                    dataSet = new string[_syncObjectList.Count];
                }

                // Build data
                Parallel.For(0, _syncObjectList.Count, currentNumber =>
                {
                    // Check if object is indexed
                    if (currentNumber < _syncObjectList.Count)
                    {
                        var thing = (SyncObject)_syncObjectList[currentNumber];
                        if (thing?.ID == "0")
                        {
                            var index = Interlocked.Increment(ref _currentIdentifier);
                            thing.SetIdentifier(index);
                        }

                        // Get Property values  
                        dataSet[currentNumber] = $"{thing.ID}{IDSEPERATOR}";
                        foreach (var prop in _propInfos)
                        {
                            dataSet[currentNumber] = $"{dataSet[currentNumber]}{prop.GetValue(thing)},";
                            //dataSet[currentNumber] += prop.GetValue(_syncObjectList[currentNumber]) + ",";
                        }
                        dataSet[currentNumber] = dataSet[currentNumber].Remove(dataSet[currentNumber].Length - 1, 1); /// OPT - hier wird ein neuer String erstellt
                    }
                });

                var numberOfTasks = _syncObjectList.Count / 10;
                var currentNumberOfTasks = numberOfTasks - 1;
                var dataSetNumber = _syncObjectList.Count;
                _syncObjectList.IsSourceUpdating = false;

                // Daten komprimieren
                while (true)
                {

                    // Alles Tasks bis auf den letzten ausführen
                    Parallel.For(0, currentNumberOfTasks + 1, currentNumber => // Muss +1 sein?
                    {
                        var start = currentNumber * 10;
                        var end = currentNumber * 10 + 10;

                        for (int i = start + 1; i < end; i++)
                        {
                            dataSet[start] += SEPERATOR + dataSet[i];
                        }
                    });


                    // Den letzten Task ausführen
                    currentNumberOfTasks++;
                    for (int i = (currentNumberOfTasks * 10) + 1; i < dataSetNumber; i++)
                    {
                        dataSet[currentNumberOfTasks * 10] += SEPERATOR + dataSet[i];
                    }


                    // Daten sortieren
                    var sortRange = currentNumberOfTasks == 1 ? 1 : dataSet.Length % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                    var multiplier = dataSet.Length > 10 ? 10 : 1;
                    for (int i = 0; i < sortRange; i++)
                    {
                        dataSet[i] = dataSet[i * multiplier];
                    }

                    if (currentNumberOfTasks / 10 > 9 == false)
                    {
                        break;
                    }
                    dataSetNumber = currentNumberOfTasks + 1;
                    currentNumberOfTasks = currentNumberOfTasks / 10 - 1;
                }


                var range = currentNumberOfTasks == 1 ? 1 : dataSet.Length % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                for (int i = 0; i < range; i++)
                {
                    dataToSend += dataSet[i] + SEPERATOR;
                }
                dataToSend = dataToSend.Remove(dataToSend.Length - SEPERATOR.Length, SEPERATOR.Length);
                Debug.WriteLine($"time to process data: {_swCycle.Elapsed}");


                // Send data
                if (_client.IsConnected)
                {
                    _client.SendToServer(dataToSend).Wait();
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
            }
        }

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void Init(IPAddress address, int port)
        {
            // Get object infos
            _syncObjectType = _syncObjectList.GetType().GenericTypeArguments[0];

            var props = _syncObjectType.GetProperties();
            foreach (var prop in props)
            {
                var attribute = prop.GetCustomAttribute<SyncPropertyAttribute>();
                if (attribute is not null)
                {
                    _propInfos.Add(prop);
                    continue;
                }
            }

            // Initialize id's of the objects
            foreach (SyncObject syncObject in _syncObjectList)
            {
                syncObject.SetIdentifier(_currentIdentifier++);
            }

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