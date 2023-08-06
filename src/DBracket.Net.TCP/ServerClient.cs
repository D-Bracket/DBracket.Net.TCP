using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DBracket.Net.TCP
{
    public class ServerClient
    {
        #region "----------------------------- Private Fields ------------------------------"
        private ServerClientSettings _settings;
        private TcpListener? _listener;

        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public ServerClient(ServerClientSettings settings)
        {
            _settings = settings;
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

        //public void StopListeningForClient() { }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private async void TakeCareOfTCPClient(string clientName, TcpClient paramClient)
        {
            try
            {
                NetworkStream stream = paramClient.GetStream();
                StreamReader reader = new StreamReader(stream);
                string? receivedText = string.Empty;
                //Span<char> buffer = new char[2077783];
                //Memory<char> buffer = new Memory<char>();

                while (KeepRunning)
                {
                    //Task.Delay(10).Wait();
                    _ = await reader.ReadLineAsync();
                    //_ = reader.Read(buffer);
                    ////_ = reader.Read();
                    ////_ = await reader.ReadAsync(buffer);

                    //if (buffer.Length != 0)
                    //{

                    //}
                    //if (receivedText is null)
                    //{
                    //    receivedText = string.Empty;
                    //}

                    //Check connection to client
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

                    if (!connected)
                    {
                        paramClient.Close();
                        break;
                    }
                    //NewMessageRecieved?.Invoke(clientName, receivedText);
                }
            }
            catch (Exception excp)
            {
                RemoveClient(clientName);
                System.Diagnostics.Debug.WriteLine(excp.ToString());
            }

        }

        public virtual String ReadToEnd(StreamReader reader)
        {
            Contract.Ensures(Contract.Result<String>() != null);

            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while ((len = reader.Read(chars, 0, chars.Length)) != 0)
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
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

        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}