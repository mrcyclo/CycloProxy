﻿using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace CycloProxyCore
{
    public class ProxyServer
    {
        private bool _isRunning = false;
        private TcpListener _tcpListener;

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
                Task.Run(() => HandleClientRequest(client));
            }
        }

        private async void HandleClientRequest(TcpClient client)
        {
            TcpClient remote = new TcpClient();

            try
            {
                // Try to read first header
                bool isRead = ReceiveFirstHeaderFromClient(client, out HttpRequest request);
                if (isRead == false) return;

                // Handle connect request
                if (request.Method == HttpMethod.CONNECT)
                {
                    client.Client.Send(Encoding.UTF8.GetBytes(request.HttpVersion + " 200 Connection Established\r\n"));
                }

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
                    remote.Connect(ipEndPoint);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Can not connect to {request.Url.DnsSafeHost}.");
                    return;
                }

                // Send first header
                try
                {
                    remote.Client.Send(request.HeaderBytes);
                    Console.WriteLine("Client -> Remote (first package).");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                Tunnel tunnel = new Tunnel(client, remote);
                while (tunnel.IsActive())
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                client.Dispose();
                remote.Dispose();
            }
        }

        private bool ReceiveFirstHeaderFromClient(TcpClient client, out HttpRequest request)
        {
            request = new HttpRequest();

            // Try to read to the end of request header
            List<byte> reqBytelist = new List<byte>();
            byte[] buffer = new byte[1];

            while (reqBytelist.Count < 4096)
            {
                int bytesRead = client.Client.Receive(buffer);
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
    }
}