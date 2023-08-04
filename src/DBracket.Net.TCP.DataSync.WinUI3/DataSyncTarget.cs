using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace DBracket.Net.TCP.DataSync.WinUI3
{
    public class DataSyncTarget : DataSync.DataSyncTarget
    {
        #region "----------------------------- Private Fields ------------------------------"
        private DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();



        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncTarget(IPAddress address, int port, IList syncObject) : base(address, port, syncObject)
        {
        }
        //public DataSyncTarget(IPAddress address, int port, IList syncObject)
        //{
        //    _target = new(address, port, syncObject);
        //}
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        protected override void UpdateListObjects()
        {
            var taskCompletionSource = new TaskCompletionSource();

            _ = _dispatcherQueue.TryEnqueue(new DispatcherQueueHandler(() =>
            {
                // Remove objects from list
                //ulong i = 1;
                //int y = 1;

                if (_syncObjectIndex?.Count > 0)
                {
                    Parallel.For((int)_syncObjectIndex.First().Key, (int)_syncObjectIndex.Last().Key, currentObjectNumber =>
                    {
                        try
                        {
                            var i = (ulong)currentObjectNumber;
                            if (_syncObjectToRemoveIndex.ContainsKey(i))
                                return;

                            var syncObject = _syncObjectIndex[i];
                            if (syncObject.NeedsToBeDeleted)
                            {
                                _syncObjectToRemoveIndex.TryAdd(i, syncObject);

                            }
                            else
                            {
                                syncObject.NeedsToBeDeleted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    });

                    foreach (var syncObject in _syncObjectToRemoveIndex)
                    {
                        var item = syncObject.Value;
                        var key = ulong.Parse(item.ID);
                        _syncObjectList.Remove(item);
                        _syncObjectIndex.Remove(key, out item);
                    }
                    _syncObjectToRemoveIndex.Clear();
                }


                // Add objects to list
                foreach (var syncObject in _syncObjectToAddIndex)
                {
                    _syncObjectList.Add(syncObject.Value);
                    _syncObjectIndex.TryAdd(syncObject.Key, syncObject.Value);
                }
                _syncObjectToAddIndex.Clear();

                taskCompletionSource.SetResult();
            }));

            taskCompletionSource.Task.Wait();
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"

        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}