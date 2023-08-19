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
        private ServerClient? _client = null;

        private static Stopwatch _swCycle = Stopwatch.StartNew();
        private static PropertyInfo[] _propInfos;

        protected IList _syncObjectList;
        private Type _listItemType;

        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectIndex = new();
        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectToAddIndex = new();
        protected ConcurrentDictionary<ulong, SyncObject> _syncObjectToRemoveIndex = new();
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

            // Init server
            _server = new Server();

            // Add client to server
            var settings = new ServerClientSettings(address, port, true);
            _client = _server.AddClient(settings);
            _client.BufferUpdated += HandleBufferUpdated;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        protected virtual void UpdateListObjects()
        {
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleBufferUpdated(Memory<char> buffer)
        {
            // ToDo:
            //      - Use Splitter to slice all strings
            //      - Start Tasks to update syncObjects

            // Recieve and convert the data back
            var bufferWithOutEndIndices = SpanSplitter.GetSplitIndices(buffer.Span, DataSyncSource.C_MESSAGE_END_SEPERATOR);
            var bufferWithOutEnd = buffer.Slice(bufferWithOutEndIndices[0].StartIndex, bufferWithOutEndIndices[0].Length);
            
            var splitter = new SpanSplitter(bufferWithOutEnd.Span, DataSyncSource.C_OBJECT_SEPERATOR);
            var syncObjectDataPaket = splitter.GetNextSplit(bufferWithOutEnd.Span);

            while(syncObjectDataPaket != null)
            {
                var syncObjectDataPaketIndices = SpanSplitter.GetSplitIndices(syncObjectDataPaket, DataSyncSource.C_ID_SEPERATOR);
                var id = ulong.Parse(syncObjectDataPaket.Slice(syncObjectDataPaketIndices[0].StartIndex, syncObjectDataPaketIndices[0].Length));
                var values = syncObjectDataPaket.Slice(syncObjectDataPaketIndices[1].StartIndex, syncObjectDataPaketIndices[1].Length);
                var valuesIndices = SpanSplitter.GetSplitIndices(values, DataSyncSource.C_VALUE_SEPERATOR);
                SyncObject syncObject = null;

                syncObjectDataPaket = splitter.GetNextSplit(bufferWithOutEnd.Span);

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
                    var newValue = values.Slice(valuesIndices[propNumber].StartIndex, valuesIndices[propNumber].Length);
                    var currentValue = _propInfos[propNumber].GetValue(syncObject)?.ToString();
                    if (MemoryExtensions.Equals(newValue, currentValue, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (_propInfos[propNumber].PropertyType == typeof(string))
                    {
                        _propInfos[propNumber].SetValue(syncObject, new string(newValue));
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(sbyte))
                    {
                        var convertedValue = sbyte.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(byte))
                    {
                        var convertedValue = byte.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(short))
                    {
                        var convertedValue = short.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(ushort))
                    {
                        var convertedValue = ushort.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(int))
                    {
                        var convertedValue = int.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(uint))
                    {
                        var convertedValue = uint.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(long))
                    {
                        var convertedValue = long.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(ulong))
                    {
                        var convertedValue = ulong.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(double))
                    {
                        var convertedValue = double.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                    else if (_propInfos[propNumber].PropertyType == typeof(bool))
                    {
                        var convertedValue = bool.Parse(newValue);
                        _propInfos[propNumber].SetValue(syncObject, convertedValue);
                    }
                }
            }


            // Check if objects have been added or deleted in the source
            UpdateListObjects();


            // Signal other side, that the data have been handled
            _client.SendToClient("d");


            // End Cycle
            _swCycle.Stop();
            CycleTime = _swCycle.Elapsed;
            Task.Run(() => CycleTimeChanged?.Invoke(CycleTime));

            // Start new update cycle
            _swCycle.Restart();
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