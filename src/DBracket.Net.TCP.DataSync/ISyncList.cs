using System.Collections;

namespace DBracket.Net.TCP.DataSync
{
    public interface ISyncList : IList
    {
        #region "--------------------------------- Methods ---------------------------------"
        public string GetID(int index);
        #endregion


        #region "--------------------------- Public Propterties ----------------------------"
        //internal DataSyncSource? SyncSource { get; set; }

        public bool IsUpdating { get; }
        internal bool IsSourceUpdating { get; set; }
        #endregion


        #region "--------------------------------- Events ----------------------------------"

        #endregion
    }
}