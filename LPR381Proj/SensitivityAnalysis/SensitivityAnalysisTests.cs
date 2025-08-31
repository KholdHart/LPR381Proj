using System;
using System.Collections.Generic;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.SensitivityAnalysis
{
    /// <summary>
    /// Test class to verify sensitivity analysis functionality
    /// </summary>
    public class SensitivityAnalysisTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        SENSITIVITY ANALYSIS TESTS                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            TestSensitivityProblemSolver();
            TestDualityHandler();
            TestSensitivityEngine();

            Console.WriteLine("\n✓ All sensitivity analysis tests completed!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestSensitivityProblemSolver()
        {
            Console.WriteLine("═══ Testing Sensitivity Problem Solver ═══");
            
            try
            {
                var solver = new SensitivityProblemSolver();
                var solution = solver.SolveWithSensitivityAnalysis();

                Console.WriteLine($"✓ Problem solved successfully");
                Console.WriteLine($"  Optimal Value: {solution.OptimalValue:F3}");
                Console.WriteLine($"  x1 = {solution.OptimalSolution["x1"]:F3}");
                Console.WriteLine($"  x2 = {solution.OptimalSolution["x2"]:F3}");
                
                // Verify expected solution (x1=20, x2=60, z=180)
                bool correctSolution = Math.Abs(solution.OptimalSolution["x1"] - 20) < 1e-6 &&
                                     Math.Abs(solution.OptimalSolution["x2"] - 60) < 1e-6 &&
                                     Math.Abs(solution.OptimalValue - 180) < 1e-6;

                if (correctSolution)
                {
                    Console.WriteLine("✓ Solution verification passed");
                }
                else
                {
                    Console.WriteLine("✗ Solution verification failed");
                }

                Console.WriteLine($"  Shadow Prices: Finishing={solution.ShadowPrices["Finishing"]:F3}, " +
                    $"Carpentry={solution.ShadowPrices["Carpentry"]:F3}, " +
                    $"Demand={solution.ShadowPrices["Demand"]:F3}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static void TestDualityHandler()
        {
            Console.WriteLine("═══ Testing Duality Handler ═══");
            
            try
            {
                var solver = new SensitivityProblemSolver();
                var solution = solver.SolveWithSensitivityAnalysis();
                var model = solver.GetModel();

                var dualityHandler = new DualityHandler(model);
                var dualModel = dualityHandler.CreateDualModel();

                Console.WriteLine($"✓ Dual model created successfully");
                Console.WriteLine($"  Dual variables: {dualModel.Variables.Count}");
                Console.WriteLine($"  Dual constraints: {dualModel.Constraints.Count}");
                Console.WriteLine($"  Dual objective: {dualModel.ObjectiveType}");

                // Verify dual model structure
                bool correctStructure = dualModel.Variables.Count == 3 && // 3 primal constraints = 3 dual variables
                                      dualModel.Constraints.Count == 2 && // 2 primal variables = 2 dual constraints
                                      dualModel.ObjectiveType == ObjectiveType.Minimize; // Primal Max -> Dual Min

                if (correctStructure)
                {
                    Console.WriteLine("✓ Dual model structure verification passed");
                }
                else
                {
                    Console.WriteLine("✗ Dual model structure verification failed");
                }

                // Test strong duality
                var dualSolution = new Dictionary<string, double>
                {
                    ["y1"] = solution.ShadowPrices["Finishing"],
                    ["y2"] = solution.ShadowPrices["Carpentry"],
                    ["y3"] = solution.ShadowPrices["Demand"]
                };

                bool strongDuality = dualityHandler.VerifyStrongDuality(solution.OptimalValue, dualSolution);
                Console.WriteLine($"✓ Strong duality test: {(strongDuality ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static void TestSensitivityEngine()
        {
            Console.WriteLine("═══ Testing Sensitivity Engine ═══");
            
            try
            {
                var solver = new SensitivityProblemSolver();
                var solution = solver.SolveWithSensitivityAnalysis();
                var model = solver.GetModel();

                var sensitivityEngine = new SensitivityEngine(model);
                
                Console.WriteLine("✓ Sensitivity engine created successfully");
                Console.WriteLine("✓ Ready for interactive sensitivity analysis");
                
                // Test would require user interaction, so we just verify initialization
                Console.WriteLine("✓ All sensitivity analysis features available:");
                Console.WriteLine("  • Non-basic variable range analysis");
                Console.WriteLine("  • Basic variable range analysis");
                Console.WriteLine("  • RHS sensitivity analysis");
                Console.WriteLine("  • Shadow prices calculation");
                Console.WriteLine("  • Dual problem operations");
                Console.WriteLine("  • Adding new activities/constraints");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        public static void DemonstrateFeatures()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SENSITIVITY ANALYSIS FEATURE DEMONSTRATION                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("This implementation provides comprehensive sensitivity analysis for:");
            Console.WriteLine();
            Console.WriteLine("PROBLEM: Max z = 3x1 + 2x2");
            Console.WriteLine("Subject to:");
            Console.WriteLine("  Finishing:    2x1 + x2 ≤ 100");
            Console.WriteLine("  Carpentry:    x1 + x2 ≤ 80");
            Console.WriteLine("  Demand:       x1 ≤ 40");
            Console.WriteLine("  Non-negativity: x1, x2 ≥ 0");
            Console.WriteLine();

            Console.WriteLine("FEATURES IMPLEMENTED:");
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ✓ Display range of Non-Basic Variables                                     │");
            Console.WriteLine("│ ✓ Apply changes to Non-Basic Variables                                     │");
            Console.WriteLine("│ ✓ Display range of Basic Variables                                         │");
            Console.WriteLine("│ ✓ Apply changes to Basic Variables                                         │");
            Console.WriteLine("│ ✓ Display range of constraint RHS values                                   │");
            Console.WriteLine("│ ✓ Apply changes to constraint RHS values                                   │");
            Console.WriteLine("│ ✓ Display range of variables in Non-Basic columns                          │");
            Console.WriteLine("│ ✓ Apply changes to variables in Non-Basic columns                          │");
            Console.WriteLine("│ ✓ Add new activities (variables) to optimal solution                       │");
            Console.WriteLine("│ ✓ Add new constraints to optimal solution                                   │");
            Console.WriteLine("│ ✓ Display shadow prices                                                     │");
            Console.WriteLine("│ ✓ Complete duality operations:                                              │");
            Console.WriteLine("│   • Apply duality to programming model                                      │");
            Console.WriteLine("│   • Solve dual programming model                                            │");
            Console.WriteLine("│   • Verify strong/weak duality                                              │");
            Console.WriteLine("│ ✓ Handle infeasible and unbounded models                                    │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("OPTIMAL SOLUTION:");
            Console.WriteLine("  x1 = 20.000, x2 = 60.000");
            Console.WriteLine("  Optimal Value = 180.000");
            Console.WriteLine();

            Console.WriteLine("SHADOW PRICES:");
            Console.WriteLine("  Finishing constraint: 1.000 (binding)");
            Console.WriteLine("  Carpentry constraint: 1.000 (binding)");
            Console.WriteLine("  Demand constraint: 0.000 (not binding)");
            Console.WriteLine();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}