using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.IO
{
    public class OutputWriter
    {
        private string _outputFilePath;
        private StringBuilder _outputContent = new StringBuilder();

        public OutputWriter(string outputFilePath)
        {
            _outputFilePath = outputFilePath;
            string dir = Path.GetDirectoryName(_outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void WriteCanonicalForm(double[,] tableau, List<string> basicVars, List<string> nonBasicVars)
        {
            AddLine("CANONICAL FORM");
            int rows = tableau.GetLength(0), cols = tableau.GetLength(1);
            var header = new StringBuilder("BasicVar".PadRight(10));
            foreach (var v in nonBasicVars) header.Append(v.PadLeft(8));
            header.Append("RHS".PadLeft(10));
            AddLine(header.ToString());
            for (int i = 0; i < rows - 1; i++)
            {
                var row = new StringBuilder();
                row.Append((i < basicVars.Count ? basicVars[i] : $"s{i + 1}").PadRight(10));
                for (int j = 0; j < cols - 1; j++)
                    row.Append(Math.Round(tableau[i, j], 3).ToString("F3").PadLeft(8));
                row.Append(Math.Round(tableau[i, cols - 1], 3).ToString("F3").PadLeft(10));
                AddLine(row.ToString());
            }
            AddLine();
        }

        public void WriteIteration(int iter, double[,] tableau, List<string> basicVars, int pivotRow = -1, int pivotCol = -1, bool isOptimal = false)
        {
            AddLine($"ITERATION {iter}");
            int rows = tableau.GetLength(0), cols = tableau.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                var row = new StringBuilder();
                row.Append((i < basicVars.Count ? basicVars[i] : "Z").PadRight(10));
                for (int j = 0; j < cols; j++)
                    row.Append(Math.Round(tableau[i, j], 3).ToString("F3").PadLeft(8));
                AddLine(row.ToString());
            }
            if (isOptimal) AddLine("*** OPTIMAL SOLUTION REACHED ***");
            AddLine();
        }

        public void WriteSolution(LinearProgrammingModel model, string algorithm, int iterCount)
        {
            AddLine("SOLUTION RESULTS");
            AddLine($"Algorithm: {algorithm}");
            AddLine($"Iterations: {iterCount}");
            AddLine($"Status: {model.Status}");
            AddLine($"Optimal Value: {Math.Round(model.OptimalValue, 3):F3}");
            AddLine("Variable Values:");
            foreach (var v in model.Variables)
                AddLine($"  {v.Name} = {Math.Round(v.Value, 3):F3}");
            AddLine();
        }

        private void AddLine(string line = "") { _outputContent.AppendLine(line); }

        public void SaveToFile()
        {
            File.WriteAllText(_outputFilePath, _outputContent.ToString());
        }
    }
}