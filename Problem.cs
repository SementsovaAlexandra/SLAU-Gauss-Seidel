using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Curs1
{
    internal class Problem
    {
        public double[,] A { get; set; }
        public double[] B { get; set; }
        public double Eps { get; set; }
        public int StartRow { get; set; }
        public int RowCount { get; set; }

        public Problem(double[,] a, double[] b, double eps)
        {
            A = a;
            B = b;
            Eps = eps;
        }

        public static Problem Random(int n, double eps)
        {
            var random = new Random();
            var a = new double[n, n];
            var b = new double[n];
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    a[r, c] = (double)(random.Next(-20, 21));
                }
                b[r] = (double)(random.Next(-20, 21));
            }
            // Обеспечим диагональное доминирование
            for (int r = 0; r < n; r++)
            {
                double sum = 0;
                for (int c = 0; c < n; c++)
                {
                    if (c != r)
                    {
                        sum += Math.Abs(a[r, c]);
                    }
                }
                a[r, r] = sum + (double)(random.Next(1, 10));
            }

            return new Problem(a, b, eps);
        }

        public Problem Partial(int startRow, int rowCount)
        {
            var problem = new Problem(A, B, Eps);
            problem.StartRow = startRow;
            problem.RowCount = rowCount;
            return problem;
        }

        public bool IsDiagonallyDominant()
        {
            var n = A.GetLength(0);

            for (int i = 0; i < n; i++)
            {
                double diagElement = Math.Abs(A[i, i]);
                double sumOthers = 0;

                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        sumOthers += Math.Abs(A[i, j]);
                    }
                }

                if (diagElement <= sumOthers)
                {
                    return false;
                }
            }

            return true;
        }

        public Solution Solve(Solution xs)
        {
            int n = B.Length;

            var solution = new Solution(StartRow, RowCount);

            Parallel.For(StartRow, StartRow+RowCount, i =>
            {
                double sigma = 0.0;

                for (int j = 0; j < n; j++)
                {
                    if (j != i)
                    {
                        sigma += A[i, j] * xs.X[j];
                    }
                }

                double xNew = (B[i] - sigma) / A[i, i];

                lock (solution)
                {
                    solution.Err[i - StartRow] = Math.Abs(xNew - xs.X[i]);
                    solution.X[i - StartRow] = xNew;
                }
            });

            return solution;
        }
    }
}
