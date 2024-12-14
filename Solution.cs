using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Curs1
{
    internal class Solution
    {
        public double[] X;
        public double[] Err;
        public int StartRow { get; set; } = 0;

        public Solution(int startRow, int rowCount)
        {
            X = new double[rowCount];
            Err = new double[rowCount];
            StartRow = startRow;
        }

        public bool IsAcceptable(double eps)
        {
            foreach (var err in Err)
            {
                if (err > eps)
                    return false;
            }
            return true;
        }

        public static Solution Combined(Solution[] solutions)
        {
            int n = 0;
            foreach (var s in solutions)
            {
                n += s.X.Length;
            }
            var res = new Solution(0, n);

            foreach (var s in solutions)
            {
                Array.Copy(s.X, 0, res.X, s.StartRow, s.X.Length);
                Array.Copy(s.Err, 0, res.Err, s.StartRow, s.Err.Length);
            }

            return res;
        }
    }
}
