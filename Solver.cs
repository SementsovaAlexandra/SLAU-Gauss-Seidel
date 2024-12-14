namespace Curs1
{
    internal class Solver
    {
        private List<SolverClient> SolverClients = new List<SolverClient>();
        private List<(string, int)> WorkerAddrs;
        private int iteration = 0;
        private Problem? problem;

        public Solver(string workersFileName)
        {
            WorkerAddrs = new List<(string, int)>();
            string[] lines = File.ReadAllLines(workersFileName);
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                {
                    WorkerAddrs.Add((parts[0], port));
                }
            }
            if (WorkerAddrs.Count < 1)
            {
                throw new Exception("Insufficient workers");
            }
        }

        ~Solver() {
            SolverClients.Clear();
        }

        public void Init(Problem problem)
        {
            this.problem = problem;
            iteration = 0;
            var partSize = problem.B.Length / WorkerAddrs.Count;
            int i = 0;
            foreach (var w in WorkerAddrs)
            {
                int startRow = i * partSize;
                int rowCount = i == WorkerAddrs.Count-1? problem.B.Length - startRow : partSize;
                var partialProblem = problem.Partial(startRow, rowCount);
                var client = new SolverClient(w.Item1, w.Item2, partialProblem);
                SolverClients.Add(client);
                i++;
            }
        }

        public Solution NextIteration(Solution xs)
        {
            var threads = new List<Thread>();
            var solutions = new Solution[SolverClients.Count];

            int i = 0;
            foreach (var client in SolverClients)
            {
                var ind = i;
                var cl = client;
                var thread = new Thread(() =>
                {
                    if (iteration == 0)
                        cl.SendProblem();
                    var solution = cl.SolveIteration(xs);
                    lock(solutions)
                    {
                        solutions[ind] = solution;
                    }
                });
                threads.Add(thread);
                thread.Start();
                i++;
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            iteration++;
            return Solution.Combined(solutions);
        }
    }
}
