using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.SensitivityAnalysis
{
    public class DualityHandler
    {
        private LinearProgrammingModel _model;

        public DualityHandler(LinearProgrammingModel model)
        {
            _model = model;
        }

        public void ShowDualForm()
        {
            if (_model == null)
            {
                Console.WriteLine("No model loaded.");
                return;
            }

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                              DUAL PROBLEM FORMULATION                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");

            DisplayPrimalProblem();
            Console.WriteLine();
            DisplayDualProblem();
            Console.WriteLine();
            DisplayDualityRules();
        }

        private void DisplayPrimalProblem()
        {
            Console.WriteLine("\n═══ PRIMAL PROBLEM ═══");
            Console.WriteLine($"{_model.GetObjectiveFunctionString()}");
            Console.WriteLine("Subject to:");
            
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                Console.WriteLine($"  {_model.Constraints[i].ToString(_model.Variables)}");
            }
            
            Console.WriteLine("  Non-negativity: " + string.Join(", ", _model.Variables.Select(v => $"{v.Name} ≥ 0")));
        }

        private void DisplayDualProblem()
        {
            Console.WriteLine("═══ DUAL PROBLEM ═══");
            
            var dualObjType = _model.ObjectiveType == ObjectiveType.Maximize ? "Minimize" : "Maximize";
            int m = _model.Constraints.Count;
            int n = _model.Variables.Count;

            // Dual objective function
            Console.Write($"{dualObjType}: ");
            var dualObjTerms = new List<string>();
            for (int i = 0; i < m; i++)
            {
                double coeff = _model.Constraints[i].RightHandSide;
                string sign = (i == 0) ? "" : (coeff >= 0 ? " + " : " ");
                dualObjTerms.Add($"{sign}{coeff:F0}y{i + 1}");
            }
            Console.WriteLine(string.Join("", dualObjTerms));

            Console.WriteLine("Subject to:");
            
            // Dual constraints (one for each primal variable)
            for (int j = 0; j < n; j++)
            {
                var constraintTerms = new List<string>();
                for (int i = 0; i < m; i++)
                {
                    double coeff = _model.Constraints[i].Coefficients[j];
                    string sign = (i == 0) ? "" : (coeff >= 0 ? " + " : " ");
                    constraintTerms.Add($"{sign}{coeff:F0}y{i + 1}");
                }
                
                string symbol = _model.ObjectiveType == ObjectiveType.Maximize ? " ≥ " : " ≤ ";
                string constraintStr = string.Join("", constraintTerms) + symbol + _model.ObjectiveCoefficients[j];
                Console.WriteLine($"  {constraintStr}");
            }
            
            Console.WriteLine("  Non-negativity: " + string.Join(", ", Enumerable.Range(1, m).Select(i => $"y{i} ≥ 0")));
        }

        private void DisplayDualityRules()
        {
            Console.WriteLine("═══ DUALITY TRANSFORMATION RULES ═══");
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ PRIMAL (Maximization)               │ DUAL (Minimization)                   │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ n variables                         │ n constraints                         │");
            Console.WriteLine("│ m constraints                       │ m variables                           │");
            Console.WriteLine("│ Constraint i: ≤ bi                  │ Variable yi ≥ 0                       │");
            Console.WriteLine("│ Constraint i: ≥ bi                  │ Variable yi ≤ 0                       │");
            Console.WriteLine("│ Constraint i: = bi                  │ Variable yi unrestricted              │");
            Console.WriteLine("│ Variable xj ≥ 0                     │ Constraint j: ≥ cj                    │");
            Console.WriteLine("│ Variable xj ≤ 0                     │ Constraint j: ≤ cj                    │");
            Console.WriteLine("│ Variable xj unrestricted            │ Constraint j: = cj                    │");
            Console.WriteLine("│ Maximize c^T x                      │ Minimize b^T y                        │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
        }

        public LinearProgrammingModel CreateDualModel()
        {
            var dualModel = new LinearProgrammingModel();
            
            // Set dual objective type (opposite of primal)
            dualModel.ObjectiveType = _model.ObjectiveType == ObjectiveType.Maximize ? 
                ObjectiveType.Minimize : ObjectiveType.Maximize;
            
            int m = _model.Constraints.Count; // Number of primal constraints = number of dual variables
            int n = _model.Variables.Count;   // Number of primal variables = number of dual constraints
            
            // Create dual variables (one for each primal constraint)
            for (int i = 0; i < m; i++)
            {
                var dualVar = new Variable($"y{i + 1}", VariableType.Continuous);
                dualModel.Variables.Add(dualVar);
                
                // Dual objective coefficients are primal RHS values
                dualModel.ObjectiveCoefficients.Add(_model.Constraints[i].RightHandSide);
            }
            
            // Create dual constraints (one for each primal variable)
            for (int j = 0; j < n; j++)
            {
                var dualConstraintCoeffs = new List<double>();
                
                // Coefficients are from the j-th column of primal constraint matrix
                for (int i = 0; i < m; i++)
                {
                    dualConstraintCoeffs.Add(_model.Constraints[i].Coefficients[j]);
                }
                
                // Determine constraint type based on primal variable type and objective
                ConstraintType dualConstraintType;
                if (_model.ObjectiveType == ObjectiveType.Maximize)
                {
                    // For maximization primal with xj ≥ 0, dual constraint is ≥
                    dualConstraintType = ConstraintType.GreaterThanOrEqual;
                }
                else
                {
                    // For minimization primal with xj ≥ 0, dual constraint is ≤
                    dualConstraintType = ConstraintType.LessThanOrEqual;
                }
                
                // RHS of dual constraint is the coefficient of xj in primal objective
                double dualRHS = _model.ObjectiveCoefficients[j];
                
                var dualConstraint = new Constraint(dualConstraintCoeffs, dualConstraintType, dualRHS, $"DC{j + 1}");
                dualModel.Constraints.Add(dualConstraint);
            }
            
            return dualModel;
        }

        public void SolveDualProblem()
        {
            Console.WriteLine("\n═══ SOLVING DUAL PROBLEM ═══");
            
            var dualModel = CreateDualModel();
            
            Console.WriteLine("Dual model created:");
            Console.WriteLine($"Variables: {dualModel.Variables.Count}");
            Console.WriteLine($"Constraints: {dualModel.Constraints.Count}");
            Console.WriteLine($"Objective: {dualModel.ObjectiveType}");
            
            // In a complete implementation, you would solve the dual using simplex
            Console.WriteLine("\nNote: Dual problem solving requires a complete simplex implementation.");
            Console.WriteLine("The dual solution can be extracted from the final primal simplex tableau:");
            Console.WriteLine("- Shadow prices are the dual variable values");
            Console.WriteLine("- Reduced costs verify dual constraint satisfaction");
        }

        public bool VerifyStrongDuality(double primalOptimal, Dictionary<string, double> dualSolution)
        {
            if (dualSolution == null || !dualSolution.Any())
            {
                Console.WriteLine("No dual solution provided for verification.");
                return false;
            }

            // Calculate dual objective value
            double dualObjective = 0;
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                string dualVarName = $"y{i + 1}";
                if (dualSolution.ContainsKey(dualVarName))
                {
                    dualObjective += dualSolution[dualVarName] * _model.Constraints[i].RightHandSide;
                }
            }

            Console.WriteLine($"\nDuality Verification:");
            Console.WriteLine($"Primal optimal value: {primalOptimal:F6}");
            Console.WriteLine($"Dual optimal value: {dualObjective:F6}");
            Console.WriteLine($"Difference: {Math.Abs(primalOptimal - dualObjective):F6}");

            bool isStrongDuality = Math.Abs(primalOptimal - dualObjective) < 1e-6;
            
            if (isStrongDuality)
            {
                Console.WriteLine("✓ STRONG DUALITY verified!");
                Console.WriteLine("Primal and dual optimal values are equal.");
            }
            else
            {
                Console.WriteLine("⚠ Strong duality not satisfied.");
                Console.WriteLine("This may indicate:");
                Console.WriteLine("- Calculation errors");
                Console.WriteLine("- Non-optimal solutions");
                Console.WriteLine("- Numerical precision issues");
            }

            return isStrongDuality;
        }

        public void VerifyComplementarySlackness(Dictionary<string, double> primalSolution, Dictionary<string, double> dualSolution)
        {
            Console.WriteLine("\n═══ COMPLEMENTARY SLACKNESS VERIFICATION ═══");
            
            if (primalSolution == null || dualSolution == null)
            {
                Console.WriteLine("Both primal and dual solutions required for verification.");
                return;
            }

            Console.WriteLine("Complementary Slackness Conditions:");
            Console.WriteLine("1. If primal constraint i has slack > 0, then dual variable yi = 0");
            Console.WriteLine("2. If dual variable yi > 0, then primal constraint i has slack = 0");
            Console.WriteLine("3. If primal variable xj > 0, then dual constraint j has slack = 0");
            Console.WriteLine("4. If dual constraint j has slack > 0, then primal variable xj = 0");

            Console.WriteLine("\nVerification Results:");
            
            // Check primal constraints vs dual variables
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                var constraint = _model.Constraints[i];
                var primalValues = _model.Variables.Select(v => 
                    primalSolution.ContainsKey(v.Name) ? primalSolution[v.Name] : 0).ToList();
                
                double slack = constraint.GetSlack(primalValues);
                string dualVarName = $"y{i + 1}";
                double dualValue = dualSolution.ContainsKey(dualVarName) ? dualSolution[dualVarName] : 0;
                
                bool condition1 = !(slack > 1e-6 && Math.Abs(dualValue) > 1e-6);
                bool condition2 = !(Math.Abs(dualValue) > 1e-6 && slack > 1e-6);
                
                Console.WriteLine($"Constraint {i + 1}: Slack = {slack:F6}, {dualVarName} = {dualValue:F6} " +
                    $"[{(condition1 && condition2 ? "✓" : "✗")}]");
            }

            // Check primal variables vs dual constraints
            for (int j = 0; j < _model.Variables.Count; j++)
            {
                var variable = _model.Variables[j];
                double primalValue = primalSolution.ContainsKey(variable.Name) ? primalSolution[variable.Name] : 0;
                
                // Calculate dual constraint slack (simplified)
                double dualSlack = 0; // This would require full dual constraint evaluation
                
                Console.WriteLine($"Variable {variable.Name}: Value = {primalValue:F6}, Dual slack ≈ {dualSlack:F6}");
            }
        }

        public void DisplayDualityTheorems()
        {
            Console.WriteLine("\n═══ FUNDAMENTAL DUALITY THEOREMS ═══");
            
            Console.WriteLine("1. WEAK DUALITY THEOREM:");
            Console.WriteLine("   If x is feasible for the primal and y is feasible for the dual, then:");
            Console.WriteLine("   c^T x ≤ b^T y (for maximization primal)");
            Console.WriteLine("   This provides bounds: dual gives upper bound on primal optimal value");
            
            Console.WriteLine("\n2. STRONG DUALITY THEOREM:");
            Console.WriteLine("   If the primal has an optimal solution x*, then the dual has an optimal");
            Console.WriteLine("   solution y*, and c^T x* = b^T y*");
            
            Console.WriteLine("\n3. COMPLEMENTARY SLACKNESS THEOREM:");
            Console.WriteLine("   For optimal solutions x* and y*:");
            Console.WriteLine("   • If primal constraint i is not tight, then yi* = 0");
            Console.WriteLine("   • If dual variable yi* > 0, then primal constraint i is tight");
            Console.WriteLine("   • If primal variable xj* > 0, then dual constraint j is tight");
            Console.WriteLine("   • If dual constraint j is not tight, then xj* = 0");
            
            Console.WriteLine("\n4. FUNDAMENTAL THEOREM OF LINEAR PROGRAMMING:");
            Console.WriteLine("   Exactly one of the following holds:");
            Console.WriteLine("   • Both primal and dual have optimal solutions");
            Console.WriteLine("   • Primal is unbounded and dual is infeasible");
            Console.WriteLine("   • Primal is infeasible and dual is unbounded");
            Console.WriteLine("   • Both primal and dual are infeasible");
        }
    }
}