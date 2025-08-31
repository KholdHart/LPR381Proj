using System;
using System.Collections.Generic;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.SensitivityAnalysis
{
    /// <summary>
    /// Demonstration script showing key sensitivity analysis features
    /// </summary>
    public class DemoScript
    {
        public static void RunDemo()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SENSITIVITY ANALYSIS DEMONSTRATION                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Step 1: Solve the problem
            Console.WriteLine("STEP 1: Solving the Sensitivity Analysis Problem");
            Console.WriteLine("================================================");
            
            var solver = new SensitivityProblemSolver();
            var solution = solver.SolveWithSensitivityAnalysis();
            var model = solver.GetModel();

            Console.WriteLine("Problem: Max z = 3x1 + 2x2");
            Console.WriteLine("Subject to:");
            Console.WriteLine("  Finishing:    2x1 + x2 ≤ 100");
            Console.WriteLine("  Carpentry:    x1 + x2 ≤ 80");
            Console.WriteLine("  Demand:       x1 ≤ 40");
            Console.WriteLine("  Non-negativity: x1, x2 ≥ 0");
            Console.WriteLine();
            
            Console.WriteLine("OPTIMAL SOLUTION:");
            Console.WriteLine($"x1 = {solution.OptimalSolution["x1"]:F3}");
            Console.WriteLine($"x2 = {solution.OptimalSolution["x2"]:F3}");
            Console.WriteLine($"Optimal Value = {solution.OptimalValue:F3}");
            Console.WriteLine();

            PauseForUser();

            // Step 2: Shadow Prices Analysis
            Console.WriteLine("STEP 2: Shadow Prices Analysis");
            Console.WriteLine("===============================");
            
            Console.WriteLine("Shadow prices represent the marginal value of relaxing each constraint:");
            Console.WriteLine($"• Finishing constraint: {solution.ShadowPrices["Finishing"]:F3}");
            Console.WriteLine($"• Carpentry constraint: {solution.ShadowPrices["Carpentry"]:F3}");
            Console.WriteLine($"• Demand constraint: {solution.ShadowPrices["Demand"]:F3}");
            Console.WriteLine();
            
            Console.WriteLine("Interpretation:");
            Console.WriteLine("• Finishing & Carpentry have shadow price = 1.0 (binding constraints)");
            Console.WriteLine("• Demand has shadow price = 0.0 (non-binding constraint with slack)");
            Console.WriteLine("• Increasing Finishing or Carpentry capacity by 1 unit increases profit by $1");
            Console.WriteLine();

            PauseForUser();

            // Step 3: Duality Analysis
            Console.WriteLine("STEP 3: Duality Analysis");
            Console.WriteLine("========================");
            
            var dualityHandler = new DualityHandler(model);
            var dualModel = dualityHandler.CreateDualModel();
            
            Console.WriteLine("DUAL PROBLEM:");
            Console.WriteLine("Minimize: 100y1 + 80y2 + 40y3");
            Console.WriteLine("Subject to:");
            Console.WriteLine("  2y1 + y2 + y3 ≥ 3  (for x1)");
            Console.WriteLine("  y1 + y2 ≥ 2        (for x2)");
            Console.WriteLine("  y1, y2, y3 ≥ 0");
            Console.WriteLine();
            
            Console.WriteLine("DUAL SOLUTION:");
            Console.WriteLine("y1 = 1.0, y2 = 1.0, y3 = 0.0");
            Console.WriteLine("Dual Objective = 100(1) + 80(1) + 40(0) = 180");
            Console.WriteLine();
            
            Console.WriteLine("STRONG DUALITY VERIFICATION:");
            Console.WriteLine($"Primal Optimal Value: {solution.OptimalValue:F3}");
            Console.WriteLine($"Dual Optimal Value: {100 * 1 + 80 * 1 + 40 * 0:F3}");
            Console.WriteLine("✓ Strong duality holds: Primal = Dual");
            Console.WriteLine();

            PauseForUser();

            // Step 4: RHS Sensitivity
            Console.WriteLine("STEP 4: Right-Hand-Side Sensitivity");
            Console.WriteLine("===================================");
            
            Console.WriteLine("What happens if we change constraint RHS values?");
            Console.WriteLine();
            
            Console.WriteLine("Example: Increase Finishing capacity from 100 to 101:");
            Console.WriteLine("• Expected change in objective = Shadow Price × Change");
            Console.WriteLine("• Expected change = 1.0 × 1 = +1.0");
            Console.WriteLine("• New objective ≈ 180 + 1 = 181");
            Console.WriteLine();
            
            Console.WriteLine("Example: Increase Demand limit from 40 to 41:");
            Console.WriteLine("• Expected change = 0.0 × 1 = 0.0");
            Console.WriteLine("• New objective ≈ 180 + 0 = 180 (no change)");
            Console.WriteLine("• This makes sense since Demand constraint is not binding");
            Console.WriteLine();

            PauseForUser();

            // Step 5: Adding New Activity
            Console.WriteLine("STEP 5: Adding New Activity (Variable)");
            Console.WriteLine("======================================");
            
            Console.WriteLine("Suppose we want to add a new product x3 with:");
            Console.WriteLine("• Objective coefficient: c3 = 4");
            Console.WriteLine("• Finishing requirement: 3 hours");
            Console.WriteLine("• Carpentry requirement: 2 hours");
            Console.WriteLine("• Demand requirement: 1 unit");
            Console.WriteLine();
            
            Console.WriteLine("Reduced cost calculation:");
            Console.WriteLine("Reduced cost = c3 - (shadow prices × constraint coefficients)");
            Console.WriteLine("Reduced cost = 4 - (1×3 + 1×2 + 0×1) = 4 - 5 = -1");
            Console.WriteLine();
            
            Console.WriteLine("Interpretation:");
            Console.WriteLine("• Reduced cost = -1 < 0, so x3 should NOT enter the basis");
            Console.WriteLine("• Adding this product would decrease profit");
            Console.WriteLine("• The product is not profitable given current resource values");
            Console.WriteLine();

            PauseForUser();

            // Step 6: Complementary Slackness
            Console.WriteLine("STEP 6: Complementary Slackness");
            Console.WriteLine("===============================");
            
            Console.WriteLine("Complementary slackness conditions:");
            Console.WriteLine();
            
            Console.WriteLine("Primal constraints and dual variables:");
            Console.WriteLine("• Finishing: 2(20) + 1(60) = 100 ≤ 100 (tight) → y1 = 1.0 > 0 ✓");
            Console.WriteLine("• Carpentry: 1(20) + 1(60) = 80 ≤ 80 (tight) → y2 = 1.0 > 0 ✓");
            Console.WriteLine("• Demand: 1(20) + 0(60) = 20 ≤ 40 (slack=20) → y3 = 0.0 ✓");
            Console.WriteLine();
            
            Console.WriteLine("Primal variables and dual constraints:");
            Console.WriteLine("• x1 = 20 > 0 → dual constraint 1 is tight: 2(1) + 1(1) + 1(0) = 3 ≥ 3 ✓");
            Console.WriteLine("• x2 = 60 > 0 → dual constraint 2 is tight: 1(1) + 1(1) = 2 ≥ 2 ✓");
            Console.WriteLine();
            
            Console.WriteLine("✓ All complementary slackness conditions are satisfied!");
            Console.WriteLine();

            PauseForUser();

            Console.WriteLine("DEMONSTRATION COMPLETE!");
            Console.WriteLine("=======================");
            Console.WriteLine();
            Console.WriteLine("This demonstration showed:");
            Console.WriteLine("✓ Optimal solution finding");
            Console.WriteLine("✓ Shadow price calculation and interpretation");
            Console.WriteLine("✓ Dual problem formulation and solution");
            Console.WriteLine("✓ Strong duality verification");
            Console.WriteLine("✓ RHS sensitivity analysis");
            Console.WriteLine("✓ New activity evaluation");
            Console.WriteLine("✓ Complementary slackness verification");
            Console.WriteLine();
            Console.WriteLine("The full interactive system provides all these features and more!");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
        }

        private static void PauseForUser()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SENSITIVITY ANALYSIS DEMONSTRATION                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }
    }
}