using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Curs1
{
    internal static class Program
    {
        const string workersFile = "workers";

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0) {
                ModeUI();
                return;
            }

            AttachConsole(-1);
            Console.WriteLine();
            Console.WriteLine("=== Curs1 ===");

            var mode = args[0];
            switch (mode)
            {
                default:
                    Usage();
                    return;
                case "worker":
                    if (args.Length < 2)
                    {
                        Usage();
                        Console.WriteLine("В режиме worker требуется аргумент port");
                        return;
                    }
                    if (int.TryParse(args[1], out int port))
                    {
                        ModeWorker(port);
                    }
                    Console.WriteLine("Неверное значение аргумента port");
                    return;
                case "file":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("В режиме file требуется аргумент fileName");
                        return;
                    }
                    ModeFile(args[1]);
                    return;
                case "generate":
                    if (args.Length < 3 || !int.TryParse(args[2], out int size))
                    {
                        Console.WriteLine("В режиме generate требуются аргументы fileName и size");
                        return;
                    }
                    ModeGenerate(args[1], size);
                    return;
            }
        }

        static void ModeUI()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void ModeWorker(int port)
        {
            SolverServer.Serve(port);
        }

        static void ModeFile(string fileName)
        {
            Problem problem;
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                problem = Transport<Problem>.ReceiveStream(fileStream, false);
            }

            try
            {
                var solver = new Solver(workersFile);
                solver.Init(problem);
                var solved = false;
                var xs = new Solution(0, problem.B.Length);
                var iter = 0;
                var sw = Stopwatch.StartNew();
                while (!solved)
                {
                    iter++;
                    Console.WriteLine($"Iteration {iter}");
                    xs = solver.NextIteration(xs);
                    solved = xs.IsAcceptable(problem.Eps);
                    
                    Console.WriteLine($"Iteration error: {xs.Err.Max()}");
                }
                sw.Stop();
                Console.WriteLine($"Solution found in {sw.Elapsed}");
                using (FileStream fileStream = new FileStream(RemoveExtension(fileName) + "-solution.json", FileMode.Create, FileAccess.Write))
                {
                    Transport<Solution>.SendStream(fileStream, xs, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        static void ModeGenerate(string fileName, int size)
        {
            var problem = Problem.Random(size, 0.001);
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Transport<Problem>.SendStream(fileStream, problem, false);
            }
        }

        static void Usage()
        {
            Console.WriteLine("Запуск без параметров: интерактивный режим");
            Console.WriteLine("curs1.exe worker 9000: запуск в режиме worker на порту 9000");
            Console.WriteLine("curs1.exe file problem.json: запуск в режиме чтения задания из файлов");
            Console.WriteLine("curs1.exe generate problem.json 20: сгенерировать файл problem.json с размером матрицы 20x20");
        }

        public static string RemoveExtension(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            if (directory is null)
                return fileNameWithoutExtension;
            return Path.Combine(directory, fileNameWithoutExtension);
        }
    }
}
