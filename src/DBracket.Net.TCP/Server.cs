using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace DBracket.Net.TCP
{
    public class Server
    {
        #region "----------------------------- Private Fields ------------------------------"
        //private TcpListener? _listener;
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
        public void AddClient(ServerClientSettings settings)
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
        }

//        public async void StartListeningForIncomingConnection(string clientName, IPAddress ipAddress, int port)
//        {
//            if (ipAddress is null)
//            {
//                throw new ArgumentException("No IP Address selected. To accept any IP parse in the argument IPAddress.Any");
//            }

//            if (port <= 0)
//            {
//                return;
//            }

//            //_ipAddress = ipAddress;
//            //_port = port;

//            Debug.WriteLine(string.Format("IP Address: {0} - Port: {1}", ipAddress.ToString(), port));

//            _listener = new TcpListener(ipAddress, port);

//            try
//            {
//                _listener.Start();
//                KeepRunning = true;

//                while (KeepRunning)
//                {
//                    Debug.WriteLine("Start Listening again");
//                    var returnedByAccept = await _listener.AcceptTcpClientAsync();

//                    if (_clients.ContainsKey(clientName))
//                    {
//                        _clients[clientName] = returnedByAccept;
//                    }
//                    else
//                    {
//                        _clients.Add(clientName, returnedByAccept);
//                    }

//#pragma warning disable CS4014
//                    Task.Run(() => CheckConnectionState(clientName, returnedByAccept));
//#pragma warning restore CS4014
//                    TakeCareOfTCPClient(clientName, returnedByAccept);
//                    ClientConnectionChanged?.Invoke(clientName, true);

//                    Debug.WriteLine(
//                        string.Format("Client connected successfully, count: {0} - {1}",
//                        _clients.Count, returnedByAccept.Client.RemoteEndPoint));
//                }
//            }
//            catch (Exception excp)
//            {
//                System.Diagnostics.Debug.WriteLine(excp.ToString());
//            }
//        }

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
        //private async void TakeCareOfTCPClient(string clientName, TcpClient paramClient)
        //{
        //    try
        //    {
        //        NetworkStream stream = paramClient.GetStream();
        //        StreamReader reader = new StreamReader(stream);
        //        string? receivedText = string.Empty;
        //        //Span<char> buffer = new char[2077783];
        //        //Memory<char> buffer = new Memory<char>();

        //        while (KeepRunning)
        //        {
        //            //Task.Delay(10).Wait();
        //            _ = await reader.ReadLineAsync();
        //            //_ = reader.Read(buffer);
        //            ////_ = reader.Read();
        //            ////_ = await reader.ReadAsync(buffer);

        //            //if (buffer.Length != 0)
        //            //{

        //            //}
        //            //if (receivedText is null)
        //            //{
        //            //    receivedText = string.Empty;
        //            //}

        //            //Check connection to client
        //            bool connected = true;
        //            if (paramClient.Client.Poll(0, SelectMode.SelectRead))
        //            {
        //                byte[] buff = new byte[1];
        //                if (paramClient.Client.Receive(buff, SocketFlags.Peek) == 0)
        //                {
        //                    // Client disconnected
        //                    connected = false;
        //                }
        //            }

        //            if (!connected)
        //            {
        //                paramClient.Close();
        //                break;
        //            }
        //            //NewMessageRecieved?.Invoke(clientName, receivedText);
        //        }
        //    }
        //    catch (Exception excp)
        //    {
        //        RemoveClient(clientName);
        //        System.Diagnostics.Debug.WriteLine(excp.ToString());
        //    }

        //}

        //public virtual String ReadToEnd(StreamReader reader)
        //{
        //    Contract.Ensures(Contract.Result<String>() != null);

        //    char[] chars = new char[4096];
        //    int len;
        //    StringBuilder sb = new StringBuilder(4096);
        //    while ((len = reader.Read(chars, 0, chars.Length)) != 0)
        //    {
        //        sb.Append(chars, 0, len);
        //    }
        //    return sb.ToString();
        //}

        private void RemoveClient(string clientName)
        {
            if (_clients.ContainsKey(clientName))
            {
                _clients.Remove(clientName);
                Debug.WriteLine(String.Format("Client removed, count: {0}", _clients.Count));
            }
        }

        //private void CheckConnectionState(string clientName, TcpClient client)
        //{
        //    while (true)
        //    {
        //        if (client is null)
        //        {
        //            break;
        //        }

        //        try
        //        {
        //            if (!client.Connected)
        //            {
        //                // Client disconnected
        //                Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
        //                break;
        //            }
        //            else if (client.Client is null)
        //            {
        //                // Client disconnected
        //                Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
        //                break;
        //            }

        //            if (client.Client is not null && client.Client.Poll(0, SelectMode.SelectRead))
        //            {
        //                byte[] buff = new byte[1];
        //                if (client.Client?.Receive(buff, SocketFlags.Peek) == 0)
        //                {
        //                    // Client disconnected
        //                    Task.Run(() => ClientConnectionChanged?.Invoke(clientName, false));
        //                    break;
        //                }
        //            }
        //        }
        //        catch 
        //        {
        //        }

        //        Task.Delay(10).Wait();
        //    }
        //}
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