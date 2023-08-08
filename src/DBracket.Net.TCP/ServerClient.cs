using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace DBracket.Net.TCP
{
    public class ServerClient
    {
        #region "----------------------------- Private Fields ------------------------------"
        private ServerClientSettings _settings;
        private TcpListener? _listener;
        private TcpClient? _client;
        internal bool _keepRunning;
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

            //Debug.WriteLine(string.Format("IP Address: {0} - Port: {1}", ipAddress.ToString(), port));

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
                    TakeCareOfTCPClient(_client);
                    ClientConnectionChanged?.Invoke(true);

                    Debug.WriteLine($"Client connected successfully: {_client.Client.RemoteEndPoint}");
                }
            }
            catch (Exception excp)
            {
                System.Diagnostics.Debug.WriteLine(excp.ToString());
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

        private async void TakeCareOfTCPClient(TcpClient _client)
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                StreamReader reader = new StreamReader(stream);
                string? receivedText = string.Empty;
                //Span<char> buffer = new char[2077783];
                //Memory<char> buffer = new Memory<char>();

                while (_keepRunning)
                {
                    bool result = false;
                    //_ = await reader.ReadLineAsync();
                    result = await ReadToEnd(reader);

                    if (!result)
                    {
                        await Task.Delay(10);
                    }

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
                    if (_client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            connected = false;
                        }
                    }

                    if (!connected)
                    {
                        _client.Close();
                        break;
                    }
                    //NewMessageRecieved?.Invoke(clientName, receivedText);
                }
            }
            catch (Exception excp)
            {
                System.Diagnostics.Debug.WriteLine(excp.ToString());
            }

        }
        private char[] _errorBuffer = new char[4096];
        private char[] _messageLengthChars = new char[12];

        public virtual Task<bool> ReadToEnd(StreamReader reader)
        {
            return Task.Run(() => Test(reader));
        }

        private bool Test(StreamReader reader)
        {
            if (_settings.UseClientDataBuffer)
            {
                //if (DiscardNewMessages)
                //{
                //    return false;
                //}
                _loop++;



                for (int i = 0; i < _messageLengthChars.Length; i++)
                {
                    _messageLengthChars[i] = '\0';
                }
                var numberLength = 0;

                numberLength = reader.Read(_messageLengthChars, 0, _messageLengthChars.Length);


                if (numberLength == 12)
                {
                    uint lengthOfTheMessage = 0;
                    //char t2 ;
                    //char t3 ;
                    //char[] test2 = new char[50];

                    try
                    {
                        for (int i = 0; i < _messageLengthChars.Length; i++)
                        {
                            if (_messageLengthChars[i] == '*')
                                _messageLengthChars[i] = '\0';
                        }

                        lengthOfTheMessage = uint.Parse(_messageLengthChars) + 20;
                        // Check if buffer is big enought
                        if (Buffer is null || Buffer?.Length < lengthOfTheMessage+2)
                        {
                            // Buffer is to small, resize
                            Buffer = new char[lengthOfTheMessage+2];
                        }

                        for (int i = 0; i < Buffer.Length; i++)
                        {
                            Buffer[i] = '\0';
                        }

                        var t = reader.Read(Buffer, 0, Buffer.Length);
                        //var message = new string(Buffer);

                        //var y = 0;
                        ////t2 = Buffer[0];
                        ////t3 = Buffer[1];
                        //for (int i = Buffer.Length - 50; i < Buffer.Length; i++)
                        //{
                        //    test2[y++] = Buffer[i];
                        //}
                        // t2 = Buffer[Buffer.Length - 1];
                        // t3 = Buffer[Buffer.Length - 2];
                        //if (t2 != '|' ||
                        //    t3 != '|')
                        //{

                        //}

                        return true;
                    }
                    catch (Exception e)
                    {
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

        private static int _loop;

        //public virtual String ReadToEnd(StreamReader reader)
        //{
        //    Contract.Ensures(Contract.Result<String>() != null);

        //    int len;
        //    StringBuilder sb = new StringBuilder(4096);
        //    while ((len = reader.Read(_chars, 0, _chars.Length)) != 0)
        //    {
        //        sb.Append(_chars, 0, len);
        //    }
        //    return sb.ToString();
        //}

        //public async virtual Task<String> ReadToEndAsync()
        //{
        //    char[] chars = new char[4096];
        //    int len;
        //    StringBuilder sb = new StringBuilder(4096);
        //    while ((len = await ReadAsyncInternal(chars, 0, chars.Length).ConfigureAwait(false)) != 0)
        //    {
        //        sb.Append(chars, 0, len);
        //    }
        //    return sb.ToString();
        //}

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

        public event HandleMessageRecieved? NewMessageRecieved;
        public delegate void HandleMessageRecieved(string message);
        #endregion
        #endregion
    }
}