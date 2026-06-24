using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MG
{
    public class NetworkManager
    {
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private bool isServer = false;
        private bool isConnected = false;
        private List<TcpClient> clients = new List<TcpClient>();

        // События
        public event Action<int> OnMoveReceived;
        public event Action<string> OnBoardStateReceived;
        public event Action OnRestartReceived; // Новое событие для рестарта

        // ===== ЗАПУСК СЕРВЕРА =====
        public void StartServer(int port)
        {
            try
            {
                isServer = true;
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                Console.WriteLine($"[SERVER] Started on port {port}");
                Task.Run(() => AcceptClients());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Error: {ex.Message}");
            }
        }

        private async Task AcceptClients()
        {
            while (true)
            {
                try
                {
                    var newClient = await server.AcceptTcpClientAsync();
                    clients.Add(newClient);
                    Console.WriteLine($"[SERVER] Client connected! Total: {clients.Count}");
                    Task.Run(() => HandleClient(newClient));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] Accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();
            byte[] buffer = new byte[1024];

            while (tcpClient.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[SERVER] Received: {message}");

                    // Обработка команд
                    if (message == "restart")
                    {
                        OnRestartReceived?.Invoke();
                    }
                    else if (int.TryParse(message, out int cellIndex) && cellIndex >= 0 && cellIndex < 9)
                    {
                        OnMoveReceived?.Invoke(cellIndex);
                    }
                    else if (message.Length == 10) // состояние доски (9 + символ хода)
                    {
                        OnBoardStateReceived?.Invoke(message);
                    }
                }
                catch
                {
                    break;
                }
            }

            clients.Remove(tcpClient);
            tcpClient.Close();
            Console.WriteLine($"[SERVER] Client disconnected. Total: {clients.Count}");
        }

        // ===== ПОДКЛЮЧЕНИЕ КЛИЕНТА =====
        public bool ConnectToServer(string ip, int port)
        {
            try
            {
                client = new TcpClient(ip, port);
                stream = client.GetStream();
                isConnected = true;
                Console.WriteLine($"[CLIENT] Connected to {ip}:{port}");
                Task.Run(() => ListenToServer());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Error: {ex.Message}");
                return false;
            }
        }

        private async Task ListenToServer()
        {
            byte[] buffer = new byte[1024];
            while (client.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[CLIENT] Received: {message}");

                    if (message == "restart")
                    {
                        OnRestartReceived?.Invoke();
                    }
                    else if (message.Length == 10)
                    {
                        OnBoardStateReceived?.Invoke(message);
                    }
                }
                catch
                {
                    break;
                }
            }

            isConnected = false;
            client.Close();
            Console.WriteLine("[CLIENT] Disconnected from server");
        }

        // ===== ОТПРАВКА СООБЩЕНИЙ =====
        public void SendMessage(string message)
        {
            if (isConnected && stream != null)
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine($"[CLIENT] Sent: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLIENT] Send error: {ex.Message}");
                }
            }
        }

        public void BroadcastMessage(string message)
        {
            if (!isServer) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                try
                {
                    var stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch { }
            }
            Console.WriteLine($"[SERVER] Broadcast: {message}");
        }

        public void BroadcastRestart()
        {
            BroadcastMessage("restart");
        }

        // ===== ПРОВЕРКИ =====
        public bool IsConnected() => isConnected || clients.Count > 0;
        public bool IsServer() => isServer;
        public bool HasConnectedClients() => clients.Count > 0;
        public int GetClientCount() => clients.Count;

        public void Close()
        {
            stream?.Close();
            client?.Close();
            foreach (var c in clients) c.Close();
            server?.Stop();
        }
    }
}