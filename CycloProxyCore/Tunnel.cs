using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CycloProxyCore
{
    public class Tunnel
    {
        private bool _isActive = false;

        private TcpClient _srcTcpClient;
        private TcpClient _destTcpClient;

        public Tunnel(TcpClient src, TcpClient dest)
        {
            _srcTcpClient = src;
            _destTcpClient = dest;

            _srcTcpClient.NoDelay = true;
            _srcTcpClient.Client.NoDelay = true;

            _destTcpClient.NoDelay = true;
            _destTcpClient.Client.NoDelay = true;

            Task.Run(SourceReaderAsync);
            Task.Run(DestinationReaderAsync);

            _isActive = true;
        }

        private TcpState GetTcpState(TcpClient tcpClient)
        {
            var state = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .FirstOrDefault(x => x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));
            return state != null ? state.State : TcpState.Unknown;
        }

        public bool IsActive()
        {
            return _isActive;
        }

        private void SourceReaderAsync()
        {
            try
            {
                byte[] buffer = new byte[65536];
                while (true)
                {
                    int bytesRead = _srcTcpClient.Client.Receive(buffer);
                    if (bytesRead == 0) continue;

                    _destTcpClient.Client.Send(buffer, 0, bytesRead, SocketFlags.None);
                    Console.WriteLine("Client -> Remote.");
                }
            }
            //catch (ObjectDisposedException)
            //{
            //    _isActive = false;
            //}
            //catch (SocketException)
            //{
            //    _isActive = false;
            //}
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _isActive = false;
            }
        }

        private void DestinationReaderAsync()
        {
            try
            {
                byte[] buffer = new byte[65536];
                while (true)
                {
                    int bytesRead = _destTcpClient.Client.Receive(buffer);
                    if (bytesRead == 0) continue;

                    _srcTcpClient.Client.Send(buffer, 0, bytesRead, SocketFlags.None);
                    Console.WriteLine("Client <- Remote.");
                }
            }
            //catch (ObjectDisposedException)
            //{
            //    _isActive = false;
            //}
            //catch (SocketException)
            //{
            //    _isActive = false;
            //}
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _isActive = false;
            }
        }
    }
}
