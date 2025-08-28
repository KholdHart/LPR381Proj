using System;
using System.Collections.Generic;
using System.Linq;
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
                else if (alg == 5)
                {
                    // Branch & Bound Knapsack
                    if (!_model.IsBinaryProgram())
                    {
                        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║                                  ❌ ERROR                                     ║");
                        Console.WriteLine("║                                                                               ║");
                        Console.WriteLine("║  Knapsack algorithm requires ALL variables to be binary (bin)!               ║");
                        Console.WriteLine("║  Please load a knapsack model file (e.g., knapsack_small.txt)               ║");
                        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                        Wait();
                        return;
                    }
                    
                    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    🚀 STARTING KNAPSACK BRANCH & BOUND                       ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    Console.WriteLine("Processing...\n");
                    
                    var knapsackSolver = new KnapsackBranchAndBound();
                    var knapsackResult = knapsackSolver.Solve(_model);
                    
                    // Display all iteration logs (includes canonical form and all table iterations)
                    foreach (var log in knapsackResult.IterationLogs)
                    {
                        Console.WriteLine(log);
                    }
                    
                    // Display branch and bound tree
                    knapsackSolver.DisplayBranchAndBoundTree(knapsackResult);
                    
                    // Show backtracking process
                    knapsackSolver.DisplayBacktrackingProcess(knapsackResult);
                    
                    Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                            📊 DETAILED ANALYSIS                              ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    Console.WriteLine("Would you like to see all node table iterations? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        knapsackSolver.DisplayAllTableIterations(knapsackResult);
                    }
                    
                    // Update model with best solution
                    if (knapsackResult.BestSolution != null)
                    {
                        _model.OptimalValue = knapsackResult.BestValue;
                        _model.OptimalSolution = new Dictionary<string, double>();
                        
                        // Set all variables to 0 first
                        for (int i = 0; i < _model.Variables.Count; i++)
                        {
                            _model.Variables[i].Value = 0;
                            _model.OptimalSolution[_model.Variables[i].Name] = 0;
                        }
                        
                        // Set included items to 1
                        foreach (var itemIndex in knapsackResult.BestSolution.IncludedItems)
                        {
                            _model.Variables[itemIndex].Value = 1;
                            _model.OptimalSolution[_model.Variables[itemIndex].Name] = 1;
                        }
                        
                        _model.Status = SolutionStatus.Optimal;
                    }
                    else
                    {
                        _model.Status = SolutionStatus.Infeasible;
                    }
                    
                    // Write knapsack-specific output
                    WriteKnapsackOutput(knapsackResult, "output.txt");
                    Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    ✅ SOLUTION COMPLETE & SAVED                              ║");
                    Console.WriteLine("║                                                                               ║");
                    Console.WriteLine("║  📄 Complete results written to: output.txt                                  ║");
                    Console.WriteLine("║  📊 Includes: Canonical form, all iterations, optimal solution               ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    return; // Exit early for knapsack to avoid duplicate output writing
                }
                else
                {
                    Console.WriteLine($"Algorithm {alg} not yet implemented.");
                    Wait();
                    return;
                }
                
                // Write output for other algorithms
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

        private void WriteKnapsackOutput(KnapsackBranchAndBound.KnapsackReport result, string outputPath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(outputPath))
                {
                    writer.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    writer.WriteLine("║                    KNAPSACK BRANCH & BOUND ALGORITHM REPORT                  ║");
                    writer.WriteLine("║                           All decimal values rounded to 3 points             ║");
                    writer.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    writer.WriteLine();
                    
                    // Write all iteration logs which include canonical form and table iterations
                    foreach (var log in result.IterationLogs)
                    {
                        writer.WriteLine(log);
                    }
                    
                    writer.WriteLine("\n" + new string('=', 60));
                    writer.WriteLine("DETAILED NODE ANALYSIS");
                    writer.WriteLine(new string('=', 60));
                    
                    // Write detailed node information
                    foreach (var node in result.AllNodes)
                    {
                        writer.WriteLine($"\n--- NODE {node.Id} DETAILED TABLE ---");
                        if (node.ParentId > 0)
                        {
                            writer.WriteLine($"Parent: Node {node.ParentId}");
                            writer.WriteLine($"Branch Decision: {node.BranchDecision}");
                        }
                        
                        // Write the table iterations for this node
                        foreach (var table in node.TableIterations)
                        {
                            writer.WriteLine(table);
                        }
                        
                        if (node.IsFathomed)
                        {
                            writer.WriteLine($"FATHOMED: {node.FathomReason}");
                        }
                        
                        writer.WriteLine(new string('-', 40));
                    }
                    
                    // Write final summary
                    writer.WriteLine("\n" + new string('=', 60));
                    writer.WriteLine("BRANCH & BOUND TREE SUMMARY");
                    writer.WriteLine(new string('=', 60));
                    
                    var nodesByLevel = result.AllNodes.GroupBy(n => n.Level).OrderBy(g => g.Key);
                    
                    foreach (var levelGroup in nodesByLevel)
                    {
                        writer.WriteLine($"\nDepth {levelGroup.Key}:");
                        foreach (var node in levelGroup.OrderBy(n => n.Id))
                        {
                            string status = node.IsFathomed ? $"FATHOMED ({node.FathomReason})" : "ACTIVE";
                            string branchInfo = node.ParentId > 0 ? $" [{node.BranchDecision}]" : "";
                            
                            writer.WriteLine($"  Node {node.Id}{branchInfo} - {status}");
                            writer.WriteLine($"    Value: {node.CurrentValue:F3}, Weight: {node.CurrentWeight:F3}, Upper Bound: {node.UpperBound:F3}");
                            
                            if (node.IncludedItems.Count > 0)
                            {
                                var includedNames = node.IncludedItems.Select(i => result.Items[i].Name);
                                writer.WriteLine($"    Included: [{string.Join(", ", includedNames)}]");
                            }
                        }
                    }
                    
                    writer.WriteLine($"\nTotal Statistics:");
                    writer.WriteLine($"Total nodes created: {result.TotalNodes}");
                    writer.WriteLine($"Nodes explored: {result.NodesExplored}");
                    writer.WriteLine($"Nodes fathomed: {result.NodesFathomed}");
                    
                    // Enhanced solution summary
                    writer.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
                    writer.WriteLine("║                            FINAL SOLUTION SUMMARY                            ║");
                    writer.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    
                    if (result.BestSolution != null)
                    {
                        writer.WriteLine($"\n🏆 OPTIMAL SOLUTION FOUND:");
                        writer.WriteLine($"┌─────────────────────────────────────────────────────────────────────────────┐");
                        writer.WriteLine($"│ Algorithm: Branch & Bound Knapsack                                         │");
                        writer.WriteLine($"│ Status: OPTIMAL                                                             │");
                        writer.WriteLine($"│ Optimal Value: {result.BestValue,7:F3}                                              │");
                        writer.WriteLine($"│ Total Weight: {result.BestSolution.CurrentWeight,8:F3}                                              │");
                        writer.WriteLine($"│ Capacity Used: {(result.BestSolution.CurrentWeight / result.Capacity * 100),6:F1}%                                                │");
                        writer.WriteLine($"│ Items Selected: {result.BestSolution.IncludedItems.Count,2} out of {result.Items.Count}                                           │");
                        writer.WriteLine($"└─────────────────────────────────────────────────────────────────────────────┘");
                        
                        writer.WriteLine($"\n📋 VARIABLE ASSIGNMENTS:");
                        writer.WriteLine($"┌─────────┬─────────┬─────────┬─────────┬─────────────────────────────────────┐");
                        writer.WriteLine($"│Variable │ Value   │ Item    │ Weight  │ Contribution                        │");
                        writer.WriteLine($"│         │         │ Value   │         │                                     │");
                        writer.WriteLine($"├─────────┼─────────┼─────────┼─────────┼─────────────────────────────────────┤");
                        
                        for (int i = 0; i < result.Items.Count; i++)
                        {
                            var item = result.Items[i];
                            int value = result.BestSolution.IncludedItems.Contains(i) ? 1 : 0;
                            string contribution = value == 1 ? $"Value: {item.Value:F3}, Weight: {item.Weight:F3}" : "Not selected";
                            writer.WriteLine($"│ {item.Name,-7} │    {value}    │ {item.Value,7:F3} │ {item.Weight,7:F3} │ {contribution,-35} │");
                        }
                        
                        writer.WriteLine($"└─────────┴─────────┴─────────┴─────────┴─────────────────────────────────────┘");
                        
                        writer.WriteLine($"\n📊 SOLUTION VERIFICATION:");
                        writer.WriteLine($"┌─ Total Value: {result.BestValue:F3}");
                        writer.WriteLine($"├─ Total Weight: {result.BestSolution.CurrentWeight:F3}");
                        writer.WriteLine($"├─ Capacity Limit: {result.Capacity:F3}");
                        writer.WriteLine($"├─ Remaining Capacity: {(result.Capacity - result.BestSolution.CurrentWeight):F3}");
                        writer.WriteLine($"└─ Feasible: {(result.BestSolution.CurrentWeight <= result.Capacity ? "YES" : "NO")}");
                        
                        writer.WriteLine($"\n🎯 EFFICIENCY METRICS:");
                        writer.WriteLine($"┌─ Nodes Created: {result.TotalNodes}");
                        writer.WriteLine($"├─ Nodes Explored: {result.NodesExplored}");
                        writer.WriteLine($"├─ Nodes Fathomed: {result.NodesFathomed}");
                        writer.WriteLine($"├─ Pruning Efficiency: {(double)result.NodesFathomed / result.TotalNodes * 100:F1}%");
                        writer.WriteLine($"└─ Search Space Reduction: {(1 - (double)result.TotalNodes / Math.Pow(2, result.Items.Count)) * 100:F1}%");
                    }
                    else
                    {
                        writer.WriteLine($"\n❌ NO FEASIBLE SOLUTION FOUND");
                        writer.WriteLine($"┌─────────────────────────────────────────────────────────────────────────────┐");
                        writer.WriteLine($"│ Algorithm: Branch & Bound Knapsack                                         │");
                        writer.WriteLine($"│ Status: INFEASIBLE                                                          │");
                        writer.WriteLine($"│ Reason: No combination of items fits within the knapsack capacity          │");
                        writer.WriteLine($"└─────────────────────────────────────────────────────────────────────────────┘");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing output: {ex.Message}");
            }
        }

        private void Wait()
        {
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
