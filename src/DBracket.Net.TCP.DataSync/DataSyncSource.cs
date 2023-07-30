using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;

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
                    Parallel.For(0, currentNumberOfTasks+1, currentNumber => // Muss +1 sein?
                    {
                        var start = currentNumber * 10;
                        var end = currentNumber * 10 + 10;

                        for (int i = start + 1; i < end; i++)
                        {
                            dataSet[start] += ";" + dataSet[i];
                        }
                    });

                    // Den letzten Task ausführen
                    currentNumberOfTasks++;
                    for (int i = (currentNumberOfTasks * 10) +1; i < dataSetNumber; i++)
                    {
                        dataSet[currentNumberOfTasks * 10] += ";" + dataSet[i];
                    }

                    // Daten sortieren
                    var sortRange = currentNumberOfTasks % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;
                    for (int i = 0; i < sortRange; i++) //+1
                    {
                        dataSet[i] = dataSet[i * 10];
                    }

                    if (currentNumberOfTasks / 10 > 9 == false)
                    {
                        break;
                    }
                    dataSetNumber = currentNumberOfTasks + 1;//+1
                    currentNumberOfTasks = currentNumberOfTasks / 10 - 1;
                }

                    var range = currentNumberOfTasks % 10 == 0 ? currentNumberOfTasks : currentNumberOfTasks + 1;

                for (int i = 0; i < range; i++) //   + 1
                {
                    dataToSend += dataSet[i] + ";";
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






        public async void SyncDataWithTarget3()
        {
            dataSet = new string[_syncObject.Count];

            while (_syncDataActive)
            {
                // Build data

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

                Parallel.For(0, _syncObject.Count, currentNumber =>
                {
                    dataSet[currentNumber] = "";
                    foreach (var prop in _propInfos)
                    {
                        dataSet[currentNumber] += prop.GetValue(_syncObject[currentNumber]) + ",";
                    }
                    dataSet[currentNumber] = dataSet[currentNumber].Remove(dataSet[currentNumber].Length - 1, 1);
                });


                var power = GetPowerOfNumber(_syncObject.Count);
                int taskNumber = 0;
                int initialFactor;
                var factor = 0;

                if (_syncObject.Count % power == 0)
                {
                    initialFactor = _syncObject.Count / 10;
                    factor = _syncObject.Count / 10;
                }
                else
                {
                    initialFactor = (_syncObject.Count + (power - _syncObject.Count % power)) / 10;
                    factor = (_syncObject.Count + (10 - _syncObject.Count % 10)) / 10;
                    // immer auf die nächste 10er Stelle runden
                }
                taskNumber = initialFactor;

                var iterator = 1;
                // data compresion loops
                while (taskNumber >= 10)
                {
                    var taskRange = (taskNumber * 10) / taskNumber;

                    //var taskNumber = dataSet.Length / factor;
                    Parallel.For(0, taskNumber, currentNumber =>
                    {
                        var start = currentNumber * taskRange;
                        var end = (currentNumber * taskRange) + taskRange;

                        if (start > _syncObject.Count)
                        {

                        }
                        else if (end > _syncObject.Count / iterator)
                        {
                            // 0 * 1000 = 0, 0*1000+1000; 1*1000
                            var tmp = _syncObject.Count / iterator;
                            if (_syncObject.Count % iterator != 0)
                            {
                                tmp = _syncObject.Count / iterator + 1;
                            }
                            for (int setNumber = start + 1; setNumber < tmp; setNumber++)
                            {
                                dataSet[start] += ";" + dataSet[setNumber];
                            }
                        }
                        else
                        {
                            // 0 * 1000 = 0, 0*1000+1000; 1*1000
                            for (int setNumber = start + 1; setNumber < end; setNumber++)
                            {
                                dataSet[start] += ";" + dataSet[setNumber];
                            }
                        }

                        //for (int setNumber = start + 1; setNumber < end; setNumber++)
                        //{
                        //    dataSet[start] += ";" + dataSet[setNumber];
                        //}
                    });

                    iterator = iterator * 10;
                    for (int i = 0; i < factor; i++)
                    {
                        dataSet[i] = dataSet[i * taskRange];
                    }
                    taskNumber = taskNumber / 10;

                    if (taskNumber >= 10)
                    {
                        if (factor % 10 == 0)
                        {
                            factor = factor / 10;
                        }
                        else
                        {
                            factor = (factor + (10 - factor % 10)) / 10;
                        }
                    }
                }

                if (initialFactor < 10)
                {
                    foreach (var data in dataSet)
                    {
                        dataToSend += data + ";";
                    }
                }
                else
                {
                    var range = 0;
                    if (taskNumber != 10)
                    {
                        range = factor - 10;
                    }
                    for (int i = 0; i < 10 + range; i++)
                    {
                        dataToSend += dataSet[i] + ";";
                    }
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

        private static int GetPowerOfNumber(int number)
        {
            int power = 1;
            int rest = 0;

            while (true)
            {
                rest = number % power;

                if (rest == number)
                {
                    power = power / 10;
                    return power;
                }
                power = power * 10;
            }
        }

        public async void SyncDataWithTarget2()
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