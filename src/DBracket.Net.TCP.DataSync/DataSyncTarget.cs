using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace DBracket.Net.TCP.DataSync
{
    public class DataSyncTarget
    {
        #region "----------------------------- Private Fields ------------------------------"
        private static Server _server = new Server();

        private static Stopwatch _sw = Stopwatch.StartNew();
        private static Stopwatch _swCycle = Stopwatch.StartNew();
        private static List<PropertyInfo> _propInfos = new List<PropertyInfo>();

        private IList _syncObject;
        private Type _listItemType;

        private bool _dataReadActive;
        private bool _dataWriteActive;
        private string _newData;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncTarget(IPAddress address, int port, IList syncObject)
        {
            if (syncObject == null)
                throw new ArgumentNullException();

            _syncObject = syncObject;

            // Get object infos
            _listItemType = _syncObject.GetType().GenericTypeArguments[0];
            var props = _listItemType.GetProperties();
            foreach (var prop in props)
            {
                var attribute = prop.GetCustomAttribute<ExchangeAttribute>();
                if (attribute is not null)
                {
                    _propInfos.Add(prop);
                    continue;
                }
            }
            Test();


            // Init Server
            _server = new Server();
            _server.NewMessageRecieved += HandleServerMessageRecieved;
            _server.StartListeningForIncomingConnection("Data Sync", address, port);

            Task.Run(() => HandleDataInput());
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        private void Test()
        {
            var t = Activator.CreateInstance(_listItemType);
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void HandleDataInput()
        {
            while (true)
            {
                _swCycle.Restart();
                while (_dataWriteActive) { Task.Delay(10).Wait(); }
                while (string.IsNullOrEmpty(_newData)) { Task.Delay(10).Wait(); }

                _dataReadActive = true;
                string newData = _newData;
                _newData = string.Empty;
                _dataReadActive = false;




                // Recieve and convert the data back
                //_sw.Restart();
                //long microseconds = 0;
                var objectPropValues = newData.Split(DataSyncSource.SEPERATOR);

                Parallel.For(0, objectPropValues.Length, currentObjectNumber =>
                {
                    try
                    {

                        var values = objectPropValues[currentObjectNumber].Split(",");

                        for (int propNumber = 0; propNumber < _propInfos.Count; propNumber++)
                        {
                            var currentValue = _propInfos[propNumber].GetValue(_syncObject[currentObjectNumber]).ToString();
                            if (currentValue == values[propNumber])
                            {
                                continue;
                            }

                            if (_propInfos[propNumber].PropertyType == typeof(string))
                            {
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], values[propNumber]);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(sbyte))
                            {
                                var convertedValue = Convert.ToSByte(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(byte))
                            {
                                var convertedValue = Convert.ToByte(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(short))
                            {
                                var convertedValue = Convert.ToInt16(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(ushort))
                            {
                                var convertedValue = Convert.ToUInt16(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(int))
                            {
                                var convertedValue = Convert.ToInt32(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(uint))
                            {
                                var convertedValue = Convert.ToUInt32(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(long))
                            {
                                var convertedValue = Convert.ToInt64(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(ulong))
                            {
                                var convertedValue = Convert.ToUInt64(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(double))
                            {
                                var convertedValue = Convert.ToDouble(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(bool))
                            {
                                var convertedValue = Convert.ToBoolean(values[propNumber]);
                                _propInfos[propNumber].SetValue(_syncObject[currentObjectNumber], convertedValue);
                            }


                        }
                        //Console.WriteLine($"Doing things {currentObjectNumber}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
                _swCycle.Stop();

                CycleTime = _swCycle.Elapsed;
                Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));

                //microseconds = _sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
                //Console.WriteLine($"Cycletime, converted message into objects: {microseconds}us");
                //Console.WriteLine($"Complete Cycletime: {_swCycle.Elapsed}");

                //Console.WriteLine($"====================================================");

                //Task.Delay(10).Wait();
            }
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleServerMessageRecieved(string clientName, string message)
        {
            _dataWriteActive = false;
            Debug.WriteLine($"Data recieved");
            while (_dataReadActive) ;

            _dataWriteActive = true;
            _newData = message;
            _dataWriteActive = false;
        }
        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public TimeSpan CycleTime { get; private set; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event CycleTimeChangedHandler CycleTimeChanged;
        public delegate void CycleTimeChangedHandler(TimeSpan time);
        #endregion
        #endregion
    }
}