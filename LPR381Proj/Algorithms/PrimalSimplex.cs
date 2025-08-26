using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Solver
{
    /// <summary>
    /// A straightforward implementation of the Primal Simplex algorithm that follows the lecture steps:
    ///  - Convert to primal-simplex standard form (add slack variables for ≤ constraints; z - c^T x = 0)
    ///  - Build the initial tableau with slacks basic
    ///  - Optimality check on the z-row (most negative entering column for Max)
    ///  - Ratio test to select pivot row (smallest positive RHS / a_ij)
    ///  - Pivot (normalize pivot row; eliminate column elsewhere)
    ///  - Iterate until no negatives remain in z-row
    ///
    /// LIMITATIONS (matching the lecture scope):
    ///  - Supports models that are feasible at the origin after adding slacks: all variables continuous, x ≥ 0
    ///  - All constraints must be of type ≤ with nonnegative RHS
    ///  - Maximization is native; minimization is handled by multiplying the objective by -1
    ///  - No artificial variables (no Big-M / Two-Phase). For ≥ or = constraints, or negative RHS, add a preprocessor first.
    /// </summary>
    public class PrimalSimplexSolver
    {
        public class SimplexOptions
        {
            public double Tolerance { get; set; } = 1e-9;
            public int MaxIterations { get; set; } = 10_000;
        }

        public class SimplexReport
        {
            public int Iterations { get; set; }
            public string[] ColumnLabels { get; set; } = Array.Empty<string>();
            public string[] RowLabels { get; set; } = Array.Empty<string>();
            public double[,] FinalTableau { get; set; } = new double[0, 0];
            public List<string> Pivots { get; set; } = new List<string>();
            public List<string> IterationSnapshots { get; set; }

            public SimplexReport()
            {
                Pivots = new List<string>();
                IterationSnapshots = new List<string>();
            }
        }

        public SimplexReport Solve(LinearProgrammingModel model, SimplexOptions options = null)
        {
            if (options == null)
            {
                options = new SimplexOptions();
            }
            

            ValidateModelForPrimalSimplex(model);

            // Work on local copies to avoid mutating the original coefficients when handling minimization
            var obj = model.ObjectiveCoefficients.ToArray();
            bool isMin = model.ObjectiveType == ObjectiveType.Minimize;
            if (isMin)
            {
                for (int i = 0; i < obj.Length; i++) obj[i] = -obj[i];
            }

            int n = model.VariableCount;          // decision variables
            int m = model.ConstraintCount;        // constraints

            // Build initial tableau: rows = m + 1 (z + constraints), cols = n + m (x + slacks) + 1 (RHS)
            int cols = n + m + 1;
            int rows = m + 1;
            var T = new double[rows, cols];

            // z-row: z - c^T x = 0  ==> coefficients are -c in the z-row under x-columns
            for (int j = 0; j < n; j++)
                T[0, j] = -obj[j];
            // slack columns in z-row are 0; RHS is 0 by construction

            // Constraint rows
            for (int i = 0; i < m; i++)
            {
                var c = model.Constraints[i];
                // x coefficients
                for (int j = 0; j < n; j++)
                    T[i + 1, j] = c.Coefficients[j];
                // slack identity
                T[i + 1, n + i] = 1.0; // s_i
                // RHS
                T[i + 1, cols - 1] = c.RightHandSide;
            }

            // Labels for reporting / debugging
            var colLabels = new string[n + m + 1];
            for (int j = 0; j < n; j++) colLabels[j] = model.Variables[j].Name; // x's
            for (int j = 0; j < m; j++) colLabels[n + j] = $"s{j + 1}";          // slacks
            colLabels[colLabels.Length - 1] = "rhs";

            var rowLabels = new string[m + 1];
            rowLabels[0] = "z";
            for (int i = 0; i < m; i++) rowLabels[i + 1] = $"{i + 1}";

            // Keep track of which column is basic in which row (row->col)
            // Initially slacks are basic: row i+1 has basic column n+i
            var basicColOfRow = Enumerable.Range(0, m).Select(i => n + i).ToArray();

            var report = new SimplexReport
            {
                ColumnLabels = colLabels,
                RowLabels = rowLabels,
            };

            int iter = 0;
            SaveSnapshot(report, T, rowLabels, colLabels, iter);

            while (true)
            {
                iter++;
                if (iter > options.MaxIterations)
                    throw new InvalidOperationException("Max iterations exceeded in Simplex.");

                // 1) Optimality check (Max): any negative in z-row among non-RHS columns?
                //incorporate biggest negative variable in z row except rhs
                int entering = ArgMostNegative(T, row: 0, lastColIndex: cols - 1, options.Tolerance);
                if (entering == -1)
                {
                    // Optimal
                    break;
                }

                // 2) Ratio test for pivot row
                //incorporate dividing rhs by pivot colum to find smallest positive ratio
                int pivotRow = RatioTest(T, entering, startRow: 1, endRow: rows - 1, rhsCol: cols - 1, options.Tolerance);
                if (pivotRow == -1)
                {
                    model.Status = SolutionStatus.Unbounded;
                    return FinalizeUnboundedReport(model, report, T, iter);
                }

                // Record pivot in human-readable form
                string enteringLabel = colLabels[entering];
                string leavingLabel = colLabels[basicColOfRow[pivotRow - 1]]; // previous basic in that row
                report.Pivots.Add($"Pivot on a[{pivotRow},{enteringLabel}] — {leavingLabel} leaves, {enteringLabel} enters");

                // 3) Pivot operation
                Pivot(T, pivotRow, entering, options.Tolerance);
                basicColOfRow[pivotRow - 1] = entering;

                SaveSnapshot(report, T, rowLabels, colLabels, iter);
            }

            // Extract primal solution for the original x variables
            var x = new double[n];
            for (int j = 0; j < n; j++)
            {
                // If column j is a unit column with 1 at some row r and zeros elsewhere -> basic with value RHS[r]
                if (TryGetBasicValue(T, j, out int r, options.Tolerance))
                    x[j] = T[r, cols - 1];
                else
                    x[j] = 0.0;
            }

            double z = T[0, cols - 1];
            if (isMin) z = -z; // reverse sign back for Min problems

            // Write back into the model
            model.OptimalSolution.Clear();
            for (int j = 0; j < n; j++)
                model.OptimalSolution[model.Variables[j].Name] = x[j];
            model.OptimalValue = z;
            model.Status = SolutionStatus.Optimal;

            report.Iterations = iter - 1; // last loop broke without pivoting
            report.FinalTableau = (double[,])T.Clone();
            return report;
        }

        private static void SaveSnapshot(SimplexReport report, double[,] tableau, string[] rowLabels, string[] colLabels, int iter)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"--- Iteration {iter} ---");
            sb.Append("\t");
            foreach (var col in colLabels) sb.Append(col + "\t");
            sb.AppendLine();
            for (int i = 0; i < tableau.GetLength(0); i++)
            {
                sb.Append(rowLabels[i] + "\t");
                for (int j = 0; j < tableau.GetLength(1); j++)
                {
                    sb.Append(tableau[i, j].ToString("F2") + "\t");
                }
                sb.AppendLine();
            }
            report.IterationSnapshots.Add(sb.ToString());
        }

        private static void ValidateModelForPrimalSimplex(LinearProgrammingModel model)
        {
            if (!model.IsValid())
                throw new ArgumentException("Model is not valid: variable/coeff counts mismatch.");

            // Variables: continuous & x ≥ 0
            foreach (var v in model.Variables)
            {
                if (v.Type != VariableType.Continuous)
                    throw new NotSupportedException("Only continuous variables are supported by this primal simplex implementation.");
                if (double.IsNegativeInfinity(v.LowerBound) || v.LowerBound < 0 - 1e-12)
                    throw new NotSupportedException("All variables must satisfy x ≥ 0 for this implementation.");
            }

            // Constraints: all ≤ with RHS ≥ 0
            foreach (var c in model.Constraints)
            {
                if (c.Type != ConstraintType.LessThanOrEqual)
                    throw new NotSupportedException("Only ≤ constraints are supported (no ≥ or =). Add a preprocessing step or two‑phase/Big‑M.");
                if (c.RightHandSide < -1e-12)
                    throw new NotSupportedException("Constraint RHS must be nonnegative for this implementation.");
            }
        }

        private static int ArgMostNegative(double[,] T, int row, int lastColIndex, double tol)
        {
            int cols = T.GetLength(1);
            int entering = -1;
            double mostNeg = -tol; // must be strictly < -tol to enter
            for (int j = 0; j < cols; j++)
            {
                if (j == lastColIndex) continue; // skip RHS
                double v = T[row, j];
                if (v < mostNeg)
                {
                    mostNeg = v;
                    entering = j;
                }
            }
            return entering; // -1 means optimal (no negatives)
        }

        private static int RatioTest(double[,] T, int enteringCol, int startRow, int endRow, int rhsCol, double tol)
        {
            int pivotRow = -1;
            double bestRatio = double.PositiveInfinity;
            for (int i = startRow; i <= endRow; i++)
            {
                double a = T[i, enteringCol];
                if (a > tol)
                {
                    double rhs = T[i, rhsCol];
                    double ratio = rhs / a;
                    if (ratio >= 0 && ratio < bestRatio - 1e-15)
                    {
                        bestRatio = ratio;
                        pivotRow = i;
                    }
                }
            }
            return pivotRow; // -1 => unbounded
        }

        private static void Pivot(double[,] T, int pivotRow, int pivotCol, double tol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            double p = T[pivotRow, pivotCol];
            if (Math.Abs(p) < tol) throw new InvalidOperationException("Pivot element too small.");

            // Normalize pivot row
            for (int j = 0; j < cols; j++)
                T[pivotRow, j] /= p;

            // Eliminate pivot column in all other rows
            for (int i = 0; i < rows; i++)
            {
                if (i == pivotRow) continue;
                double factor = T[i, pivotCol];
                if (Math.Abs(factor) < tol) continue;
                for (int j = 0; j < cols; j++)
                    T[i, j] -= factor * T[pivotRow, j];
            }
        }

        private static bool TryGetBasicValue(double[,] T, int col, out int row, double tol)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);

            row = -1;
            int countOnes = 0;
            for (int i = 0; i < rows; i++)
            {
                double v = T[i, col];
                if (Math.Abs(v - 1.0) <= 1e-9)
                {
                    countOnes++;
                    row = i;
                }
                else if (Math.Abs(v) > 1e-9)
                {
                    // Non-zero entry other than the unit => not basic
                    return false;
                }
            }
            // A basic column should have exactly one 1 and all other entries 0
            return countOnes == 1;
        }

        private static SimplexReport FinalizeUnboundedReport(LinearProgrammingModel model, SimplexReport report, double[,] T, int iterations)
        {
            report.Iterations = iterations;
            report.FinalTableau = (double[,])T.Clone();
            return report;
        }
    }
}
