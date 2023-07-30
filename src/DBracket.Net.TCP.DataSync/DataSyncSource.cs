using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace DBracket.Net.TCP.DataSync
{
    public class DataSyncSource
    {
        #region "----------------------------- Private Fields ------------------------------"C
        internal static string SEPERATOR = $"|;";

        private Client _client = new Client();

        private Stopwatch _sw = Stopwatch.StartNew();
        private Stopwatch _swCycle = Stopwatch.StartNew();
        private List<PropertyInfo> _propInfos = new List<PropertyInfo>();
        private DataSyncSourceSettings _settings;

        private bool _syncDataActive = false;
        string[] dataSet;

        private IList _syncObject;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncSource(DataSyncSourceSettings settings, IList syncObject)
        {
            _settings = settings;
            if (syncObject == null)
                throw new ArgumentNullException();

            _syncObject = syncObject;

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

        public void SyncDataWithTarget()
        {
            dataSet = new string[_syncObject.Count];

            while (_syncDataActive)
            {
                if (_syncObject is null || _syncObject?.Count == 0)
                {
                    Task.Delay(100).Wait();
                    continue;
                }

                _sw.Restart();
                _swCycle.Restart();
                string dataToSend = string.Empty;
                Console.WriteLine($"Number of list items: {_syncObject.Count}");

                // Checken ob sich die Größe des dataSets geändert hat
                if (dataSet.Length != _syncObject.Count)
                {
                    // Größe hat sich geändert dataset neu initialisieren
                    dataSet = new string[_syncObject.Count];
                }

                // Build data
                Parallel.For(0, _syncObject.Count, currentNumber =>
                {
                    dataSet[currentNumber] = "";
                    foreach (var prop in _propInfos)
                    {
                        dataSet[currentNumber] += prop.GetValue(_syncObject[currentNumber]) + ",";
                    }
                    dataSet[currentNumber] = dataSet[currentNumber].Remove(dataSet[currentNumber].Length - 1, 1);
                });

                var numberOfTasks = _syncObject.Count / 10;
                var currentNumberOfTasks = numberOfTasks - 1;
                var dataSetNumber = _syncObject.Count;

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
                    var sortRange = currentNumberOfTasks % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                    for (int i = 0; i < sortRange; i++)
                    {
                        dataSet[i] = dataSet[i * 10];
                    }

                    if (currentNumberOfTasks / 10 > 9 == false)
                    {
                        break;
                    }
                    dataSetNumber = currentNumberOfTasks + 1;
                    currentNumberOfTasks = currentNumberOfTasks / 10 - 1;
                }

                var range = currentNumberOfTasks % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                for (int i = 0; i < range; i++) //   + 1
                {
                    dataToSend += dataSet[i] + SEPERATOR;
                }
                dataToSend = dataToSend.Remove(dataToSend.Length - SEPERATOR.Length, SEPERATOR.Length);


                // Send data
                if (_client.IsConnected)
                {
                    _client.SendToServer(dataToSend).Wait();
                }

                while (_swCycle.ElapsedMilliseconds < _settings.UpdateCycleTimeMs)
                {
                    Task.Delay(10).Wait();
                }

                CycleTime = _sw.Elapsed;
                Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));
                Debug.WriteLine($"Cycletime, Object read data: {_sw.Elapsed}");
            }
        }

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void Init(IPAddress address, int port)
        {
            // Get object infos
            var props = _syncObject[0].GetType().GetProperties();
            foreach (var prop in props)
            {
                var attribute = prop.GetCustomAttribute<ExchangeAttribute>();
                if (attribute is not null)
                {
                    _propInfos.Add(prop);
                    continue;
                }
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