using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DBracket.Net.TCP.DataSync
{
    public sealed class DataSyncSourceSettings
    {
        #region "----------------------------- Private Fields ------------------------------"

        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public DataSyncSourceSettings(IPAddress ipAddress, int port, bool alwaysUpdate, int updateCycleTimeMs)
        {
            IPAddress = ipAddress;
            Port = port;
            AlwaysUpdate = alwaysUpdate;
            UpdateCycleTimeMs = updateCycleTimeMs;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion


        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public IPAddress IPAddress { get; }
        public int Port { get; }
        public bool AlwaysUpdate { get; }
        public int UpdateCycleTimeMs { get; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}