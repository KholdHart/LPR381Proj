using System;
using System.Collections.Generic;
using LinearProgrammingProject.Models;
using LinearProgrammingProject.IO;
using LinearProgrammingProject.Utilities;
using LinearProgrammingProject.Algorithms;
using LinearProgrammingProject.Solver;

namespace LinearProgrammingProject.Utilities
{
    public class MenuSystem
    {
        private LinearProgrammingModel _model;
        private OutputWriter _outputWriter;

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("LP/IP Solver");
                Console.WriteLine("1. Load Model");
                Console.WriteLine("2. Solve Model");
                Console.WriteLine("3. Sensitivity Analysis");
                Console.WriteLine("4. Exit");
                int choice = GetChoice(1, 4);
                if (choice == 1) LoadModel();
                else if (choice == 2) SolveModel();
                else if (choice == 3) SensitivityAnalysis();
                else break;
            }
        }

        private void LoadModel()
        {
            Console.Write("Input file path: ");
            string path = Console.ReadLine();
            try
            {
                var reader = new InputReader(path);
                _model = reader.ReadModel();
                Console.WriteLine("Model loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Wait();
        }

        private void SolveModel()
        {
            if (_model == null) { Console.WriteLine("Load a model first."); Wait(); return; }
            Console.WriteLine("Select Algorithm:");
            Console.WriteLine("1. Primal Simplex");
            Console.WriteLine("2. Revised Primal Simplex");
            Console.WriteLine("3. Branch & Bound Simplex");
            Console.WriteLine("4. Cutting Plane");
            Console.WriteLine("5. Branch & Bound Knapsack");
            int alg = GetChoice(1, 5);
            
            Console.WriteLine("Solving...");
            
            try
            {
                if (alg == 1)
                {
                    // Primal Simplex
                    var solver = new PrimalSimplexSolver();
                    var result = solver.Solve(_model);
                    
                    // Display canonical form
                    Console.WriteLine(result.CanonicalForm);
                    
                    Console.WriteLine("\n=== PRIMAL SIMPLEX TABLEAU ITERATIONS ===");
                    foreach (var snapshot in result.IterationSnapshots)
                    {
                        Console.WriteLine(snapshot);
                    }
                    
                    if (result.Pivots.Count > 0)
                    {
                        Console.WriteLine("\nPivot Operations Summary:");
                        foreach (var pivot in result.Pivots)
                        {
                            Console.WriteLine($"  {pivot}");
                        }
                    }
                    
                    Console.WriteLine("\n=== FINAL SOLUTION ===");
                    Console.WriteLine($"Status: {_model.Status}");
                    if (_model.Status == SolutionStatus.Optimal)
                    {
                        Console.WriteLine($"Optimal Value: {_model.OptimalValue:F6}");
                        Console.WriteLine("Optimal Solution:");
                        foreach (var kvp in _model.OptimalSolution)
                        {
                            Console.WriteLine($"  {kvp.Key} = {kvp.Value:F6}");
                        }
                    }
                    else if (_model.Status == SolutionStatus.Unbounded)
                    {
                        Console.WriteLine("The problem is unbounded.");
                    }
                }
                else if (alg == 3)
                {
                    // Branch & Bound Simplex
                    if (!_model.IsIntegerProgram())
                    {
                        Console.WriteLine("Error: Branch & Bound requires integer or binary variables!");
                        Wait();
                        return;
                    }
                    
                    var bnbSolver = new BranchAndBoundSimplex();
                    var bnbResult = bnbSolver.Solve(_model);
                    
                    // Display canonical form
                    Console.WriteLine(bnbResult.CanonicalForm);
                    
                    // Display all iteration logs
                    foreach (var log in bnbResult.IterationLogs)
                    {
                        Console.WriteLine(log);
                    }
                    
                    // Display branch and bound tree
                    bnbSolver.DisplayBranchAndBoundTree(bnbResult);
                    
                    Console.WriteLine("\nWould you like to see all simplex table iterations? (y/n)");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        bnbSolver.DisplayAllTableIterations(bnbResult);
                    }
                    
                    // Update model with best solution
                    if (bnbResult.BestIntegerSolution != null)
                    {
                        _model.OptimalValue = bnbResult.BestIntegerValue;
                        _model.OptimalSolution = new Dictionary<string, double>(bnbResult.BestIntegerSolution.Solution);
                        _model.Status = SolutionStatus.Optimal;
                    }
                    else
                    {
                        _model.Status = SolutionStatus.Infeasible;
                    }
                }
                else
                {
                    Console.WriteLine($"Algorithm {alg} not yet implemented.");
                    Wait();
                    return;
                }
                
                // Write output
                _outputWriter = new OutputWriter("output.txt");
                _outputWriter.WriteSolution(_model, GetAlgorithmName(alg), 5);
                _outputWriter.SaveToFile();
                Console.WriteLine("\nSolved. Output written to output.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error solving model: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Wait();
        }
        
        private string GetAlgorithmName(int algorithmNumber)
        {
            switch (algorithmNumber)
            {
                case 1: return "Primal Simplex";
                case 2: return "Revised Primal Simplex";
                case 3: return "Branch & Bound Simplex";
                case 4: return "Cutting Plane";
                case 5: return "Branch & Bound Knapsack";
                default: return $"Algorithm {algorithmNumber}";
            }
        }

        private void SensitivityAnalysis()
        {
            Console.WriteLine("Sensitivity Analysis (not implemented)");
            Wait();
        }

        private int GetChoice(int min, int max)
        {
            int c;
            while (!int.TryParse(Console.ReadLine(), out c) || c < min || c > max)
                Console.Write($"Enter choice ({min}-{max}): ");
            return c;
        }

        private void Wait()
        {
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
