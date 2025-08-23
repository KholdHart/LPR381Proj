public interface ISolver
{
    Solution Solve(LPModel model);
}


public class PrimalSimplexSolver : ISolver
{
    public Solution Solve(LPModel model)
    {
        var solution = new Solution();

        int numVariables = model.Coefficients.Count;
        int numConstraints = model.Constraints.Count;

        double[] obj = model.Coefficients.Select(x => x.Sign == "+" ? x.Value : x.Value).ToArray(); //reading in the objective function coefficients 
        double[] con = model.Constraints[0].Coefficients.Select(x => x.Sign == "+" ? x.Value : x.Value).ToArray();//reading in the constraint function coefficients
        double RHS = model.Constraints[0].RightHandSide;//finding the right hand side
        DisplayCanonicalForm(obj, con, RHS);

        double[,] tableau = new double[numConstraints + 1, numVariables + numConstraints + 1];

        
        for (int j = 0; j < numVariables; j++)
        {
            tableau[0, j] = model.Coefficients[j].Value; 
        }

        // Populate the constraints
        for (int i = 0; i < numConstraints; i++)
        {
            var constraint = model.Constraints[i];
           
            for (int j = 0; j < constraint.Coefficients.Count; j++)
            {
               
                tableau[i + 1, j] = constraint.Coefficients[j].Value;
               // Console.WriteLine(tableau[i + 1, j]);
            }
            tableau[i + 1, numVariables + i] = 1; // Add slack variable
            tableau[i + 1, tableau.GetLength(1) - 1] = constraint.RightHandSide;
        }

        // Main Loop
        while (true)
        {
            
            int pivotColumn = FindPivotColumn(tableau);
            if (pivotColumn == -1) break; 

            
            int pivotRow = FindPivotRow(tableau, pivotColumn);
            if (pivotRow == -1) throw new InvalidOperationException("Linear program is unbounded.");

            
            Pivot(tableau, pivotRow, pivotColumn);
        }

        // Extract solution
        ExtractSolution(tableau, numVariables, numConstraints, solution);

        return solution;
    }

    static void DisplayCanonicalForm(double[] objValue, double[] conValue, double rhs)
    {
        Console.WriteLine("Canonical Form of the Linear Program:");
        Console.Write("Objective Function: Maximize Z = ");
        for (int i = 0; i < objValue.Length; i++)
        {
            if (i > 0 && objValue[i] >= 0)
                Console.Write(" + ");
            Console.Write($"{objValue[i]} x{i + 1}");
        }
        Console.WriteLine();

        Console.WriteLine("Subject to:");
        Console.Write("   ");
        for (int i = 0; i < conValue.Length; i++)
        {
            if (i > 0 && conValue[i] >= 0)
                Console.Write(" + ");
            Console.Write($"{conValue[i]} x{i + 1}");
        }
        Console.WriteLine($" <= {rhs}");
        Console.WriteLine("============================================================");
    }

    private int FindPivotColumn(double[,] tableau)
    {
        int numCols = tableau.GetLength(1);
        int pivotColumn = -1;
        double maxValue = 0;

        for (int j = 0; j < numCols - 1; j++)
        {
            if (tableau[0, j] > maxValue)
            {
                maxValue = tableau[0, j];
                pivotColumn = j;
            }
        }

        return pivotColumn;
    }

    private int FindPivotRow(double[,] tableau, int pivotColumn)
    {
        int numRows = tableau.GetLength(0);
        int pivotRow = -1;
        double minRatio = double.MaxValue;

        for (int i = 1; i < numRows; i++)
        {
            if (tableau[i, pivotColumn] > 0)
            {
                double ratio = tableau[i, tableau.GetLength(1) - 1] / tableau[i, pivotColumn];
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
        int numRows = tableau.GetLength(0);
        int numCols = tableau.GetLength(1);

        double pivotValue = tableau[pivotRow, pivotColumn];

        // Normalize pivot row
        for (int j = 0; j < numCols; j++)
        {
            tableau[pivotRow, j] /= pivotValue;
        }

        // Zero out pivot column
        for (int i = 0; i < numRows; i++)
        {
            if (i == pivotRow) continue;
            double factor = tableau[i, pivotColumn];
            for (int j = 0; j < numCols; j++)
            {
                tableau[i, j] -= factor * tableau[pivotRow, j];
            }
        }
    }

    private void ExtractSolution(double[,] tableau, int numVariables, int numConstraints, Solution solution)
    {
        // Extract the optimal values from the tableau
        for (int i = 0; i < numVariables; i++)
        {
            double value = 0;
            for (int j = 0; j < numConstraints; j++)
            {
                if (tableau[j + 1, i] != 0)
                {
                    value = tableau[j + 1, tableau.GetLength(1) - 1];
                }
            }
            solution.OptimalValues.Add(value);
        }

        // Extract the objective value
        solution.ObjectiveValue = tableau[0, tableau.GetLength(1) - 1];
    }
}


public class RevPrimalSimplexSolver : ISolver
{
    public Solution Solve(LPModel model)
    {
        // Implement your algorithm here
        var solution = new Solution();
        // Make Simplex iterations and fill the solution object
        return solution;
    }
}
