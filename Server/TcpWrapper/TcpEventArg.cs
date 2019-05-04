using System;
using System.Net.Sockets;

namespace Tcp.Server.TcpWrapper
{
    public class TcpEventArg : EventArgs
    {
        public TcpClient TcpClient { get; }

        public TcpEventArg(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }
}