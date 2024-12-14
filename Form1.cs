using System.ComponentModel;
using System.Data;

namespace Curs1
{
    public partial class Form1 : Form
    {
        const string workersFile = "workers";

        private int n;
        private DataTable? dataA;
        private DataTable? dataB;
        private DataTable? dataSolution;
        private double eps;
        private BackgroundWorker backgroundWorker;

        public Form1()
        {
            InitializeComponent();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
        }

        private void NewMatrix(int size)
        {
            n = size;
            dataA = new DataTable();
            dataB = new DataTable();
            dataSolution = new DataTable();

            for (int c = 0; c < n; c++)
            {
                dataA.Columns.Add("");
                dataSolution.Columns.Add("");
            }

            dataB.Columns.Add("");

            for (int r = 0; r < n; r++)
            {
                var rowA = dataA.NewRow();
                for (int c = 0; c < n; c++)
                {
                    rowA[c] = (double)0;
                }
                dataA.Rows.Add(rowA);

                var rowB = dataB.NewRow();
                rowB[0] = (double)0;
                dataB.Rows.Add(rowB);
            }


            var rowX = dataSolution.NewRow();
            var rowE = dataSolution.NewRow();
            for (int c = 0; c < n; c++)
            {
                rowX[c] = (double)0;
                rowE[c] = double.PositiveInfinity;
            }
            dataSolution.Rows.Add(rowX);
            dataSolution.Rows.Add(rowE);

            viewA.DataSource = dataA;
            viewB.DataSource = dataB;
            viewSolution.DataSource = dataSolution;

            for (int c = 0; c < n; c++)
            {
                viewSolution.Rows[1].Cells[c].Style.BackColor = Color.LightCoral;
            }

            startButton.Enabled = true;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            NewMatrix((int)inputN.Value);
        }

        private void randomButton_Click(object sender, EventArgs e)
        {
            NewMatrix((int)inputN.Value);
            UpdateProblem(Problem.Random((int)inputN.Value, 0.001));
        }

        private Problem FillProblem()
        {
            var a = new double[n, n];
            var b = new double[n];
            double e;
            if (!double.TryParse(inputEps.Text, out e))
            {
                e = 0.001;
            }
            eps = e;
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    a[r, c] = GetDouble(dataA!.Rows[r][c]);
                }
                b[r] = GetDouble(dataB!.Rows[r][0]);
            }

            return new Problem(a, b, eps);
        }

        private void UpdateProblem(Problem problem)
        {
            for (int r = 0; r < problem.B.Length; r++)
            {
                dataB!.Rows[r][0] = problem.B[r];
                for (int c = 0; c < problem.B.Length; c++)
                {
                    dataA!.Rows[r][c] = problem.A[r,c];
                }
                inputEps.Text = problem.Eps.ToString();
            }
        }

        private void UpdateSolution(Solution solution)
        {
            for (int r = 0; r < solution.X.Length; r++)
            {
                var ind = r + solution.StartRow;
                var x = solution.X[ind];
                var err = solution.Err[ind];
                dataSolution!.Rows[0][ind] = x.ToString("F10");
                dataSolution!.Rows[1][ind] = err.ToString("F10");
                viewSolution.Rows[1].Cells[ind].Style.BackColor = 
                    err <= eps? Color.LightGreen : Color.LightCoral;
            }
        }

        static private double GetDouble(object? cell)
        {
            if (cell is null)
                return 0;
            if (cell is double)
                return (double)cell;

            double res;
            if (!double.TryParse(cell as string, out res))
                return 0;

            return res;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
                backgroundWorker.CancelAsync();
            else
            {
                var problem = FillProblem();
                if (!problem.IsDiagonallyDominant())
                {
                    MessageBox.Show("Матрица не является диагонально доминантной, решение не применимо");
                    return;
                }
                backgroundWorker.RunWorkerAsync(problem);
            }
        }

        private void inputEps_TextChanged(object sender, EventArgs e)
        {
            double eps;
            if (!double.TryParse(inputEps.Text, out eps))
            {
                inputEps.Text = "0.001";
            }

        }

        private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (sender as BackgroundWorker)!;

            try
            {
                var problem = (e.Argument as Problem)!;
                var solver = new Solver(workersFile);
                solver.Init(problem);
                var solved = false;
                var xs = new Solution(0, problem.B.Length);
                while (!worker.CancellationPending && !solved)
                {
                    xs = solver.NextIteration(xs);
                    solved = xs.IsAcceptable(problem.Eps);
                    worker.ReportProgress(0, xs);
                }
            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            var xs = (Solution)e.UserState!;
            UpdateSolution(xs);
        }
    }
}
