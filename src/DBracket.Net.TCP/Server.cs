using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Net.Http;

namespace DBracket.Net.TCP
{
    public class Server
    {
        #region "----------------------------- Private Fields ------------------------------"
        //private IPAddress? _ipAddress;
        //private int _port;

        private TcpListener? _listener;
        private readonly Dictionary<string, TcpClient> _clients;
        //private readonly Dictionary<string, IPEndPoint> _allowedClientList = new Dictionary<string, IPEndPoint>();
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public Server()
        {
            _clients = new Dictionary<string, TcpClient>();
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public async void StartListeningForIncomingConnection(string clientName, IPAddress ipAddress, int port)
        {
            if (ipAddress is null)
            {
                throw new ArgumentException("No IP Address selected. To accept any IP parse in the argument IPAddress.Any");
            }

            if (port <= 0)
            {
                return;
            }

            //_ipAddress = ipAddress;
            //_port = port;

            Debug.WriteLine(string.Format("IP Address: {0} - Port: {1}", ipAddress.ToString(), port));

            _listener = new TcpListener(ipAddress, port);

            try
            {
                _listener.Start();
                KeepRunning = true;

                while (KeepRunning)
                {
                    Debug.WriteLine("Start Listening again");
                    var returnedByAccept = await _listener.AcceptTcpClientAsync();

                    if (_clients.ContainsKey(clientName))
                    {
                        _clients[clientName] = returnedByAccept;
                    }
                    else
                    {
                        _clients.Add(clientName, returnedByAccept);
                    }

#pragma warning disable CS4014
                    Task.Run(() => CheckConnectionState(clientName, returnedByAccept));
#pragma warning restore CS4014
                    TakeCareOfTCPClient(clientName, returnedByAccept);
                    ClientConnectionChanged?.Invoke(clientName, true);

                    Debug.WriteLine(
                        string.Format("Client connected successfully, count: {0} - {1}",
                        _clients.Count, returnedByAccept.Client.RemoteEndPoint));
                }
            }
            catch (Exception excp)
            {
                System.Diagnostics.Debug.WriteLine(excp.ToString());
            }
        }

        public void StopServer()
        {
            try
            {
                if (_listener is not null)
                {
                    _listener.Stop();
                }

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

            byte[] buffMessage = Encoding.ASCII.GetBytes(leMessage);
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
        private async void TakeCareOfTCPClient(string clientName, TcpClient paramClient)
        {
            try
            {
                NetworkStream stream = paramClient.GetStream();
                StreamReader reader = new StreamReader(stream);

                while (KeepRunning)
                {
                    Debug.WriteLine("*** Ready to read");

                    string? receivedText = await reader.ReadLineAsync();
                    if (receivedText is null)
                    {
                        receivedText = string.Empty;
                    }

                    Debug.WriteLine($"Client connection: {paramClient.Connected}");
                    // Check connection to client
                    bool connected = true;

                    if (paramClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (paramClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            connected = false;
                        }
                    }

                    //bool connected =
                    //    paramClient.Client.Poll(01,SelectMode.SelectWrite) &&
                    //    paramClient.Client.Poll(01, SelectMode.SelectRead) && !paramClient.Client.Poll(01, SelectMode.SelectError) ? true : false;
                    Debug.WriteLine($"Determined client connection: {connected}");

                    if (!connected)
                    {
                        paramClient.Close();
                        //if (KeepRunning)
                        //{
                        //    StartListeningForIncomingConnection(clientName, )
                        //}
                        break;
                    }
                    //Debug.WriteLine("*** RECEIVED: " + receivedText);
                    NewMessageRecieved?.Invoke(clientName, receivedText);
                }
            }
            catch (Exception excp)
            {
                RemoveClient(clientName);
                System.Diagnostics.Debug.WriteLine(excp.ToString());
            }

        }

        private void RemoveClient(string clientName)
        {
            if (_clients.ContainsKey(clientName))
            {
                _clients.Remove(clientName);
                Debug.WriteLine(String.Format("Client removed, count: {0}", _clients.Count));
            }
        }

        private void CheckConnectionState(string clientName, TcpClient client)
        {
            while (true)
            {
                if (client is null)
                {
                    break;
                }

                try
                {
                    if (!client.Connected)
                    {
                        // Client disconnected
                        Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
                        break;
                    }
                    else if (client.Client is null)
                    {
                        // Client disconnected
                        Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
                        break;
                    }

                    if (client.Client is not null && client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (client.Client?.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
                            break;
                        }
                    }
                }
                catch 
                {
                }

                Task.Delay(10).Wait();
            }
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
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