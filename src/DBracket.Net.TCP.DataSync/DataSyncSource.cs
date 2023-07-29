using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace DBracket.Net.TCP.DataSync
{
    public class DataSyncSource
    {
        #region "----------------------------- Private Fields ------------------------------"C
        private Client _client = new Client();

        private Stopwatch _sw = Stopwatch.StartNew();
        private Stopwatch _swCycle = Stopwatch.StartNew();
        private List<PropertyInfo> _propInfos = new List<PropertyInfo>();
        private DataSyncSourceSettings _settings;

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

        private bool _syncDataActive = false;
        string[] dataSet;
        string[] compressedData;

        public async void SyncDataWithTarget()
        {
            dataSet = new string[_syncObject.Count];

            while (_syncDataActive)
            {
                // Build data

                _sw.Restart();
                _swCycle.Restart();
                string dataToSend = string.Empty;
                Console.WriteLine($"Number of list items: {_syncObject.Count}");

                Parallel.For(0, _syncObject.Count, currentNumber =>
                {
                    dataSet[currentNumber] = "";
                    foreach (var prop in _propInfos)
                    {
                        dataSet[currentNumber] += prop.GetValue(_syncObject[currentNumber]) + ",";
                    }
                    dataSet[currentNumber] = dataSet[currentNumber].Remove(dataSet[currentNumber].Length - 1, 1);
                });


                int factor = 1000;
                if (compressedData is null)
                {
                    compressedData = new string[dataSet.Length / factor];
                }
                else if (compressedData.Length == 0)
                {
                    compressedData = new string[dataSet.Length / factor];
                }

                Parallel.For(0, (dataSet.Length / factor), currentNumber =>
                {
                    compressedData[currentNumber] = "";
                    // 0 * 1000 = 0, 0*1000+1000; 1*1000
                    for (int setNumber = currentNumber * factor; setNumber < (currentNumber * factor) + factor; setNumber++)
                    {
                        compressedData[currentNumber] += dataSet[setNumber] + ";";
                    }
                });

                foreach (var data in compressedData)
                {
                    dataToSend += data;
                }

                dataToSend = dataToSend.Remove(dataToSend.Length - 1, 1);



                // Send data
                if (_client.IsConnected)
                {
                    await _client.SendToServer(dataToSend);
                }

                while (_swCycle.ElapsedMilliseconds < _settings.UpdateCycleTimeMs)
                {
                    await Task.Delay(10);
                }

                CycleTime = _sw.Elapsed;
                Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));
                Debug.WriteLine($"Cycletime, Object read data: {_sw.Elapsed}");
            }

        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private async void Init(IPAddress address, int port)
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