using System;
using System.Collections.Generic;
using System.IO;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.IO
{
    public class InputReader
    {
        private string _filePath;
        public InputReader(string filePath) { _filePath = filePath; }

        public LinearProgrammingModel ReadModel()
        {
            if (!File.Exists(_filePath)) throw new FileNotFoundException(_filePath);
            string[] lines = File.ReadAllLines(_filePath);
            if (lines.Length < 3) throw new Exception("File must have at least 3 lines.");

            var model = new LinearProgrammingModel();
            ParseObjectiveFunction(lines[0], model);
            ParseConstraints(lines, model);
            ParseVariableTypes(lines[lines.Length - 1], model);
            if (!model.IsValid()) throw new Exception("Model invalid.");
            return model;
        }

        private void ParseObjectiveFunction(string line, LinearProgrammingModel model)
        {
            var tokens = line.Trim().Split(' ');
            model.ObjectiveType = tokens[0].ToLower() == "max" ? ObjectiveType.Maximize : ObjectiveType.Minimize;
            for (int i = 1; i < tokens.Length; i++)
            {
                double coeff = double.Parse(tokens[i].Replace("+", ""));
                model.ObjectiveCoefficients.Add(coeff);
                model.Variables.Add(new Variable($"x{i}", VariableType.Continuous));
            }
        }

        private void ParseConstraints(string[] lines, LinearProgrammingModel model)
        {
            for (int i = 1; i < lines.Length - 1; i++)
            {
                var tokens = lines[i].Trim().Split(' ');
                var coeffs = new List<double>();
                for (int j = 0; j < model.VariableCount; j++)
                    coeffs.Add(double.Parse(tokens[j].Replace("+", "")));
                ConstraintType type = tokens[model.VariableCount] == "<=" ? ConstraintType.LessThanOrEqual :
                                      tokens[model.VariableCount] == ">=" ? ConstraintType.GreaterThanOrEqual : ConstraintType.Equal;
                double rhs = double.Parse(tokens[model.VariableCount + 1]);
                model.Constraints.Add(new Constraint(coeffs, type, rhs, $"C{i}"));
            }
        }

        private void ParseVariableTypes(string line, LinearProgrammingModel model)
        {
            var types = line.Trim().Split(' ');
            for (int i = 0; i < types.Length; i++)
            {
                VariableType vt = types[i] == "+" ? VariableType.Continuous :
                                  types[i] == "-" ? VariableType.Continuous :
                                  types[i] == "urs" ? VariableType.Unrestricted :
                                  types[i] == "int" ? VariableType.Integer :
                                  types[i] == "bin" ? VariableType.Binary : VariableType.Continuous;
                model.Variables[i].Type = vt;
            }
        }
    }
}