using LinearProgrammingProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PrimalSimplexSolver
{
    public void Solve(LinearProgrammingModel model)
    {
        if (!model.IsValid())
            throw new InvalidOperationException("Invalid Linear Programming Model.");

        int numVariables = model.VariableCount;
        int numConstraints = model.ConstraintCount;

        // Adjust for Minimize (convert to Max by multiplying by -1)
        double senseFactor = model.ObjectiveType == ObjectiveType.Maximize ? 1.0 : -1.0;

        // Count slack columns (only for <= constraints)
        int slackCount = model.Constraints.Count(c => c.Type == ConstraintType.LessThanOrEqual);
        int cols = numVariables + slackCount + 1; // +1 for RHS
        int rows = numConstraints + 1; // +1 for Z row

        double[,] tableau = new double[rows, cols];

        // Build Z-row: Z - (c1)x1 - (c2)x2 ... = 0
        for (int j = 0; j < numVariables; j++)
            tableau[0, j] = -senseFactor * model.ObjectiveCoefficients[j];

        // Fill constraints rows
        int slackIndex = numVariables;
        for (int i = 0; i < numConstraints; i++)
        {
            var constraint = model.Constraints[i];

            // Variable coefficients
            for (int j = 0; j < numVariables; j++)
                tableau[i + 1, j] = constraint.Coefficients[j];

            // Slack variable if <=
            if (constraint.Type == ConstraintType.LessThanOrEqual)
            {
                tableau[i + 1, slackIndex++] = 1;
            }
            else
            {
                // No Big-M implemented for >= or = constraints
                throw new InvalidOperationException("Only <= constraints are supported for now.");
            }

            // RHS
            tableau[i + 1, cols - 1] = constraint.RightHandSide;
        }

        // Display canonical form
        Console.WriteLine("=== Canonical Form ===");
        Console.WriteLine(model.GetObjectiveFunctionString());
        foreach (var line in model.GetConstraintStrings())
            Console.WriteLine(line);
        Console.WriteLine();

        int iteration = 0;
        PrintTableau(tableau, iteration);

        // Main simplex loop
        while (true)
        {
            int pivotColumn = FindPivotColumn(tableau);
            if (pivotColumn == -1)
            {
                // Optimal solution found
                model.Status = SolutionStatus.Optimal;
                break;
            }

            int pivotRow = FindPivotRow(tableau, pivotColumn);
            if (pivotRow == -1)
            {
                model.Status = SolutionStatus.Unbounded;
                return;
            }

            Pivot(tableau, pivotRow, pivotColumn);
            PrintTableau(tableau, ++iteration);
        }

        // Extract solution and update model
        ExtractSolution(model, tableau, numVariables, numConstraints);
    }

    private void PrintTableau(double[,] tableau, int iteration)
    {
        Console.WriteLine($"=== Iteration {iteration} ===");
        for (int i = 0; i < tableau.GetLength(0); i++)
        {
            for (int j = 0; j < tableau.GetLength(1); j++)
            {
                Console.Write($"{Math.Round(tableau[i, j], 3),8}");
            }
            Console.WriteLine();
        }
        Console.WriteLine("----------------------------");
    }

    private int FindPivotColumn(double[,] tableau)
    {
        int cols = tableau.GetLength(1) - 1; // exclude RHS
        int pivotCol = -1;
        double maxValue = 0;

        for (int j = 0; j < cols; j++)
        {
            if (tableau[0, j] > maxValue)
            {
                maxValue = tableau[0, j];
                pivotCol = j;
            }
        }
        return pivotCol;
    }

    private int FindPivotRow(double[,] tableau, int pivotColumn)
    {
        int rows = tableau.GetLength(0);
        int rhsCol = tableau.GetLength(1) - 1;

        int pivotRow = -1;
        double minRatio = double.MaxValue;

        for (int i = 1; i < rows; i++)
        {
            double colValue = tableau[i, pivotColumn];
            if (colValue > 0)
            {
                double ratio = tableau[i, rhsCol] / colValue;
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    pivotRow = i;
                }
            }
        }
        return pivotRow;
    }

    private void Pivot(double[,] tableau, int pivotRow, int pivotColumn)
    {
        int rows = tableau.GetLength(0);
        int cols = tableau.GetLength(1);

        double pivotValue = tableau[pivotRow, pivotColumn];

        // Normalize pivot row
        for (int j = 0; j < cols; j++)
            tableau[pivotRow, j] /= pivotValue;

        // Zero out other rows in pivot column
        for (int i = 0; i < rows; i++)
        {
            if (i == pivotRow) continue;
            double factor = tableau[i, pivotColumn];
            for (int j = 0; j < cols; j++)
            {
                tableau[i, j] -= factor * tableau[pivotRow, j];
            }
        }
    }

    private void ExtractSolution(LinearProgrammingModel model, double[,] tableau, int numVariables, int numConstraints)
    {
        int rhsCol = tableau.GetLength(1) - 1;
        model.OptimalSolution.Clear();

        // Assign values to decision variables
        for (int j = 0; j < numVariables; j++)
        {
            int rowWithOne = -1;
            bool isUnitColumn = true;

            for (int i = 1; i <= numConstraints; i++)
            {
                if (Math.Abs(tableau[i, j] - 1) < 1e-9)
                {
                    if (rowWithOne == -1) rowWithOne = i;
                    else { isUnitColumn = false; break; }
                }
                else if (Math.Abs(tableau[i, j]) > 1e-9)
                {
                    isUnitColumn = false; break;
                }
            }

            double value = (isUnitColumn && rowWithOne != -1) ? tableau[rowWithOne, rhsCol] : 0;
            model.OptimalSolution[model.Variables[j].Name] = value;
        }

        model.OptimalValue = tableau[0, rhsCol];
    }
}
