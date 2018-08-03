using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tcp.Server.TcpWrapper
{
	public class TcpServer : IDisposable
	{
		private readonly string ipAddress;
		private readonly int port;
		private readonly TcpListener listener;
		private bool isStarted;

		public TcpServer(string ipAddress, int port)
		{
			this.ipAddress = ipAddress;
			this.port = port;
			IPAddress address = IPAddress.Parse(ipAddress);
			listener = new TcpListener(address, port);
			isStarted = false;
		}

		public void Start()
		{
			Console.WriteLine($"Tcp server was started on:{ipAddress}:{port}");
			listener.Start();
			isStarted = true;

			Task.Run(() => Listen());
		}

		public void Stop()
		{
			if (isStarted)
			{
				listener.Stop();
				isStarted = false;
			}
		}

		public event EventHandler<TcpEventArg> ClientConnected;

		public void Dispose()
		{
			Stop();
		}

		private void Listen()
		{
			if (listener != null)
			{
				while (true)
				{
					Console.WriteLine("Waiting for a client...");
					TcpClient client = listener.AcceptTcpClient();

					Console.WriteLine("Client connected.");
					OnClientConectedEvent(client);
					client.GetStream().Dispose();
					Console.WriteLine("Client disposed.");
				}
			}
		}

		private void OnClientConectedEvent(TcpClient client)
		{
			var tcpEventArg = new TcpEventArg(client);
			EventHandler<TcpEventArg> clientConnectedEvent = ClientConnected;
			clientConnectedEvent?.Invoke(this, tcpEventArg);
		}
	}
}