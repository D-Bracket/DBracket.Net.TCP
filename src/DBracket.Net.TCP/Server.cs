using System.Net.Sockets;
using System.Diagnostics;

namespace DBracket.Net.TCP
{
    public class Server
    {
        #region "----------------------------- Private Fields ------------------------------"
        private readonly Dictionary<string, ServerClient> _serverClients = new();
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public Server()
        {
            _clients = new Dictionary<string, TcpClient>();
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public ServerClient AddClient(ServerClientSettings settings)
        {
            if (settings is null)
                throw new ArgumentNullException("Settings can't be null");

            // Check if EndPoint is already in use
            var ipEndPoint = $"{settings.IPAddress}:{settings.Port}";
            if (_serverClients.ContainsKey(ipEndPoint))
                throw new ArgumentException("IPEndPoint already added to server");

            // Add Client
            var serverClient = new ServerClient(settings);
            _serverClients.Add(ipEndPoint, serverClient);

            return serverClient;
        }

        public ServerClient GetClient(string ipAddress, int port)
        {
            var ipEndPoint = $"{ipAddress}:{port}";
            return _serverClients[ipEndPoint];
        }

        public void StopServer()
        {
            try
            {
                foreach (var c in _clients)
                {
                    c.Value.Close();
                }

                _clients.Clear();
            }
            catch (Exception excp)
            {
                Debug.WriteLine(excp.ToString());
            }
        }

        public async void SendToClient(string clientName, string leMessage)
        {
            if (string.IsNullOrEmpty(leMessage))
            {
                return;
            }

            var client = _clients[clientName];

            if (client is null)
            {
                throw new Exception("Client not connected");
            }

            Debug.WriteLine("Sending to client");
            StreamWriter clientStreamWriter = new StreamWriter(client.GetStream());
            clientStreamWriter.AutoFlush = true;

            await clientStreamWriter.WriteLineAsync(leMessage);
        }

        public async void SendToAll(string leMessage)
        {
            if (string.IsNullOrEmpty(leMessage))
            {
                return;
            }

            try
            {
                foreach (var c in _clients)
                {
                    StreamWriter clientStreamWriter = new StreamWriter(c.Value.GetStream());
                    await clientStreamWriter.WriteLineAsync(leMessage);
                }

                //byte[] buffMessage = Encoding.ASCII.GetBytes(leMessage);

                //foreach (var c in _clients)
                //{
                //    await c.Value.GetStream().WriteAsync(buffMessage, 0, buffMessage.Length);
                //}
            }
            catch (Exception excp)
            {
                Debug.WriteLine(excp.ToString());
            }

        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void RemoveClient(string clientName)
        {
            if (_clients.ContainsKey(clientName))
            {
                _clients.Remove(clientName);
                Debug.WriteLine(String.Format("Client removed, count: {0}", _clients.Count));
            }
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public Dictionary<string, TcpClient> Clients { get => _clients; }
        private readonly Dictionary<string, TcpClient> _clients;

        public bool KeepRunning { get; set; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event ClientConnectionChangedHandler? ClientConnectionChanged;
        public delegate void ClientConnectionChangedHandler(string clientName, bool newState);

        public event HandleMessageRecieved? NewMessageRecieved;
        public delegate void HandleMessageRecieved(string clientName, string message);
        #endregion
        #endregion
    }
}