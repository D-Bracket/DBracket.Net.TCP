using System.Net;

namespace DBracket.Net.TCP
{
    public class ServerClientSettings
    {
        #region "----------------------------- Private Fields ------------------------------"
        internal string _ipEndPoint;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public ServerClientSettings(IPAddress iPAddress, int port)
        {
            IPAddress = iPAddress;
            Port = port;
            _ipEndPoint = $"{iPAddress}:{port}";
        }

        public ServerClientSettings(IPAddress iPAddress, int port, bool useClientDataBuffer)
        {
            IPAddress = iPAddress;
            Port = port;
            _ipEndPoint = $"{iPAddress}:{port}";
            UseClientDataBuffer = useClientDataBuffer;
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
        public bool UseClientDataBuffer { get; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}