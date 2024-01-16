using System;
using System.Collections.Generic;

namespace OpenCVForUnityExample.MOT.ByteTrack
{
    public static class Lapjv
    {
        private const int LARGE = 1000000;

        private enum FpType
        {
            FP_1 = 1,
            FP_2 = 2,
            FP_DYNAMIC = 3
        }

        /** Column-reduction and reduction transfer for a dense cost matrix.
         */
        private static int CcrrtDense(int n, List<float> cost, List<int> free_rows,
                                 List<int> x, List<int> y, List<double> v)
        {
            int n_free_rows;
            List<bool> unique = new List<bool>(new bool[n]);

            for (int i = 0; i < n; i++)
            {
                x[i] = -1;
                v[i] = LARGE;
                y[i] = 0;
            }

            for (int i = 0; i < n; i++)
            {
                for (int jj = 0; jj < n; jj++)
                {
                    double c = cost[i * n + jj];
                    if (c < v[jj])
                    {
                        v[jj] = c;
                        y[jj] = i;
                    }
                }
            }

            int j = n;
            do
            {
                j--;
                int i = y[j];
                if (x[i] < 0)
                {
                    x[i] = j;
                }
                else
                {
                    unique[i] = false;
                    y[j] = -1;
                }
            } while (j > 0);

            n_free_rows = 0;
            for (int i = 0; i < n; i++)
            {
                if (x[i] < 0)
                {
                    free_rows[n_free_rows++] = i;
                }
                else if (unique[i])
                {
                    int jVal = x[i];
                    double min = LARGE;
                    for (int j2 = 0; j2 < n; j2++)
                    {
                        if (j2 == jVal)
                        {
                            continue;
                        }
                        double c = cost[i * n + j2] - v[j2];
                        if (c < min)
                        {
                            min = c;
                        }
                    }
                    v[jVal] -= min;
                }
            }
            return n_free_rows;
        }

        /** Augmenting row reduction for a dense cost matrix.
         */
        private static int CarrDense(int n, List<float> cost, int n_free_rows,
                          List<int> free_rows, List<int> x, List<int> y,
                          List<double> v)
        {
            int current = 0;
            int new_free_rows = 0;
            int rr_cnt = 0;

            while (current < n_free_rows)
            {
                int i0;
                int j1, j2;
                double v1, v2, v1_new;
                bool v1_lowers;

                rr_cnt++;
                int free_i = free_rows[current++];
                j1 = 0;
                v1 = cost[free_i * n] - v[0];
                j2 = -1;
                v2 = LARGE;

                for (int j = 1; j < n; j++)
                {
                    double c = cost[free_i * n + j] - v[j];
                    if (c < v2)
                    {
                        if (c >= v1)
                        {
                            v2 = c;
                            j2 = j;
                        }
                        else
                        {
                            v2 = v1;
                            v1 = c;
                            j2 = j1;
                            j1 = j;
                        }
                    }
                }

                i0 = y[j1];
                v1_new = v[j1] - (v2 - v1);
                v1_lowers = v1_new < v[j1];

                if (rr_cnt < current * n)
                {
                    if (v1_lowers)
                    {
                        v[j1] = v1_new;
                    }
                    else if (i0 >= 0 && j2 >= 0)
                    {
                        j1 = j2;
                        i0 = y[j2];
                    }

                    if (i0 >= 0)
                    {
                        if (v1_lowers)
                        {
                            free_rows[--current] = i0;
                        }
                        else
                        {
                            free_rows[new_free_rows++] = i0;
                        }
                    }
                }
                else
                {
                    if (i0 >= 0)
                    {
                        free_rows[new_free_rows++] = i0;
                    }
                }

                x[free_i] = j1;
                y[j1] = free_i;
            }

            return new_free_rows;
        }

        /** Find columns with minimum d[j] and put them on the SCAN list.
         */
        private static int FindDense(int n, int lo, List<double> d, List<int> cols)
        {
            int hi = lo + 1;
            double mind = d[cols[lo]];

            for (int k = hi; k < n; k++)
            {
                int j = cols[k];
                if (d[j] <= mind)
                {
                    if (d[j] < mind)
                    {
                        hi = lo;
                        mind = d[j];
                    }
                    cols[k] = cols[hi];
                    cols[hi++] = j;
                }
            }

            return hi;
        }

        // Scan all columns in TODO starting from arbitrary column in SCAN
        // and try to decrease d of the TODO columns using the SCAN column.
        private static int ScanDense(int n, List<float> cost, ref int plo, ref int phi,
                          List<double> d, List<int> cols, List<int> pred,
                          List<int> y, List<double> v)
        {
            int lo = plo;
            int hi = phi;
            double h, cred_ij;

            while (lo != hi)
            {
                int j = cols[lo++];
                int i = y[j];
                double mind = d[j];
                h = cost[i * n + j] - v[j] - mind;
                // For all columns in TODO
                for (int k = hi; k < n; k++)
                {
                    j = cols[k];
                    cred_ij = cost[i * n + j] - v[j] - h;
                    if (cred_ij < d[j])
                    {
                        d[j] = cred_ij;
                        pred[j] = i;
                        if (cred_ij == mind)
                        {
                            if (y[j] < 0)
                            {
                                return j;
                            }
                            cols[k] = cols[hi];
                            cols[hi++] = j;
                        }
                    }
                }
            }

            plo = lo;
            phi = hi;
            return -1;
        }

        /** Single iteration of modified Dijkstra shortest path algorithm as explained
         * in the JV paper.
         *
         * This is a dense matrix version.
         *
         * \return The closest free column index.
         */
        private static int FindPathDense(int n, List<float> cost, int start_i,
                              List<int> y, List<double> v, List<int> pred)
        {
            int lo = 0, hi = 0;
            int final_j = -1;
            int n_ready = 0;
            List<int> cols = new List<int>(new int[n]);
            List<double> d = new List<double>(new double[n]);

            for (int i = 0; i < n; i++)
            {
                cols[i] = i;
                pred[i] = start_i;
                d[i] = cost[start_i * n + i] - v[i];
            }

            while (final_j == -1)
            {
                // No columns left on the SCAN list.
                if (lo == hi)
                {
                    n_ready = lo;
                    hi = FindDense(n, lo, d, cols);
                    for (int k = lo; k < hi; k++)
                    {
                        int j = cols[k];
                        if (y[j] < 0)
                        {
                            final_j = j;
                        }
                    }
                }
                if (final_j == -1)
                {
                    final_j = ScanDense(n, cost, ref lo, ref hi, d, cols, pred, y, v);
                }
            }

            double mind = d[cols[lo]];
            for (int k = 0; k < n_ready; k++)
            {
                int j = cols[k];
                v[j] += d[j] - mind;
            }

            return final_j;
        }

        /** Augment for a dense cost matrix.
         */
        private static int CaDense(int n, List<float> cost, int n_free_rows,
                        List<int> free_rows, List<int> x, List<int> y,
                        List<double> v)
        {
            List<int> pred = new List<int>(new int[n]);

            for (int row_n = 0; row_n < n_free_rows; ++row_n)
            {
                int free_row = free_rows[row_n];
                int i = -1;
                int k = 0;

                int j = FindPathDense(n, cost, free_row, y, v, pred);
                if (j < 0)
                {
                    throw new Exception("Error occured in _ca_dense(): j < 0");
                }
                if (j >= n)
                {
                    throw new Exception("Error occured in _ca_dense(): j >= n");
                }
                while (i != free_row)
                {
                    i = pred[j];
                    y[j] = i;
                    int temp = x[i];
                    x[i] = j;
                    j = temp;
                    k++;
                    if (k >= n)
                    {
                        throw new Exception("Error occured in _ca_dense(): k >= n");
                    }
                }
            }
            return 0;
        }

        /** Solve dense sparse LAP. */
        public static int LapjvInternal(int n, List<float> cost, List<int> x, List<int> y)
        {
            int ret;
            List<int> free_rows = new List<int>(new int[n]);
            List<double> v = new List<double>(new double[n]);

            ret = CcrrtDense(n, cost, free_rows, x, y, v);
            int i = 0;
            while (ret > 0 && i < 2)
            {
                ret = CarrDense(n, cost, ret, free_rows, x, y, v);
                i++;
            }
            if (ret > 0)
            {
                ret = CaDense(n, cost, ret, free_rows, x, y, v);
            }

            return ret;
        }
    }
}
