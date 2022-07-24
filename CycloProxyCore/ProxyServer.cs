using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CycloProxyCore
{
    public class ProxyServer
    {
        private bool _isRunning = false;
        private TcpListener _tcpListener;
        private List<StreamMapItem> _streamMap;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        public void Start(IPAddress ipAddress, int port)
        {
            AllocConsole();

            _tcpListener = new TcpListener(ipAddress, port);
            _tcpListener.Start();

            _isRunning = true;
            Task.Run(ProxyLoop);
        }

        public void Stop()
        {
            _tcpListener.Stop();
            _isRunning = false;

            FreeConsole();
        }

        private void ProxyLoop()
        {
            while (_isRunning)
            {
                TcpClient client = _tcpListener.AcceptTcpClient();
                Task.Run(() => SocketHandler(client));
            }
        }

        private void SocketHandler(TcpClient client)
        {
            Console.WriteLine($"AcceptSocket: {client.Client.RemoteEndPoint}");

            IPHostEntry ipHostInfo = Dns.GetHostEntry("example.com");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, 80);

            TcpClient remote = new TcpClient(ipAddress.ToString(), 80);

            Task.Run(() => StreamReadHandler(client.Client, remote.Client));
            Task.Run(() => StreamWriteHandler(client.Client, remote.Client));
        }

        private async void StreamReadHandler(Socket client, Socket remote)
        {
            byte[] buffer = new byte[1024];
            while (SocketIsAlive(client) && SocketIsAlive(remote))
            {
                try
                {
                    int bytesRead = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0) continue;

                    Console.WriteLine("Client -> Remote");
                    remote.Send(buffer, 0, bytesRead, SocketFlags.None);
                }
                catch (Exception) { }
            }

            client.Dispose();
            remote.Dispose();

            Console.WriteLine("StreamReadHandler completed");
        }

        private async void StreamWriteHandler(Socket client, Socket remote)
        {
            byte[] buffer = new byte[1024];
            while (SocketIsAlive(client) && SocketIsAlive(remote))
            {
                try
                {
                    int bytesRead = await remote.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0) continue;

                    Console.WriteLine("Client <- Remote");
                    client.Send(buffer, 0, bytesRead, SocketFlags.None);
                }
                catch (Exception) { }
            }

            client.Dispose();
            remote.Dispose();

            Console.WriteLine("StreamWriteHandler completed");
        }

        public static bool SocketIsAlive(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}