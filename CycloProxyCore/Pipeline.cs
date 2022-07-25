using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CycloProxyCore
{
    public class Pipeline
    {
        private TcpClient _clientTcpClient;
        private TcpClient _remoteTcpClient;
        private Socket _clientSocket;
        private Socket _remoteSocket;

        public Pipeline(TcpClient client)
        {
            _clientTcpClient = client;
            _clientSocket = client.Client;

            // Try to read first header
            bool isRead = ReceiveFirstHeaderFromClient(out HttpRequest request);
            if (isRead == false) return;

            // Dns resolve
            IPHostEntry ipHostEntry = Dns.GetHostEntry(request.Url.DnsSafeHost);
            if (ipHostEntry.AddressList.Length == 0)
            {
                Console.WriteLine($"Can not resolve ip of {request.Url.DnsSafeHost}.");
                return;
            }

            // Get host ip
            IPAddress ipAddress = ipHostEntry.AddressList.First();
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, request.Url.Port);

            // Create remote tcp client
            try
            {
                _remoteTcpClient = new TcpClient();
                _remoteTcpClient.Connect(ipEndPoint);
            }
            catch (Exception)
            {
                Console.WriteLine($"Can not connect to {request.Url.DnsSafeHost}.");
                return;
            }

            // Set remote socket
            _remoteSocket = _remoteTcpClient.Client;

            // Send first header
            try
            {
                _remoteSocket.Send(request.HeaderBytes);
                Console.WriteLine("Client -> Remote (first package).");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            // Start pipeline
            Task.Run(ClientToRemote);
            Task.Run(RemoteToClient);
        }

        private bool ReceiveFirstHeaderFromClient(out HttpRequest request)
        {
            request = new HttpRequest();

            // Try to read to the end of request header
            List<byte> reqBytelist = new List<byte>();
            byte[] buffer = new byte[1];

            while (true)
            {
                int bytesRead = _clientSocket.Receive(buffer);
                if (bytesRead == 0) break;

                reqBytelist.AddRange(buffer);
                if (reqBytelist.Count < 4) continue;

                // Check if reach to the end of request header, break loop
                if (
                    reqBytelist[reqBytelist.Count - 4] == 13 &&
                    reqBytelist[reqBytelist.Count - 3] == 10 &&
                    reqBytelist[reqBytelist.Count - 2] == 13 &&
                    reqBytelist[reqBytelist.Count - 1] == 10
                )
                {
                    break;
                }
            }

            // Client send no data
            if (reqBytelist.Count == 0) return false;

            // Try to parse request header
            bool isHttpRequest = request.TryParseHeaderFromBytes(reqBytelist.ToArray());

            // If request is not http request
            if (!isHttpRequest)
            {
                Console.WriteLine("Request is not http request.");
                return false;
            }

            // ----------------------- //
            // Read request here later //
            // ----------------------- //

            // Print debug
            Console.WriteLine($"{request.Method} {request.Url}");

            return true;
        }

        private void ClientToRemote()
        {
            while (!_clientSocket.Poll(1, SelectMode.SelectError))
            {
                byte[] buffer = new byte[4096];
                int bytesRead = _clientSocket.Receive(buffer);
                if (bytesRead == 0) continue;

                _remoteSocket.Send(buffer, 0, bytesRead, SocketFlags.None);

                Console.WriteLine("Client -> Remote.");
            }
        }

        private void RemoteToClient()
        {
            while (!_remoteSocket.Poll(1, SelectMode.SelectError))
            {
                byte[] buffer = new byte[4096];
                int bytesRead = _remoteSocket.Receive(buffer);
                if (bytesRead == 0) continue;

                _clientSocket.Send(buffer, 0, bytesRead, SocketFlags.None);

                Console.WriteLine("Client <- Remote.");
            }
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
