using System.Collections.ObjectModel;

namespace DBracket.Net.TCP.DataSync
{
    public class ObservableSyncCollection<T> : ObservableCollection<T>, ISyncList
    {
        #region "----------------------------- Private Fields ------------------------------"

        #endregion



        #region "------------------------------ Constructor --------------------------------"

        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public string GetID(int index)
        {
            return "1";
            var t = Items.ElementAt(index) as SyncObject;
            return t.ID;
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        protected override void RemoveItem(int index)
        {
            IsUpdating = true;
            while (_isSourceUpdating) { Task.Delay(10).Wait(); }
            base.RemoveItem(index);
            IsUpdating = false;
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion


        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public bool IsUpdating { get; private set; }

        bool ISyncList.IsSourceUpdating { get => _isSourceUpdating; set { _isSourceUpdating = value; } }
        private bool _isSourceUpdating;

        //bool ISyncList.IsUpdating => throw new NotImplementedException();

        //bool ISyncList.IsSourceUpdating { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //DataSyncSource? ISyncList.SyncSource { get => _syncSource; set { _syncSource = value; } }
        //internal DataSyncSource? _syncSource;
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}