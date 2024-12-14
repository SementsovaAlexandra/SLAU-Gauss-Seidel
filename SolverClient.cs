using System.Net.Sockets;

namespace Curs1
{
    internal class SolverClient
    {
        TcpClient client;
        NetworkStream stream;
        Problem problem;

        public SolverClient(string addr, int port, Problem problem)
        {
            client = new TcpClient(addr, port);
            stream = client.GetStream();
            this.problem = problem;
        }

        ~SolverClient()
        {
            stream.Close();
            client.Close();
        }

        public void SendProblem()
        {
            Console.WriteLine($"Sending problem {problem.StartRow},{problem.RowCount}/{problem.B.Length}");
            Transport<Problem>.Send(stream, problem);
        }

        public Solution SolveIteration(Solution xs)
        {
            Console.WriteLine($"Sending iteration task");
            Transport<Solution>.Send(stream, xs);
            Console.WriteLine($"Reading iteration solution");
            var solution = Transport<Solution>.Receive(stream);
            Console.WriteLine($"Read solution {solution.StartRow},{solution.X.Length}");
            return solution;
        }
    }
}
