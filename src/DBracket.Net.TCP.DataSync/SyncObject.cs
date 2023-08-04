namespace DBracket.Net.TCP.DataSync
{
    public abstract class SyncObject
    {
        #region "----------------------------- Private Fields ------------------------------"
        #endregion



        #region "------------------------------ Constructor --------------------------------"

        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        internal void SetIdentifier(ulong id)
        {
            ID = id.ToString();
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public string ID { get; private set; } 
        public bool NeedsToBeDeleted { get; set; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}