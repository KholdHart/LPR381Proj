using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
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
            // Better error handling for file existence
            if (!File.Exists(_filePath))
            {
                // Show both the original path and the resolved path for debugging
                string originalPath = Path.GetFileName(_filePath);
                throw new FileNotFoundException($"File not found: {originalPath}\nSearched at: {_filePath}\nCurrent directory: {Directory.GetCurrentDirectory()}");
            }

            string[] lines = File.ReadAllLines(_filePath);
            if (lines.Length < 3)
                throw new Exception($"File must have at least 3 lines. Found {lines.Length} lines.");

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

                model.ObjectiveType = tokens[0].ToLower() == "max" ? ObjectiveType.Maximize : ObjectiveType.Minimize;

                // Parse coefficients with their signs
                for (int i = 1; i < tokens.Length; i++)
                {
                    double coeff;
                    string token = tokens[i];

                    // Handle different formats: "+30", "-30", "30"
                    if (token.StartsWith("+"))
                    {
                        if (!double.TryParse(token.Substring(1), out coeff))
                            throw new Exception($"Invalid coefficient '{token}' in objective function.");
                    }
                    else if (token.StartsWith("-"))
                    {
                        if (!double.TryParse(token.Substring(1), out coeff))
                            throw new Exception($"Invalid coefficient '{token}' in objective function.");
                        coeff = -coeff;
                    }
                    else
                    {
                        if (!double.TryParse(token, out coeff))
                            throw new Exception($"Invalid coefficient '{token}' in objective function.");
                    }

                    model.ObjectiveCoefficients.Add(coeff);
                    model.Variables.Add(new Variable($"x{i}", VariableType.Continuous));
                }

                Console.WriteLine($"Created {model.VariableCount} variables from objective function");
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
                    var tokens = lines[i].Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    Console.WriteLine($"Parsing constraint line {i + 1}: '{lines[i]}'");
                    Console.WriteLine($"Tokens found: [{string.Join(", ", tokens)}] (count: {tokens.Length})");
                    Console.WriteLine($"Expected: {model.VariableCount} coefficients + constraint type + RHS = {model.VariableCount + 2} tokens");

                    if (tokens.Length < model.VariableCount + 2)
                        throw new Exception($"Constraint line {i + 1} has insufficient tokens. Expected at least {model.VariableCount + 2}, found {tokens.Length}.");

                    var coeffs = new List<double>();
                    for (int j = 0; j < model.VariableCount; j++)
                    {
                        if (j >= tokens.Length)
                            throw new Exception($"Missing coefficient for variable x{j + 1} in constraint {i + 1}.");

                        double coeff;
                        string token = tokens[j];

                        // Handle different formats: "+8", "-8", "8", "+1.5", "-1.5"
                        // Use invariant culture to ensure decimal parsing works correctly
                        if (token.StartsWith("+"))
                        {
                            string numberPart = token.Substring(1);
                            Console.WriteLine($"  Parsing positive coefficient: '{token}' -> number part: '{numberPart}'");
                            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                                throw new Exception($"Invalid coefficient '{token}' in constraint {i + 1}. Cannot parse number part: '{numberPart}'");
                        }
                        else if (token.StartsWith("-"))
                        {
                            string numberPart = token.Substring(1);
                            Console.WriteLine($"  Parsing negative coefficient: '{token}' -> number part: '{numberPart}'");
                            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                                throw new Exception($"Invalid coefficient '{token}' in constraint {i + 1}. Cannot parse number part: '{numberPart}'");
                            coeff = -coeff;
                        }
                        else
                        {
                            Console.WriteLine($"  Parsing unsigned coefficient: '{token}'");
                            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                                throw new Exception($"Invalid coefficient '{token}' in constraint {i + 1}.");
                        }

                        coeffs.Add(coeff);
                    }

                    // Parse constraint type
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

                    // Parse RHS
                    if (model.VariableCount + 1 >= tokens.Length)
                        throw new Exception($"Missing RHS value in constraint {i + 1}.");

                    if (!double.TryParse(tokens[model.VariableCount + 1], out double rhs))
                        throw new Exception($"Invalid RHS value '{tokens[model.VariableCount + 1]}' in constraint {i + 1}.");

                    model.Constraints.Add(new Constraint(coeffs, type, rhs, $"C{i}"));
                    Console.WriteLine($"Successfully parsed constraint {i + 1}: [{string.Join(", ", coeffs)}], type: {type}, RHS: {rhs}");
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
                var types = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (types.Length != model.VariableCount)
                    throw new Exception($"Variable types line has {types.Length} types, but model has {model.VariableCount} variables.");

                for (int i = 0; i < types.Length; i++)
                {
                    VariableType vt;
                    switch (types[i].ToLower())
                    {
                        case "+":
                        case "-":
                            vt = VariableType.Continuous;
                            break;
                        case "urs":
                            vt = VariableType.Unrestricted;
                            break;
                        case "int":
                            vt = VariableType.Integer;
                            break;
                        case "bin":
                            vt = VariableType.Binary;
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