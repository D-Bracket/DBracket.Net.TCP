using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DBracket.Net.TCP
{
    public class Client
    {
        #region "----------------------------- Private Fields ------------------------------"
        IPAddress? _serverIPAddress;
        int _serverPort;
        TcpClient? _client;
        private StreamReader? _clientStreamReader;
        private CancellationTokenSource? _cts;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public Client()
        {
            _client = null;
            _serverPort = -1;
            _serverIPAddress = null;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public bool SetServerIPAddress(string ipAddress)
        {
            IPAddress? ipaddr = null;

            if (!IPAddress.TryParse(ipAddress, out ipaddr))
            {
                Console.WriteLine("Invalid server IP supplied.");
                return false;
            }

            _serverIPAddress = ipaddr;

            return true;
        }

        public bool SetPortNumber(string port)
        {
            int portNumber = 0;

            if (!int.TryParse(port.Trim(), out portNumber))
            {
                Console.WriteLine("Invalid port number supplied, return.");
                return false;
            }

            if (portNumber <= 0 || portNumber > 65535)
            {
                Console.WriteLine("Port number must be between 0 and 65535.");
                return false;
            }

            _serverPort = portNumber;

            return true;
        }

        public async Task ConnectToServer()
        {
            _client = new TcpClient();

            try
            {
                if (_serverIPAddress is null)
                {
                    Debug.WriteLine("No ip entered, client can't connect to server");
                    return;
                }

                Debug.WriteLine($"Client wants to connect to: {_serverIPAddress}:{_serverPort}");

                await _client.ConnectAsync(_serverIPAddress, _serverPort);
                Console.WriteLine(string.Format("Connected to server IP/Port: {0} / {1}",
                    _serverIPAddress, _serverPort));

                if (_client.Connected)
                {
                    _cts = new CancellationTokenSource();
                    Debug.WriteLine("Client connected");
#pragma warning disable CS4014
                    Task.Run(() => CheckConnectionState());
                    Task.Run(() => ReadData(_client, _cts.Token));
#pragma warning restore CS4014
                    ConnectionChanged?.Invoke(true);
                }
            }
            catch (Exception excp)
            {
                Console.WriteLine(excp.ToString());
                //throw;
            }
        }

        public void CloseAndDisconnect()
        {
            if (_client is not null)
            {
                if (_client.Connected)
                {
                    if (_cts is not null)
                    {
                        _cts.Cancel();
                    }

                    _client.Client.Shutdown(SocketShutdown.Both);

                    if (_clientStreamReader is not null)
                    {
                        _clientStreamReader.Close();
                        _clientStreamReader.Dispose();
                        _clientStreamReader = null;
                    }

                    _client.Close();

                    ConnectionChanged?.Invoke(false);
                }
            }
        }

        public async Task SendToServer(string strInputUser)
        {
            if (string.IsNullOrEmpty(strInputUser))
            {
                return;
            }

            if (_client != null)
            {
                if (_client.Connected)
                {
                    StreamWriter clientStreamWriter = new StreamWriter(_client.GetStream());
                    clientStreamWriter.AutoFlush = true;

                    await clientStreamWriter.WriteLineAsync(strInputUser);
                }
            }
        }

        public async Task SendToServer(char[] strInputUser)
        {
            if (_client != null)
            {
                if (_client.Connected)
                {
                    StreamWriter clientStreamWriter = new StreamWriter(_client.GetStream());
                    clientStreamWriter.AutoFlush = true;

                    await clientStreamWriter.WriteLineAsync(strInputUser);
                }
            }
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void CheckConnectionState()
        {
            while(true) 
            {
                if (_client is null)
                {
                    break;
                }
                else
                {
                    if (_client.Client is null)
                    {
                        break;
                    }
                }

                if (_client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        CloseAndDisconnect();
                        ConnectionChanged?.Invoke(false);
                        break;
                    }
                }
                Task.Delay(10).Wait();
            }
        }

        private void ReadData(TcpClient mClient, CancellationToken token)
        {
            try
            {
                //MessageLogger.LogMessage("bcnc.HMI.Data.PLC", "PLCControl", LogTypes.MessageLog,
                //    $"Start reading data from server");
                //Debug.WriteLine("Reading?");
                _clientStreamReader = new StreamReader(mClient.GetStream());

                while (true)
                {
                    //Debug.WriteLine("Client Start Listening");
                    var message = _clientStreamReader.ReadLineAsync(token).Result;

                    if (token.IsCancellationRequested)
                    {
                        Debug.WriteLine("Client listening was canceled");
                        break;
                    }
                    //Debug.WriteLine("Message recieved");
                    message ??= string.Empty;
                    //NewMessageRecieved?.Invoke(message);
                    ReportNewMessage(message);
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine($"Client was closed, reading was cancelled: {ex.Message}");
            }
            catch (Exception excp)
            {
                //MessageLogger.LogMessage("bcnc.HMI.Data.PLC", "PLCControl", LogTypes.MessageLog,
                //    $"Error while reading data from server: {excp.Message}");
                Debug.WriteLine(excp.ToString());
                //throw;
            }
        }

        private void ReportNewMessage(string message)
        {
            Task.Run(() => NewMessageRecieved?.Invoke(message));
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public IPAddress? ServerIPAddress
        {
            get
            {
                return _serverIPAddress;
            }
        }

        public int ServerPort
        {
            get
            {
                return _serverPort;
            }
        }

        public bool IsConnected { get => _client is not null && _client.Connected; }
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event ConnectionChangedHandler? ConnectionChanged;
        public delegate void ConnectionChangedHandler(bool newState);

        public event HandleMessageRecieved? NewMessageRecieved;
        public delegate void HandleMessageRecieved(string message);
        #endregion
        #endregion    
    }
}