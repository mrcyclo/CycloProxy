using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CycloProxyCore
{
    public class ProxyServer
    {
        private bool _isRunning = false;
        private TcpListener _tcpListener;
        private List<Pipeline> _streamMap;

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
                Task.Run(() => new Pipeline(client));
            }
        }
    }
}