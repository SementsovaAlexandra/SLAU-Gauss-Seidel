using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Curs1
{
    internal class SolverServer
    {
        public static void Serve(int port)
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            try
            {
                server.Start();
                Console.WriteLine($"Server is listening on :{port}");

                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        private static void HandleClient(TcpClient client)
        {
            try
            {
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

                NetworkStream stream = client.GetStream();

                var problem = Transport<Problem>.Receive(stream);
                Console.WriteLine($"Received problem {problem.StartRow},{problem.RowCount}/{problem.B.Length}");

                while (true)
                {
                    var xs = Transport<Solution>.Receive(stream);
                    Console.WriteLine($"Received iteration task");

                    var sw = Stopwatch.StartNew();
                    var solution = problem.Solve(xs);
                    sw.Stop();
                    Console.WriteLine($"Calculated in {sw.Elapsed}");
                    Console.WriteLine($"Sending solution {solution.StartRow},{solution.X.Length}");
                    Transport<Solution>.Send(stream, solution);
                    Console.WriteLine($"Sent solution {solution.StartRow},{solution.X.Length}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected");
            }
        }

    }
}
