using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.IO
{
    public class InputReader
    {
        private string _filePath;
        public InputReader(string filePath)
        {
            // Handle relative paths by making them absolute
            if (!Path.IsPathRooted(filePath))
            {
                _filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            }
            else
            {
                _filePath = filePath;
            }
        }



        public LinearProgrammingModel ReadModel()
        {
            if (!File.Exists(_filePath))
            {
                string originalPath = Path.GetFileName(_filePath);
                throw new FileNotFoundException($"File not found: {originalPath}\nSearched at: {_filePath}\nCurrent directory: {Directory.GetCurrentDirectory()}");
            }

            string[] lines = File.ReadAllLines(_filePath);
            
            // Remove empty lines and trim whitespace
            lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).ToArray();
            
            if (lines.Length < 3)
                throw new Exception($"File must have at least 3 lines (objective function, at least one constraint, variable types). Found {lines.Length} non-empty lines.");

            var model = new LinearProgrammingModel();
            try
            {
                ParseObjectiveFunction(lines[0], model);
                ParseConstraints(lines, model);
                ParseVariableTypes(lines[lines.Length - 1], model);

                if (!model.IsValid())
                    throw new Exception("Model validation failed after parsing.");

                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing model file: {ex.Message}", ex);
            }
        }



        private void ParseObjectiveFunction(string line, LinearProgrammingModel model)
        {
            try
            {
                var tokens = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2)
                    throw new Exception("Objective function line must have at least 2 tokens (objective type and coefficients).");

                // Parse objective type (max or min)
                string objTypeStr = tokens[0].ToLower();
                if (objTypeStr != "max" && objTypeStr != "min")
                    throw new Exception($"Invalid objective type '{tokens[0]}'. Use 'max' or 'min'.");
                
                model.ObjectiveType = objTypeStr == "max" ? ObjectiveType.Maximize : ObjectiveType.Minimize;

                // Parse coefficients with their explicit signs (+ or -)
                for (int i = 1; i < tokens.Length; i++)
                {
                    double coeff;
                    string token = tokens[i];

                    // According to specification, each coefficient must have an explicit operator (+ or -)
                    if (token.StartsWith("+"))
                    {
                        string numberPart = token.Substring(1);
                        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                            throw new Exception($"Invalid coefficient '{token}' in objective function.");
                    }
                    else if (token.StartsWith("-"))
                    {
                        string numberPart = token.Substring(1);
                        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                            throw new Exception($"Invalid coefficient '{token}' in objective function.");
                        coeff = -coeff;
                    }
                    else
                    {
                        // According to specification, coefficients should have explicit signs
                        throw new Exception($"Coefficient '{token}' in objective function must have explicit sign (+ or -). Use '+{token}' or '-{token}'.");
                    }

                    model.ObjectiveCoefficients.Add(coeff);
                    model.Variables.Add(new Variable($"x{i}", VariableType.Continuous));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing objective function '{line}': {ex.Message}", ex);
            }
        }

        private void ParseConstraints(string[] lines, LinearProgrammingModel model)
        {
            try
            {
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                        
                    var tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length < model.VariableCount + 2)
                        throw new Exception($"Constraint line {i + 1} has insufficient tokens. Expected at least {model.VariableCount + 2}, found {tokens.Length}.");

                    var coeffs = new List<double>();
                    
                    // Parse technological coefficients for each variable
                    for (int j = 0; j < model.VariableCount; j++)
                    {
                        if (j >= tokens.Length)
                            throw new Exception($"Missing coefficient for variable x{j + 1} in constraint {i + 1}.");

                        double coeff;
                        string token = tokens[j];

                        // Handle technological coefficients with explicit operators (+ or -)
                        if (token.StartsWith("+"))
                        {
                            string numberPart = token.Substring(1);
                            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                                throw new Exception($"Invalid coefficient '{token}' in constraint {i + 1}.");
                        }
                        else if (token.StartsWith("-"))
                        {
                            string numberPart = token.Substring(1);
                            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                                throw new Exception($"Invalid coefficient '{token}' in constraint {i + 1}.");
                            coeff = -coeff;
                        }
                        else
                        {
                            // According to specification, technological coefficients should have explicit operators
                            throw new Exception($"Technological coefficient '{token}' in constraint {i + 1} must have explicit operator (+ or -). Use '+{token}' or '-{token}'.");
                        }

                        coeffs.Add(coeff);
                    }

                    // Parse constraint relation (<=, >=, =)
                    if (model.VariableCount >= tokens.Length)
                        throw new Exception($"Missing constraint type in constraint {i + 1}.");

                    string typeStr = tokens[model.VariableCount];
                    ConstraintType type;
                    switch (typeStr)
                    {
                        case "<=":
                            type = ConstraintType.LessThanOrEqual;
                            break;
                        case ">=":
                            type = ConstraintType.GreaterThanOrEqual;
                            break;
                        case "=":
                            type = ConstraintType.Equal;
                            break;
                        default:
                            throw new Exception($"Invalid constraint type '{typeStr}' in constraint {i + 1}. Use '<=', '>=', or '='.");
                    }

                    // Parse right-hand-side value
                    if (model.VariableCount + 1 >= tokens.Length)
                        throw new Exception($"Missing RHS value in constraint {i + 1}.");

                    if (!double.TryParse(tokens[model.VariableCount + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs))
                        throw new Exception($"Invalid RHS value '{tokens[model.VariableCount + 1]}' in constraint {i + 1}.");

                    model.Constraints.Add(new Constraint(coeffs, type, rhs, $"C{i}"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing constraints: {ex.Message}", ex);
            }
        }

        private void ParseVariableTypes(string line, LinearProgrammingModel model)
        {
            try
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    throw new Exception("Variable types line is empty.");
                    
                var types = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (types.Length != model.VariableCount)
                    throw new Exception($"Variable types line has {types.Length} types, but model has {model.VariableCount} variables.");

                for (int i = 0; i < types.Length; i++)
                {
                    VariableType vt;
                    string typeStr = types[i].ToLower();
                    
                    switch (typeStr)
                    {
                        case "+":
                            // Non-negative continuous variable (x >= 0)
                            vt = VariableType.Continuous;
                            model.Variables[i].LowerBound = 0.0;
                            model.Variables[i].UpperBound = double.PositiveInfinity;
                            break;
                        case "-":
                            // Non-positive continuous variable (x <= 0)
                            vt = VariableType.Continuous;
                            model.Variables[i].LowerBound = double.NegativeInfinity;
                            model.Variables[i].UpperBound = 0.0;
                            break;
                        case "urs":
                            // Unrestricted variable (can be positive or negative)
                            vt = VariableType.Unrestricted;
                            model.Variables[i].LowerBound = double.NegativeInfinity;
                            model.Variables[i].UpperBound = double.PositiveInfinity;
                            break;
                        case "int":
                            // Integer variable (non-negative by default)
                            vt = VariableType.Integer;
                            model.Variables[i].LowerBound = 0.0;
                            model.Variables[i].UpperBound = double.PositiveInfinity;
                            break;
                        case "bin":
                            // Binary variable (0 or 1)
                            vt = VariableType.Binary;
                            model.Variables[i].LowerBound = 0.0;
                            model.Variables[i].UpperBound = 1.0;
                            break;
                        default:
                            throw new Exception($"Invalid variable type '{types[i]}' for variable {i + 1}. Use '+', '-', 'urs', 'int', or 'bin'.");
                    }
                    
                    model.Variables[i].Type = vt;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing variable types '{line}': {ex.Message}", ex);
            }
        }
    }
}