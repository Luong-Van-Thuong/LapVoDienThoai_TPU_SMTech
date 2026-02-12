using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TPU_Assembly.Class
{
    public class TCP_Server(string ip, int port)
    {
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private Thread listenThread;
        private bool isRunning;

        public event Action<string> OnDataReceived;
        public event Action<string> OnClientConnected;
        public event Action OnClientDisconnected;

        public event Action<string> OnError;

        public string ServerIP { get; private set; } = ip;
        public int ServerPort { get; private set; } = port;

        public bool Start()
        {
            try
            {
                IPAddress ipAddr = IPAddress.Parse(ServerIP);
                server = new TcpListener(ipAddr, ServerPort);
                server.Start();
                isRunning = true;

                listenThread = new Thread(ListenForClients) { IsBackground = true };
                listenThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Start Error: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            isRunning = false;
            server?.Stop();
            server = null;
        }

        private void ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    client = server.AcceptTcpClient();
                    stream = client.GetStream();

                    string clientIP = "Unknown";
                    if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
                    {
                        clientIP = endPoint.Address.ToString();
                    }

                    OnClientConnected?.Invoke(clientIP);

                    ListenForData();
                }
                catch (Exception ex)
                {
                    if (isRunning) OnError?.Invoke($"Listener Error: {ex.Message}");
                }
            }
        }

        private void ListenForData()
        {
            byte[] buffer = new byte[4096];
            while (isRunning && client.Connected)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    OnDataReceived?.Invoke(data);
                }
                catch
                {
                    break;
                }
            }
            OnClientDisconnected?.Invoke();
        }
    }
}