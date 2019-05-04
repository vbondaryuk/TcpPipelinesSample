using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tcp.Server.TcpWrapper
{
    public class TcpServer : IDisposable
    {
        private readonly string _ipAddress;
        private readonly TcpListener _listener;
        private readonly int _port;
        private bool _isStarted;

        public TcpServer(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            var address = IPAddress.Parse(ipAddress);
            _listener = new TcpListener(address, port);
            _isStarted = false;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            Console.WriteLine($"Tcp server was started on: {_ipAddress}:{_port}");
            _listener.Start();
            _isStarted = true;

            _ = Listen();
        }

        public void Stop()
        {
            if (_isStarted)
            {
                _listener.Stop();
                _isStarted = false;
            }
        }

        public event EventHandler<TcpEventArg> ClientConnected;

        private async Task Listen()
        {
            if (_listener == null)
                return;

            while (true)
            {
                Console.WriteLine("Waiting for a client...");
                var client = await _listener.AcceptTcpClientAsync();

                Console.WriteLine("Client connected.");
                OnClientConnectedEvent(client);
                Console.WriteLine("Client disposed.");
            }
        }

        private void OnClientConnectedEvent(TcpClient client)
        {
            var tcpEventArg = new TcpEventArg(client);
            var clientConnectedEvent = ClientConnected;
            clientConnectedEvent?.Invoke(this, tcpEventArg);
        }
    }
}