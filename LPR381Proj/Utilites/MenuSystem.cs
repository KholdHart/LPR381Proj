using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;
using LinearProgrammingProject.IO;
using LinearProgrammingProject.Utilities;
using LinearProgrammingProject.Algorithms;
using LinearProgrammingProject.Solver;
using LinearProgrammingProject.SensitivityAnalysis;

namespace LinearProgrammingProject.Utilities
{
    public class MenuSystem
    {
        private LinearProgrammingModel _model;
        private const int MENU_WIDTH = 80;

        public void Run()
        {
            ShowWelcomeScreen();

            while (true)
            {
                DisplayMainMenu();
                int choice = GetChoice(1, 6);

                Console.Clear();
                switch (choice)
                {
                    case 1:
                        LoadModel();
                        break;
                    case 2:
                        SolveModel();
                        break;
                    case 3:
                        SensitivityAnalysis();
                        break;
                    case 4:
                        ViewOutputFile();
                        break;
                    case 5:
                        RunSensitivityAnalysisTests();
                        break;
                    case 6:
                        ShowExitMessage();
                        return;
                }
            }
        }

        private void ShowWelcomeScreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            DrawBox("LP/IP OPTIMIZATION SOLVER", "Advanced Mathematical Programming Suite");
            Console.ResetColor();

            Console.WriteLine("\n" + CenterText("Supported Algorithms:"));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(CenterText("• Primal Simplex Method"));
            Console.WriteLine(CenterText("• Revised Primal Simplex"));
            Console.WriteLine(CenterText("• Branch & Bound for Integer Programming"));
            Console.WriteLine(CenterText("• Cutting Plane Method"));
            Console.WriteLine(CenterText("• Knapsack Branch & Bound"));
            Console.ResetColor();

            Console.WriteLine("\n" + CenterText("Press any key to continue..."));
            Console.ReadKey();
        }

        private void DisplayMainMenu()
        {
            Console.Clear();

            // Header
            Console.ForegroundColor = ConsoleColor.Green;
            DrawSeparator('═');
            Console.WriteLine(CenterText("MAIN MENU"));
            DrawSeparator('═');
            Console.ResetColor();

            // Model status
            DisplayModelStatus();

            // Menu options
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │                              MENU OPTIONS                                   │");
            Console.WriteLine("  ├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("  │                                                                             │");
            Console.WriteLine("  │  1. Load Model                                                              │");
            Console.WriteLine("  │  2. Solve Model                                                             │");
            Console.WriteLine("  │  3. Sensitivity Analysis                                                    │");
            Console.WriteLine("  │  4. View Output File                                                        │");
            Console.WriteLine("  │  5. Run Sensitivity Analysis Tests                                          │");
            Console.WriteLine("  │  6. Exit                                                                    │");
            Console.WriteLine("  │                                                                             │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
            Console.ResetColor();

            Console.WriteLine();
            Console.Write("  Please select an option (1-5): ");
        }

        private void DisplayModelStatus()
        {
            Console.WriteLine();
            if (_model == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("  │ STATUS: No model loaded                                                     │");
                Console.WriteLine("  │ ACTION: Please load a model file before solving                             │");
                Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ───────────────────────────────────────────────────────────────────────────────");
                Console.WriteLine($"   STATUS: Model loaded successfully ");
                Console.WriteLine($"   VARIABLES:{_model.Variables.Count,-3}│CONSTRAINTS:{_model.Constraints.Count,-3}│TYPE: {GetModelType(),-15} ");
                Console.WriteLine("  ───────────────────────────────────────────────────────────────────────────────");
            }
            Console.ResetColor();
        }

        private string GetModelType()
        {
            if (_model == null) return "Unknown";
            if (_model.IsBinaryProgram()) return "Binary Program";
            if (_model.IsIntegerProgram()) return "Integer Program";
            return "Linear Program";
        }

        private void ShowExitMessage()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            DrawBox("THANK YOU", "Thank you for using the LP/IP Solver!");
            Console.ResetColor();

            Console.WriteLine("\n" + CenterText("Session completed successfully."));
            Console.WriteLine(CenterText("Press any key to exit..."));
            Console.ReadKey();
        }

        private void LoadModel()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            DrawSeparator('═');
            Console.WriteLine(CenterText("LOAD MODEL"));
            DrawSeparator('═');
            Console.ResetColor();

            Console.WriteLine("\n  Available sample files:");
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("  • universal_lp.txt      - Linear programming model");
            Console.WriteLine("  • universal_ip.txt      - Integer programming model");
            Console.WriteLine("  • universal_binary.txt  - Binary programming model");
            Console.WriteLine("  • knapsack_small.txt    - Small knapsack problem");
            Console.WriteLine("  • knapsack_medium.txt   - Medium knapsack problem");
            Console.WriteLine("  • example_format.txt    - Example from specification");
            Console.ResetColor();

            Console.WriteLine("\n  Enter the file path (or just filename for samples):");
            Console.Write("  File: ");
            string path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path))
            {
                ShowError("File path cannot be empty!");
                Wait();
                return;
            }

            Console.WriteLine("\n  Loading model...");

            try
            {
                var reader = new InputReader(path);
                _model = reader.ReadModel();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  SUCCESS!");
                Console.WriteLine("   ┌───────────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"   │     Model loaded successfully from: {path,-43}   | ");
                Console.WriteLine("   └───────────────────────────────────────────────────────────────────────────────────┘");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load model: {ex.Message}");
            }

            Wait();
        }

        private void SolveModel()
        {
            if (_model == null)
            {
                ShowError("No model loaded! Please load a model first.");
                Wait();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            DrawSeparator('═');
            Console.WriteLine(CenterText("ALGORITHM SELECTION"));
            DrawSeparator('═');
            Console.ResetColor();

            DisplayAlgorithmMenu();
            int alg = GetChoice(1, 5);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  STARTING OPTIMIZATION PROCESS");
            DrawSeparator('─');
            Console.ResetColor();

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

                    // Save Primal Simplex results to output file with canonical form and all iterations
                    WritePrimalSimplexOutput(result, "output.txt");
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

                    // Save Branch & Bound results to output file with canonical form and all iterations
                    WriteBranchAndBoundOutput(bnbResult, "output.txt");
                }
                else if (alg == 5)
                {
                    // Branch & Bound Knapsack
                    if (!_model.IsBinaryProgram())
                    {
                        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║                                    ERROR                                    ║");
                        Console.WriteLine("║                                                                               ║");
                        Console.WriteLine("║  Knapsack algorithm requires ALL variables to be binary (bin)!                ║");
                        Console.WriteLine("║  Please load a knapsack model file (e.g., knapsack_small.txt)                 ║");
                        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                        Wait();
                        return;
                    }

                    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                      STARTING KNAPSACK BRANCH & BOUND                       ║");
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
                }
                else
                {
                    Console.WriteLine($"Algorithm {alg} not yet implemented.");
                    Wait();
                    return;
                }

                // Show completion message for all algorithms
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  OPTIMIZATION COMPLETE");
                Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"  │ Algorithm: {GetAlgorithmName(alg),-25}                                        │");
                Console.WriteLine($"  │ Status: {_model.Status,-15}                                                     │");
                Console.WriteLine("  │ Output: Detailed results saved to output.txt                                │");
                Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n  OPTIMIZATION FAILED");
                Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($" │ Error: {ex.Message,-67}                                                     │");
                Console.WriteLine("  │                                                                             │");
                Console.WriteLine("  │ Possible solutions:                                                         │");
                Console.WriteLine("  │ • Check if the model file format is correct                                 │");
                Console.WriteLine("  │ • Verify that the selected algorithm matches the model type                 │");
                Console.WriteLine("  │ • Ensure the model is feasible                                              │");
                Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n  Debug information: {ex.StackTrace}");
                Console.ResetColor();
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

        private void ViewOutputFile()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            DrawSeparator('═');
            Console.WriteLine(CenterText("OUTPUT FILE VIEWER"));
            DrawSeparator('═');
            Console.ResetColor();

            string outputPath = "output.txt";

            try
            {
                if (!System.IO.File.Exists(outputPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine("    │ No output file found!                                                       │");
                    Console.WriteLine("    │                                                                             │");
                    Console.WriteLine("    │ The output file 'output.txt' does not exist.                                │");
                    Console.WriteLine("    │ Please solve a model first to generate output.                              │");
                    Console.WriteLine("    └─────────────────────────────────────────────────────────────────────────────┘");
                    Console.ResetColor();
                    Wait();
                    return;
                }

                string content = System.IO.File.ReadAllText(outputPath);

                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine("    │ Output file is empty!                                                       │");
                    Console.WriteLine("    │                                                                             │");
                    Console.WriteLine("    │ The output file exists but contains no data.                                │");
                    Console.WriteLine("    │ Please solve a model to generate output.                                    │");
                    Console.WriteLine("    └─────────────────────────────────────────────────────────────────────────────┘");
                    Console.ResetColor();
                    Wait();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"   │ Displaying contents of: {outputPath,-52}                                    │");
                Console.WriteLine("    └─────────────────────────────────────────────────────────────────────────────┘");
                Console.ResetColor();

                Console.WriteLine();
                DrawSeparator('─');
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(content);
                Console.ResetColor();
                DrawSeparator('─');

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n  File size: {new System.IO.FileInfo(outputPath).Length} bytes");
                Console.WriteLine($"  Last modified: {System.IO.File.GetLastWriteTime(outputPath):yyyy-MM-dd HH:mm:ss}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to read output file: {ex.Message}");
            }

            Wait();
        }

        private void SensitivityAnalysis()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            DrawSeparator('═');
            Console.WriteLine(CenterText("SENSITIVITY ANALYSIS"));
            DrawSeparator('═');
            Console.ResetColor();

            Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │                          SENSITIVITY ANALYSIS OPTIONS                       │");
            Console.WriteLine("  ├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("  │  1. Load and solve the specific sensitivity analysis problem               │");
            Console.WriteLine("  │  2. Perform sensitivity analysis on current loaded model                   │");
            Console.WriteLine("  │  0. Return to main menu                                                     │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
            Console.Write("\n  Select option (0-2): ");

            int choice = GetChoice(0, 2);

            switch (choice)
            {
                case 1:
                    SolveSensitivityAnalysisProblem();
                    break;
                case 2:
                    PerformSensitivityAnalysisOnCurrentModel();
                    break;
                case 0:
                    return;
            }
        }

        private void SolveSensitivityAnalysisProblem()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SENSITIVITY ANALYSIS PROBLEM SOLVER                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            try
            {
                var problemSolver = new SensitivityProblemSolver();
                var solution = problemSolver.SolveWithSensitivityAnalysis();
                
                // Display the solution
                problemSolver.DisplaySolution(solution);
                
                // Update the current model with the solved problem
                _model = problemSolver.GetModel();
                
                // Launch sensitivity analysis on the solved problem
                Console.Clear();
                var sensitivityEngine = new SensitivityEngine(_model);
                sensitivityEngine.Run();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to solve sensitivity analysis problem: {ex.Message}");
                Wait();
            }
        }

        private void PerformSensitivityAnalysisOnCurrentModel()
        {
            if (_model == null)
            {
                ShowError("No model loaded! Please load a model first or use option 1 to solve the specific problem.");
                Wait();
                return;
            }

            if (_model.Status != SolutionStatus.Optimal)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("  │ WARNING: Model has not been solved or does not have an optimal solution.    │");
                Console.WriteLine("  │                                                                             │");
                Console.WriteLine("  │ For accurate sensitivity analysis, please solve the model first.            │");
                Console.WriteLine("  │ Some features may not work correctly without an optimal solution.           │");
                Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                Console.Write("\n  Do you want to continue anyway? (y/n): ");
                string proceed = Console.ReadLine();
                if (proceed?.ToLower() != "y")
                {
                    return;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  STARTING SENSITIVITY ANALYSIS");
            Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │ Available Features:                                                         │");
            Console.WriteLine("  │ • Non-basic and basic variable range analysis                              │");
            Console.WriteLine("  │ • Right-hand-side sensitivity analysis                                     │");
            Console.WriteLine("  │ • Shadow prices and reduced costs                                          │");
            Console.WriteLine("  │ • Dual problem formulation and solution                                    │");
            Console.WriteLine("  │ • Adding new activities and constraints                                     │");
            Console.WriteLine("  │ • Duality verification (strong/weak)                                       │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
            Console.ResetColor();

            Console.WriteLine("\n  Launching sensitivity analysis module...");
            Wait();

            try
            {
                var sensitivityEngine = new SensitivityEngine(_model);
                sensitivityEngine.Run();
            }
            catch (Exception ex)
            {
                ShowError($"Sensitivity analysis failed: {ex.Message}");
                Wait();
            }
        }

        private void RunSensitivityAnalysisTests()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        SENSITIVITY ANALYSIS TESTS                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │                              TEST OPTIONS                                   │");
            Console.WriteLine("  ├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("  │  1. Run comprehensive functionality tests                                  │");
            Console.WriteLine("  │  2. Demonstrate sensitivity analysis features                               │");
            Console.WriteLine("  │  3. Run interactive demonstration                                           │");
            Console.WriteLine("  │  4. Verify all required operations are implemented                          │");
            Console.WriteLine("  │  5. Display quick start guide                                               │");
            Console.WriteLine("  │  0. Return to main menu                                                     │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
            Console.Write("\n  Select option (0-2): ");

            int choice = GetChoice(0, 5);

            switch (choice)
            {
                case 1:
                    FunctionalityTest.RunComprehensiveTest();
                    break;
                case 2:
                    SensitivityAnalysisTests.DemonstrateFeatures();
                    break;
                case 3:
                    DemoScript.RunDemo();
                    break;
                case 4:
                    OperationsVerification.VerifyAllOperations();
                    break;
                case 5:
                    MenuVerification.DisplayQuickStartGuide();
                    break;
                case 0:
                    return;
            }

            Wait();
        }

        private int GetChoice(int min, int max)
        {
            int choice;
            while (true)
            {
                string input = Console.ReadLine();

                if (int.TryParse(input, out choice) && choice >= min && choice <= max)
                {
                    return choice;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"  Invalid input! Please enter a number between {min} and {max}: ");
                Console.ResetColor();
            }
        }

        private void WriteKnapsackOutput(KnapsackBranchAndBound.KnapsackReport result, string outputPath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(outputPath))
                {
                    writer.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    writer.WriteLine("║                    KNAPSACK BRANCH & BOUND ALGORITHM REPORT                   ║");
                    writer.WriteLine("║                     All decimal values rounded to 3 points                    ║");
                    writer.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    writer.WriteLine();

                    // Write canonical form
                    writer.WriteLine("CANONICAL FORM");
                    writer.WriteLine(new string('=', 60));
                    // Extract canonical form from iteration logs
                    foreach (var log in result.IterationLogs)
                    {
                        if (log.Contains("CANONICAL FORM") || log.Contains("Maximize") || log.Contains("Subject to"))
                        {
                            writer.WriteLine(log);
                            break;
                        }
                    }
                    writer.WriteLine();

                    // Write all tableau iterations
                    writer.WriteLine("TABLEAU ITERATIONS");
                    writer.WriteLine(new string('=', 60));
                    // Write all iteration logs which include canonical form and table iterations
                    foreach (var log in result.IterationLogs)
                    {
                        writer.WriteLine(log);
                    }
                    writer.WriteLine();

                    // Write final solution
                    writer.WriteLine("FINAL SOLUTION");
                    writer.WriteLine(new string('=', 60));
                    writer.WriteLine($"Algorithm: Branch & Bound Knapsack");
                    writer.WriteLine($"Total Iterations: {result.IterationLogs.Count}");
                    writer.WriteLine($"Status: {(_model.Status == SolutionStatus.Optimal ? "Optimal" : _model.Status.ToString())}");

                    if (result.BestSolution != null)
                    {
                        writer.WriteLine($"Optimal Value: {result.BestValue:F3}");
                        writer.WriteLine("Variable Values:");

                        // Write variable assignments in the same format as other algorithms
                        for (int i = 0; i < result.Items.Count; i++)
                        {
                            var item = result.Items[i];
                            int value = result.BestSolution.IncludedItems.Contains(i) ? 1 : 0;
                            writer.WriteLine($"  {item.Name} = {value:F3}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("No feasible solution found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Knapsack output: {ex.Message}");
            }
        }

        private void WritePrimalSimplexOutput(PrimalSimplexSolver.SimplexReport result, string outputPath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(outputPath))
                {
                    writer.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    writer.WriteLine("║                        PRIMAL SIMPLEX ALGORITHM REPORT                        ║");
                    writer.WriteLine("║                     All decimal values rounded to 3 points                    ║");
                    writer.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    writer.WriteLine();

                    // Write canonical form
                    writer.WriteLine("CANONICAL FORM");
                    writer.WriteLine(new string('=', 60));
                    writer.WriteLine(result.CanonicalForm);
                    writer.WriteLine();

                    // Write all tableau iterations
                    writer.WriteLine("TABLEAU ITERATIONS");
                    writer.WriteLine(new string('=', 60));
                    foreach (var snapshot in result.IterationSnapshots)
                    {
                        writer.WriteLine(snapshot);
                        writer.WriteLine();
                    }

                    // Write pivot operations summary
                    if (result.Pivots.Count > 0)
                    {
                        writer.WriteLine("PIVOT OPERATIONS SUMMARY");
                        writer.WriteLine(new string('=', 60));
                        foreach (var pivot in result.Pivots)
                        {
                            writer.WriteLine($"  {pivot}");
                        }
                        writer.WriteLine();
                    }

                    // Write final solution
                    writer.WriteLine("FINAL SOLUTION");
                    writer.WriteLine(new string('=', 60));
                    writer.WriteLine($"Algorithm: Primal Simplex");
                    writer.WriteLine($"Iterations: {result.IterationSnapshots.Count}");
                    writer.WriteLine($"Status: {_model.Status}");

                    if (_model.Status == SolutionStatus.Optimal)
                    {
                        writer.WriteLine($"Optimal Value: {_model.OptimalValue:F3}");
                        writer.WriteLine("Variable Values:");
                        foreach (var kvp in _model.OptimalSolution)
                        {
                            writer.WriteLine($"  {kvp.Key} = {kvp.Value:F3}");
                        }
                    }
                    else if (_model.Status == SolutionStatus.Unbounded)
                    {
                        writer.WriteLine("The problem is unbounded.");
                    }
                    else if (_model.Status == SolutionStatus.Infeasible)
                    {
                        writer.WriteLine("The problem is infeasible.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Primal Simplex output: {ex.Message}");
            }
        }

        private void WriteBranchAndBoundOutput(BranchAndBoundSimplex.BranchAndBoundReport result, string outputPath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(outputPath))
                {
                    writer.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
                    writer.WriteLine("║                    BRANCH & BOUND SIMPLEX ALGORITHM REPORT                    ║");
                    writer.WriteLine("║                     All decimal values rounded to 3 points                    ║");
                    writer.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
                    writer.WriteLine();

                    // Write canonical form
                    writer.WriteLine("CANONICAL FORM");
                    writer.WriteLine(new string('=', 60));
                    writer.WriteLine(result.CanonicalForm);
                    writer.WriteLine();

                    // Write all iteration logs (includes all tableau iterations)
                    writer.WriteLine("BRANCH & BOUND ITERATIONS");
                    writer.WriteLine(new string('=', 60));
                    foreach (var log in result.IterationLogs)
                    {
                        writer.WriteLine(log);
                        writer.WriteLine();
                    }

                    // Write final solution
                    writer.WriteLine("FINAL SOLUTION");
                    writer.WriteLine(new string('=', 60));
                    writer.WriteLine($"Algorithm: Branch & Bound Simplex");
                    writer.WriteLine($"Iterations: {result.IterationLogs.Count}");
                    writer.WriteLine($"Status: {_model.Status}");

                    if (_model.Status == SolutionStatus.Optimal && result.BestIntegerSolution != null)
                    {
                        writer.WriteLine($"Optimal Value: {result.BestIntegerValue:F3}");
                        writer.WriteLine("Variable Values:");
                        foreach (var kvp in result.BestIntegerSolution.Solution)
                        {
                            writer.WriteLine($"  {kvp.Key} = {kvp.Value:F3}");
                        }
                    }
                    else if (_model.Status == SolutionStatus.Infeasible)
                    {
                        writer.WriteLine("No feasible integer solution found.");
                    }
                    else if (_model.Status == SolutionStatus.Unbounded)
                    {
                        writer.WriteLine("The problem is unbounded.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Branch & Bound output: {ex.Message}");
            }
        }

        private void DisplayAlgorithmMenu()
        {
            Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │                           AVAILABLE ALGORITHMS                              │");
            Console.WriteLine("  ├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("  │                                                                             │");

            // Show algorithm compatibility
            string modelType = GetModelType();

            Console.WriteLine($"  │  Current Model Type: {modelType,-25}                              │");
            Console.WriteLine("  │                                                                             │");
            Console.WriteLine("  │   1.  Primal Simplex                                                        │");
            Console.WriteLine("  │   2.  Revised Primal Simplex                                                │");

            if (_model.IsIntegerProgram())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  │   3. Branch & Bound Simplex                                                 │");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  │   3. Branch & Bound Simplex                                                 │");
                Console.ResetColor();
            }

            Console.WriteLine("  │   4. Cutting Plane                                                          │");

            if (_model.IsBinaryProgram())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  │   5. Branch & Bound Knapsack                                                │");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  │   5. Branch & Bound Knapsack                                                │");
                Console.ResetColor();
            }

            Console.WriteLine("  │                                                                             │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");

            Console.WriteLine("\n  Select algorithm (1-5): ");
        }

        private void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  ERROR");
            Console.WriteLine("  ┌────────────────────────────────────────────────────────────────┐");
            Console.WriteLine($" │ {message,-75}                                                  │");
            Console.WriteLine("  └────────────────────────────────────────────────────────────────┘");
            Console.ResetColor();
        }

        private void DrawBox(string title, string subtitle = "")
        {
            DrawSeparator('╔', '╗', '═');
            Console.WriteLine($"║{CenterTextForBox(title)}║");
            if (!string.IsNullOrEmpty(subtitle))
            {
                Console.WriteLine($"║{CenterTextForBox(subtitle)}║");
            }
            DrawSeparator('╚', '╝', '═');
        }

        private void DrawSeparator(char character = '─')
        {
            Console.WriteLine(new string(character, MENU_WIDTH));
        }

        private void DrawSeparator(char left, char right, char fill)
        {
            Console.WriteLine($"{left}{new string(fill, MENU_WIDTH - 2)}{right}");
        }

        private string CenterText(string text)
        {
            if (text.Length >= MENU_WIDTH) return text;
            int padding = (MENU_WIDTH - text.Length) / 2;
            return new string(' ', padding) + text + new string(' ', MENU_WIDTH - text.Length - padding);
        }

        private string CenterTextForBox(string text)
        {
            int availableWidth = MENU_WIDTH - 2; // Account for the box borders
            if (text.Length >= availableWidth)
            {
                // Truncate if too long
                text = text.Substring(0, availableWidth - 3) + "...";
            }

            int padding = (availableWidth - text.Length) / 2;
            int rightPadding = availableWidth - text.Length - padding;
            return new string(' ', padding) + text + new string(' ', rightPadding);
        }

        private void Wait()
        {
            Console.WriteLine("\n  Press any key to continue...");
            Console.ReadKey();
        }
    }
}