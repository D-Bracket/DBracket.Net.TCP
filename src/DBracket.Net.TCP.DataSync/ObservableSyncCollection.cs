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
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}