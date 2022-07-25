using System;
using System.Collections.Generic;
using System.Linq;
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

            Task.Run(ReceivePacketFromClient);
        }

        private void ReceivePacketFromClient()
        {
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

            // Try to parse request header
            HttpRequest request = new HttpRequest();
            bool isHttpRequest = request.TryParseHeaderFromBytes(reqBytelist.ToArray());

            // If request is not http request
            if (!isHttpRequest)
            {
                Console.WriteLine("Request is not http request.");
                return;
            }

            // Read request body
            reqBytelist.Clear();
            buffer = new byte[1024];
            while (true)
            {
                int bytesRead = _clientSocket.Receive(buffer);
                if (bytesRead == 0) break;

                byte[] realBytes = new byte[bytesRead];
                Array.Copy(buffer, realBytes, bytesRead);

                reqBytelist.AddRange(realBytes);
            }

            // Set request body
            request.SetByteBody(reqBytelist.ToArray());

            // Print debug
            Console.WriteLine($"{request.Method} {request.Url}");
        }
    }
}
