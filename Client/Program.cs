using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Extensions;
using Microsoft.Extensions.Configuration;

namespace Tcp.Client
{
    static class Program
    {
        private static IConfiguration _configuration;
        static void Main()
        {
            _configuration = ReadConfiguration();

            string host = _configuration["tcp:host"];
            int port = int.Parse(_configuration["tcp:port"]);

            var pipe = new Pipe();
            Task.WaitAll(
                TcpReaderAsync(pipe.Writer, host, port),
                FileWriteAsync(pipe.Reader)
            );
            Console.ReadLine();
        }

        private static IConfiguration ReadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            return builder.Build();
        }


        private static async Task TcpReaderAsync(PipeWriter writer, string host, int port)
        {
            const int minimumBufferSize = 512;

            using (TcpClient client = new TcpClient())
            {
                Console.WriteLine("Connecting to server.");
                await client.ConnectAsync(host, port);
                Console.WriteLine("Connected.");

                using (NetworkStream stream = client.GetStream())
                {
                    while (true)
                    {
                        try
                        {
                            Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                            int read = await stream.ReadAsync(memory);

                            if (read == 0)
                                break;

                            writer.Advance(read);

                            Console.WriteLine($"Read from stream {read} bytes");

                        }
                        catch
                        {
                            break;
                        }

                        // Make the data available to the PipeReader
                        if (!await writer.Flush())
                            break;
                    }

                    Console.WriteLine("Message was read");
                }
            }
            writer.Complete();
        }

        static async Task FileWriteAsync(PipeReader reader)
        {
            string file = _configuration["filePath"];

            using (FileStream fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                while (true)
                {
                    ReadResult readResult = await reader.ReadAsync();

                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    if (buffer.IsEmpty && readResult.IsCompleted)
                        break;

                    foreach (ReadOnlyMemory<byte> segment in buffer)
                        await fileStream.WriteAsync(segment);

                    reader.AdvanceTo(buffer.End);

                    Console.WriteLine($"Append to file {buffer.Length} bytes");
                }

                reader.Complete();
            }
        }
    }
}
