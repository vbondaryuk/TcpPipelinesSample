using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Extensions;
using Microsoft.Extensions.Configuration;
using Tcp.Server.TcpWrapper;

namespace Tcp.Server
{
    static class Program
    {
        private static IConfiguration _configuration;
        static void Main()
        {
            _configuration = ReadConfiguration();
            string host = _configuration["tcp:host"];
            int port = int.Parse(_configuration["tcp:port"]);

            using (var tcpServer = new TcpServer(host, port))
            {
                tcpServer.Start();
                tcpServer.ClientConnected += Listen;
                Console.WriteLine("Press enter to exit");
                Console.ReadKey();
            }
        }


        private static IConfiguration ReadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            return builder.Build();
        }


        private static void Listen(object sender, TcpEventArg e)
        {
            string file = _configuration["fileForTransfer"];

            Pipe pipe = new Pipe(new PipeOptions(minimumSegmentSize: 8 * 1024));
            _ = FillPipeAsync(pipe.Writer, file);
            _ = SendToClientAsync(pipe.Reader, e.TcpClient);
        }

        private static async Task FillPipeAsync(PipeWriter writer, string path)
        {
            const int minimumBufferSize = 512;

            // This turns off internal file stream buffering
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1))
            {
                while (true)
                {
                    try
                    {
                        Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                        int readBytes = await fileStream.ReadAsync(memory);

                        if (readBytes == 0)
                            break;

                        writer.Advance(readBytes);

                        // Make the data available to the PipeReader
                        if (!await writer.Flush())
                            break;
                    }
                    catch (Exception ex)
                    {
                        writer.Complete(ex);
                        break;
                    }
                }
            }

            writer.Complete();
        }

        static async Task SendToClientAsync(PipeReader reader, TcpClient client)
        {
            Stream tcpStream = client.GetStream();
            while (true)
            {
                ReadResult readResult = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                if (buffer.IsEmpty && readResult.IsCompleted)
                    break;

                foreach (ReadOnlyMemory<byte> segment in buffer)
                    await tcpStream.WriteAsync(segment);

                reader.AdvanceTo(buffer.End);

                if (readResult.IsCompleted)
                    break;
            }

            await tcpStream.FlushAsync();
            tcpStream.Dispose();
            reader.Complete();
        }
    }
}
