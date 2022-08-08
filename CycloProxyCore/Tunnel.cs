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

        private TcpState[] TCP_ACTIVE_STATES = new TcpState[]
        {
            TcpState.Established,
            TcpState.Listen,
            TcpState.SynReceived,
            TcpState.SynSent,
            TcpState.TimeWait
        };

        public Tunnel(TcpClient src, TcpClient dest)
        {
            _srcTcpClient = src;
            _destTcpClient = dest;

            //_srcTcpClient.NoDelay = true;
            //_srcTcpClient.Client.NoDelay = true;

            //_destTcpClient.NoDelay = true;
            //_destTcpClient.Client.NoDelay = true;

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
            if (_isActive == false) return false;

            if (_srcTcpClient.Connected == false) return false;
            if (_destTcpClient.Connected == false) return false;

            TcpState srcState = GetTcpState(_srcTcpClient);
            if (TCP_ACTIVE_STATES.Contains(srcState) == false) return false;

            TcpState destState = GetTcpState(_destTcpClient);
            if (TCP_ACTIVE_STATES.Contains(destState) == false) return false;

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
                    if (bytesRead > 0)
                    {
                        _destTcpClient.Client.Send(buffer, 0, bytesRead, SocketFlags.None);
                        Console.WriteLine("Client -> Remote.");
                    }

                    if (_isActive) continue;
                    break;
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
                    if (bytesRead > 0)
                    {
                        _srcTcpClient.Client.Send(buffer, 0, bytesRead, SocketFlags.None);
                        Console.WriteLine("Client <- Remote.");
                    }

                    if (_isActive) continue;
                    break;
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
