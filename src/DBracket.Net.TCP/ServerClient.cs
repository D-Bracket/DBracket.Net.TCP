using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DBracket.Net.TCP
{
    public class ServerClient
    {
        #region "----------------------------- Private Fields ------------------------------"
        private ServerClientSettings _settings;
        private TcpListener? _listener;
        private TcpClient? _client;
        internal bool _keepRunning;

        private char[] _errorBuffer = new char[4096];
        private char[] _messageLengthChars = new char[12];
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public ServerClient(ServerClientSettings settings)
        {
            _settings = settings;
            StartListeningForIncomingConnection(_settings.IPAddress, _settings.Port);
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public async void StartListeningForIncomingConnection(IPAddress ipAddress, int port)
        {
            if (ipAddress is null)
            {
                throw new ArgumentException("No IP Address selected. To accept any IP parse in the argument IPAddress.Any");
            }

            if (port <= 0)
            {
                return;
            }

            _listener = new TcpListener(ipAddress, port);

            try
            {
                _listener.Start();
                _keepRunning = true;

                while (_keepRunning)
                {
                    Debug.WriteLine("Start Listening again");
                    _client = await _listener.AcceptTcpClientAsync();
                    CheckConnectionState(_client);
                    await TakeCareOfTCPClient(_client);
                    ClientConnectionChanged?.Invoke(true);

                    Debug.WriteLine($"Client connected successfully: {_client.Client.RemoteEndPoint}");
                }
            }
            catch (Exception excp)
            {
                System.Diagnostics.Debug.WriteLine(excp.ToString());
            }
        }

        public async void SendToClient(string message)
        {
            if (_client is not null && _client.Connected)
            {
                try
                {
                    StreamWriter clientStreamWriter = new StreamWriter(_client.GetStream());
                    clientStreamWriter.AutoFlush = true;
                    await clientStreamWriter.WriteLineAsync("d");
                }
                catch (Exception ex)
                { }
            }
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void CheckConnectionState(TcpClient client)
        {
            Task.Run(() =>
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
                            Task.Run(() => ClientConnectionChanged?.Invoke(false));
                            break;
                        }
                        else if (client.Client is null)
                        {
                            // Client disconnected
                            Task.Run(() => ClientConnectionChanged?.Invoke(false));
                            break;
                        }

                        if (client.Client is not null && client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (client.Client?.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                Task.Run(() => ClientConnectionChanged?.Invoke(false));
                                break;
                            }
                        }
                    }
                    catch
                    {
                    }

                    Task.Delay(10).Wait();
                }
            });
        }

        private Task TakeCareOfTCPClient(TcpClient _client)
        {
            return Task.Run(() =>
            {
                try
                {
                    var stream = _client.GetStream();
                    var reader = new StreamReader(stream);

                    while (_keepRunning)
                    {
                        bool result = false;
                        result = ReadToEnd(reader).Result;

                        if (!result)
                        {
                            Task.Delay(10).Wait();
                        }

                        Task.Run(() => BufferUpdated?.Invoke(Buffer));
                    }
                }
                catch (Exception excp)
                {
                    System.Diagnostics.Debug.WriteLine(excp.ToString());
                }
            });
        }

        private Task<bool> ReadToEnd(StreamReader reader)
        {
            return Task.Run(() => ReadMessageToEnd(reader));
        }

        private bool ReadMessageToEnd(StreamReader reader)
        {
            if (_settings.UseClientDataBuffer)
            {
                for (int i = 0; i < _messageLengthChars.Length; i++)
                {
                    _messageLengthChars[i] = '\0';
                }
                var numberLength = 0;

                numberLength = reader.Read(_messageLengthChars, 0, _messageLengthChars.Length);


                if (numberLength == 12)
                {
                    uint lengthOfTheMessage = 0;

                    try
                    {
                        for (int i = 0; i < _messageLengthChars.Length; i++)
                        {
                            if (_messageLengthChars[i] == '*')
                                _messageLengthChars[i] = '\0';
                        }

                        lengthOfTheMessage = uint.Parse(_messageLengthChars) + 20;
                        // Check if buffer is big enought
                        if (Buffer is null || Buffer?.Length < lengthOfTheMessage + 2)
                        {
                            // Buffer is to small, resize
                            Buffer = new char[lengthOfTheMessage + 2];
                        }

                        for (int i = 0; i < Buffer.Length; i++)
                        {
                            Buffer[i] = '\0';
                        }

                        var t = reader.Read(Buffer, 0, Buffer.Length);

                        return true;
                    }
                    catch (Exception e)
                    {
                        // Find start of new message if error occured
                        while ((_ = reader.Read(_errorBuffer, 0, _errorBuffer.Length)) != 0)
                        {
                            try
                            {
                                var stop = false;
                                for (int i = 0; i < _errorBuffer.Length; i++)
                                {
                                    if (i == _errorBuffer.Length - 1)
                                        continue;
                                    if (_errorBuffer[i] == '|' && _errorBuffer[i + 1] == '|')
                                    {
                                        stop = true;
                                        break;
                                    }
                                }
                                if (stop)
                                    break;
                            }
                            catch (Exception ex)
                            { 
                            
                            }
                        }
                    }

                }
            }

            return false;
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public char[] Buffer { get; private set; }
        public bool DiscardNewMessages { get; set; } = true;
        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event ClientConnectionChangedHandler? ClientConnectionChanged;
        public delegate void ClientConnectionChangedHandler(bool newState);

        public event HandleBufferUpdated? BufferUpdated;
        public delegate void HandleBufferUpdated(Memory<char> buffer);

        public event HandleMessageRecieved? NewMessageRecieved;
        public delegate void HandleMessageRecieved(string message);
        #endregion
        #endregion
    }
}