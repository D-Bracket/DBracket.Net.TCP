using System.Collections;
using System.Collections.Concurrent;
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
        private static PropertyInfo[] _propInfos;

        protected IList _syncObjectList;
        private Type _listItemType;

        private bool _dataReadActive;
        private bool _dataWriteActive;
        private string _newData;

        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectIndex = new();
        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectToAddIndex = new();
        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectToRemoveIndex = new();

        //protected IDictionary<ulong, SyncObject> _syncObjectIndexLoop;
        //protected IDictionary<ulong, SyncObject> _syncObjectsToRemove;
        private bool _isFirstListActive;

        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncTarget(IPAddress address, int port, IList syncObject)
        {
            if (syncObject == null)
                throw new ArgumentNullException();

            _syncObjectList = syncObject;
            _syncObjectList.Clear();

            // Get object infos
            _listItemType = _syncObjectList.GetType().GenericTypeArguments[0];
            _propInfos = _listItemType.GetProperties().Where(x => x.GetCustomAttribute<SyncPropertyAttribute>() is not null).ToArray();

            // Init Server
            _server = new Server();
            _server.NewMessageRecieved += HandleServerMessageRecieved;
            _server.StartListeningForIncomingConnection("Data Sync", address, port);

            Task.Run(() => HandleDataInput());
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void HandleDataInput()
        {
            int loop = 0;
            while (true)
            {
                // Start new update cycle
                _swCycle.Restart();
                loop++;

                // Wait until new data have been recieved
                while (string.IsNullOrEmpty(_newData)) { Task.Delay(10).Wait(); }
                // Wait until the new data is in buffer
                while (_dataWriteActive) { Task.Delay(10).Wait(); }

                // Buffer the new data
                _dataReadActive = true;
                string newData = _newData;
                _newData = string.Empty;
                _dataReadActive = false;


                // Recieve and convert the data back
                var objectPropValues = newData.Split(DataSyncSource.OBJECT_SEPERATOR);

                //for (int currentObjectNumber = 0; currentObjectNumber < objectPropValues.Length; currentObjectNumber++)
                //{
                //    try
                //    {
                //        // Get object parameters
                //        var syncObjects = objectPropValues[currentObjectNumber].Split(DataSyncSource.ID_SEPERATOR);
                //        var id = ulong.Parse(syncObjects[0]);
                //        var values = syncObjects[1].Split(DataSyncSource.VALUE_SEPERATOR);
                //        SyncObject syncObject = null;


                //        // Check if object exists
                //        if (!_syncObjectIndex.ContainsKey(id))
                //        {
                //            // Object doesn't exist and needs to be created
                //            syncObject = (SyncObject)Activator.CreateInstance(_listItemType);
                //            syncObject.InitObject(id, _propInfos);
                //            _syncObjectToAddIndex.TryAdd(id, syncObject);
                //        }
                //        else
                //        {
                //            // Object exitsts and needs to be updated
                //            syncObject = _syncObjectIndex[id];
                //            syncObject.NeedsToBeDeleted = false;
                //        }


                //        //// Update values
                //        //for (int propNumber = 0; propNumber < _propInfos.Length; propNumber++)
                //        //{
                //        //    var currentValue = _propInfos[propNumber].GetValue(syncObject)?.ToString();
                //        //    if (currentValue == values[propNumber])
                //        //    {
                //        //        continue;
                //        //    }

                //        //    if (_propInfos[propNumber].PropertyType == typeof(string))
                //        //    {
                //        //        _propInfos[propNumber].SetValue(syncObject, values[propNumber]);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(sbyte))
                //        //    {
                //        //        var convertedValue = Convert.ToSByte(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(byte))
                //        //    {
                //        //        var convertedValue = Convert.ToByte(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(short))
                //        //    {
                //        //        var convertedValue = Convert.ToInt16(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(ushort))
                //        //    {
                //        //        var convertedValue = Convert.ToUInt16(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(int))
                //        //    {
                //        //        var convertedValue = Convert.ToInt32(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(uint))
                //        //    {
                //        //        var convertedValue = Convert.ToUInt32(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(long))
                //        //    {
                //        //        var convertedValue = Convert.ToInt64(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(ulong))
                //        //    {
                //        //        var convertedValue = Convert.ToUInt64(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(double))
                //        //    {
                //        //        var convertedValue = Convert.ToDouble(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //    else if (_propInfos[propNumber].PropertyType == typeof(bool))
                //        //    {
                //        //        var convertedValue = Convert.ToBoolean(values[propNumber]);
                //        //        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                //        //    }
                //        //}
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine(ex.Message);
                //    }
                //}
















                Parallel.For(0, objectPropValues.Length, currentObjectNumber =>
                {
                    try
                    {
                        // Get object parameters
                        var syncObjects = objectPropValues[currentObjectNumber].Split(DataSyncSource.ID_SEPERATOR);
                        var id = ulong.Parse(syncObjects[0]);
                        var values = syncObjects[1].Split(DataSyncSource.VALUE_SEPERATOR);
                        SyncObject syncObject = null;


                        // Check if object exists
                        if (!_syncObjectIndex.ContainsKey(id))
                        {
                            // Object doesn't exist and needs to be created
                            syncObject = (SyncObject)Activator.CreateInstance(_listItemType);
                            syncObject.InitObject(id, _propInfos);
                            _syncObjectToAddIndex.TryAdd(id, syncObject);
                        }
                        else
                        {
                            // Object exitsts and needs to be updated
                            syncObject = _syncObjectIndex[id];
                            syncObject.NeedsToBeDeleted = false;
                        }


                        // Update values
                        for (int propNumber = 0; propNumber < _propInfos.Length; propNumber++)
                        {
                            var currentValue = _propInfos[propNumber].GetValue(syncObject)?.ToString();
                            if (currentValue == values[propNumber])
                            {
                                continue;
                            }

                            if (_propInfos[propNumber].PropertyType == typeof(string))
                            {
                                _propInfos[propNumber].SetValue(syncObject, values[propNumber]);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(sbyte))
                            {
                                var convertedValue = Convert.ToSByte(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(byte))
                            {
                                var convertedValue = Convert.ToByte(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(short))
                            {
                                var convertedValue = Convert.ToInt16(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(ushort))
                            {
                                var convertedValue = Convert.ToUInt16(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(int))
                            {
                                var convertedValue = Convert.ToInt32(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(uint))
                            {
                                var convertedValue = Convert.ToUInt32(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(long))
                            {
                                var convertedValue = Convert.ToInt64(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(ulong))
                            {
                                var convertedValue = Convert.ToUInt64(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(double))
                            {
                                var convertedValue = Convert.ToDouble(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                            else if (_propInfos[propNumber].PropertyType == typeof(bool))
                            {
                                var convertedValue = Convert.ToBoolean(values[propNumber]);
                                _propInfos[propNumber].SetValue(syncObject, convertedValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });


                // Check if objects have been added or deleted in the source
                UpdateListObjects();


                // End Cycle
                _swCycle.Stop();
                CycleTime = _swCycle.Elapsed;
                Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));
            }
        }

        protected virtual void UpdateListObjects()
        {
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleServerMessageRecieved(string clientName, string message)
        {
            _dataWriteActive = false;
            //Debug.WriteLine($"Data recieved");
            while (_dataReadActive) { Task.Delay(10).Wait(); }

            _dataWriteActive = true;
            //_newData = message;
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