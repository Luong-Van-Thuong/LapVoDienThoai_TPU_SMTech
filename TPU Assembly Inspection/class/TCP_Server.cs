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

            //try { stream?.Close(); } catch { }
            //try { client?.Close(); } catch { }
            //try { server?.Stop(); } catch { }

            //stream = null;
            //client = null;
            //server = null;

            isRunning = false;
            server?.Stop();
            server = null;
        }

        public void Send(string message)
        {
            try
            {
                string messageToSend = $"\x02{message}\x03";
                if (client != null && client.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes(messageToSend);
                    stream.Write(data, 0, data.Length);
                    MSystem.InsertAndSaveLogs($"Sent to Robot: {message}", Color.Blue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi: {ex.Message}");
            }
        }

        public bool IsConnected()
        {
            return client != null && client.Connected;
        }

        public string GetClientIP()
        {
            if (client != null && client.Connected)
            {
                return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            }
            return "Không có client";
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