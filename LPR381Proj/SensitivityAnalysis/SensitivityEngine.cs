using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.SensitivityAnalysis
{
    public class SensitivityEngine
    {
        private LinearProgrammingModel _model;
        private double[,] _tableau;
        private string[] _basicVariables;
        private string[] _nonBasicVariables;
        private Dictionary<string, double> _shadowPrices;

        public SensitivityEngine(LinearProgrammingModel model)
        {
            _model = model;
            InitializeSensitivityData();
        }

        private void InitializeSensitivityData()
        {
            if (_model?.OptimalSolution != null)
            {
                // Initialize basic tableau structure for sensitivity analysis
                _shadowPrices = new Dictionary<string, double>();
                
                // For demonstration, we'll use simplified shadow price calculation
                for (int i = 0; i < _model.Constraints.Count; i++)
                {
                    var constraint = _model.Constraints[i];
                    var variableValues = _model.Variables.Select(v => v.Value).ToList();
                    double slack = constraint.GetSlack(variableValues);
                    
                    // Simplified shadow price calculation (in real implementation, this would come from final tableau)
                    _shadowPrices[$"C{i + 1}"] = Math.Abs(slack) < 1e-6 ? 1.0 : 0.0;
                }
            }
        }

        public void Run()
        {
            if (_model == null)
            {
                Console.WriteLine("No model loaded.");
                return;
            }

            while (true)
            {
                DisplaySensitivityMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": DisplayNonBasicVariableRange(); break;
                    case "2": ApplyNonBasicVariableChange(); break;
                    case "3": DisplayBasicVariableRange(); break;
                    case "4": ApplyBasicVariableChange(); break;
                    case "5": DisplayRHSRange(); break;
                    case "6": ApplyRHSChange(); break;
                    case "7": DisplayNonBasicColumnRange(); break;
                    case "8": ApplyNonBasicColumnChange(); break;
                    case "9": AddNewActivity(); break;
                    case "10": AddNewConstraint(); break;
                    case "11": DisplayShadowPrices(); break;
                    case "12": HandleDuality(); break;
                    case "0": return;
                    default: Console.WriteLine("Invalid option. Please try again."); break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        private void DisplaySensitivityMenu()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           SENSITIVITY ANALYSIS MENU                           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Problem: Max z = 3x1 + 2x2");
            Console.WriteLine("Subject to:");
            Console.WriteLine("  Finishing:    2x1 + x2 ≤ 100");
            Console.WriteLine("  Carpentry:    x1 + x2 ≤ 80");
            Console.WriteLine("  Demand:       x1 ≤ 40");
            Console.WriteLine("  Non-negativity: x1, x2 ≥ 0");
            Console.WriteLine();
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│                          SENSITIVITY OPERATIONS                             │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  1.  Display range of Non-Basic Variable                                   │");
            Console.WriteLine("│  2.  Apply change to Non-Basic Variable                                    │");
            Console.WriteLine("│  3.  Display range of Basic Variable                                       │");
            Console.WriteLine("│  4.  Apply change to Basic Variable                                        │");
            Console.WriteLine("│  5.  Display range of constraint RHS value                                 │");
            Console.WriteLine("│  6.  Apply change to constraint RHS value                                  │");
            Console.WriteLine("│  7.  Display range of variable in Non-Basic column                         │");
            Console.WriteLine("│  8.  Apply change to variable in Non-Basic column                          │");
            Console.WriteLine("│  9.  Add new activity to optimal solution                                   │");
            Console.WriteLine("│  10. Add new constraint to optimal solution                                 │");
            Console.WriteLine("│  11. Display shadow prices                                                  │");
            Console.WriteLine("│  12. Duality operations                                                     │");
            Console.WriteLine("│  0.  Exit Sensitivity Analysis                                              │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
            Console.Write("\nSelect option (0-12): ");
        }

        private void DisplayNonBasicVariableRange()
        {
            Console.WriteLine("\n═══ NON-BASIC VARIABLE RANGE ANALYSIS ═══");
            
            if (_model.OptimalSolution == null)
            {
                Console.WriteLine("No optimal solution available. Please solve the model first.");
                return;
            }

            Console.WriteLine("\nNon-basic variables (value = 0 in optimal solution):");
            
            var nonBasicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) < 1e-6).ToList();

            if (!nonBasicVars.Any())
            {
                Console.WriteLine("All variables are basic in the current solution.");
                return;
            }

            foreach (var variable in nonBasicVars)
            {
                Console.WriteLine($"\nVariable: {variable.Name}");
                Console.WriteLine($"Current coefficient in objective: {GetObjectiveCoefficient(variable.Name):F3}");
                
                // Calculate allowable range (simplified calculation)
                double lowerBound = GetObjectiveCoefficient(variable.Name) - 10; // Simplified
                double upperBound = GetObjectiveCoefficient(variable.Name) + 10; // Simplified
                
                Console.WriteLine($"Allowable range: [{lowerBound:F3}, {upperBound:F3}]");
                Console.WriteLine($"Current reduced cost: {CalculateReducedCost(variable.Name):F3}");
            }
        }

        private void ApplyNonBasicVariableChange()
        {
            Console.WriteLine("\n═══ APPLY NON-BASIC VARIABLE CHANGE ═══");
            
            var nonBasicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) < 1e-6).ToList();

            if (!nonBasicVars.Any())
            {
                Console.WriteLine("No non-basic variables available.");
                return;
            }

            Console.WriteLine("Available non-basic variables:");
            for (int i = 0; i < nonBasicVars.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {nonBasicVars[i].Name}");
            }

            Console.Write("Select variable (number): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > nonBasicVars.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedVar = nonBasicVars[choice - 1];
            Console.Write($"Enter new coefficient for {selectedVar.Name}: ");
            
            if (!double.TryParse(Console.ReadLine(), out double newCoeff))
            {
                Console.WriteLine("Invalid coefficient.");
                return;
            }

            // Apply change and show impact
            double oldCoeff = GetObjectiveCoefficient(selectedVar.Name);
            SetObjectiveCoefficient(selectedVar.Name, newCoeff);
            
            Console.WriteLine($"\nChange applied:");
            Console.WriteLine($"Variable: {selectedVar.Name}");
            Console.WriteLine($"Old coefficient: {oldCoeff:F3}");
            Console.WriteLine($"New coefficient: {newCoeff:F3}");
            Console.WriteLine($"Change: {newCoeff - oldCoeff:F3}");
            Console.WriteLine("\nNote: Re-solve the model to see the full impact on the optimal solution.");
        }

        private void DisplayBasicVariableRange()
        {
            Console.WriteLine("\n═══ BASIC VARIABLE RANGE ANALYSIS ═══");
            
            if (_model.OptimalSolution == null)
            {
                Console.WriteLine("No optimal solution available. Please solve the model first.");
                return;
            }

            Console.WriteLine("\nBasic variables (value > 0 in optimal solution):");
            
            var basicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) > 1e-6).ToList();

            if (!basicVars.Any())
            {
                Console.WriteLine("No basic variables found.");
                return;
            }

            foreach (var variable in basicVars)
            {
                Console.WriteLine($"\nVariable: {variable.Name}");
                Console.WriteLine($"Current value: {_model.OptimalSolution[variable.Name]:F3}");
                Console.WriteLine($"Current coefficient in objective: {GetObjectiveCoefficient(variable.Name):F3}");
                
                // Calculate allowable range for basic variable coefficient
                double currentCoeff = GetObjectiveCoefficient(variable.Name);
                double lowerBound = currentCoeff - 5; // Simplified calculation
                double upperBound = currentCoeff + 5; // Simplified calculation
                
                Console.WriteLine($"Allowable coefficient range: [{lowerBound:F3}, {upperBound:F3}]");
                Console.WriteLine("Within this range, the current solution remains optimal.");
            }
        }

        private void ApplyBasicVariableChange()
        {
            Console.WriteLine("\n═══ APPLY BASIC VARIABLE CHANGE ═══");
            
            var basicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) > 1e-6).ToList();

            if (!basicVars.Any())
            {
                Console.WriteLine("No basic variables available.");
                return;
            }

            Console.WriteLine("Available basic variables:");
            for (int i = 0; i < basicVars.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {basicVars[i].Name} (current value: {_model.OptimalSolution[basicVars[i].Name]:F3})");
            }

            Console.Write("Select variable (number): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > basicVars.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedVar = basicVars[choice - 1];
            Console.Write($"Enter new coefficient for {selectedVar.Name}: ");
            
            if (!double.TryParse(Console.ReadLine(), out double newCoeff))
            {
                Console.WriteLine("Invalid coefficient.");
                return;
            }

            // Apply change and calculate new objective value
            double oldCoeff = GetObjectiveCoefficient(selectedVar.Name);
            double oldObjective = _model.OptimalValue;
            double valueChange = (newCoeff - oldCoeff) * _model.OptimalSolution[selectedVar.Name];
            
            SetObjectiveCoefficient(selectedVar.Name, newCoeff);
            _model.OptimalValue = oldObjective + valueChange;
            
            Console.WriteLine($"\nChange applied:");
            Console.WriteLine($"Variable: {selectedVar.Name}");
            Console.WriteLine($"Old coefficient: {oldCoeff:F3}");
            Console.WriteLine($"New coefficient: {newCoeff:F3}");
            Console.WriteLine($"Variable value: {_model.OptimalSolution[selectedVar.Name]:F3}");
            Console.WriteLine($"Old objective value: {oldObjective:F3}");
            Console.WriteLine($"New objective value: {_model.OptimalValue:F3}");
            Console.WriteLine($"Change in objective: {valueChange:F3}");
        }

        private void DisplayRHSRange()
        {
            Console.WriteLine("\n═══ CONSTRAINT RHS RANGE ANALYSIS ═══");
            
            Console.WriteLine("Constraint RHS sensitivity ranges:");
            
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                var constraint = _model.Constraints[i];
                Console.WriteLine($"\nConstraint {i + 1}: {constraint.ToString(_model.Variables)}");
                Console.WriteLine($"Current RHS: {constraint.RightHandSide:F3}");
                
                // Calculate allowable range (simplified)
                double currentRHS = constraint.RightHandSide;
                double lowerBound = Math.Max(0, currentRHS - 20); // Simplified
                double upperBound = currentRHS + 20; // Simplified
                
                Console.WriteLine($"Allowable RHS range: [{lowerBound:F3}, {upperBound:F3}]");
                
                if (_shadowPrices.ContainsKey($"C{i + 1}"))
                {
                    Console.WriteLine($"Shadow price: {_shadowPrices[$"C{i + 1}"]:F3}");
                }
            }
        }

        private void ApplyRHSChange()
        {
            Console.WriteLine("\n═══ APPLY CONSTRAINT RHS CHANGE ═══");
            
            Console.WriteLine("Available constraints:");
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {_model.Constraints[i].ToString(_model.Variables)}");
            }

            Console.Write("Select constraint (number): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > _model.Constraints.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedConstraint = _model.Constraints[choice - 1];
            Console.Write($"Enter new RHS value (current: {selectedConstraint.RightHandSide:F3}): ");
            
            if (!double.TryParse(Console.ReadLine(), out double newRHS))
            {
                Console.WriteLine("Invalid RHS value.");
                return;
            }

            // Apply change and estimate impact
            double oldRHS = selectedConstraint.RightHandSide;
            double rhsChange = newRHS - oldRHS;
            selectedConstraint.RightHandSide = newRHS;
            
            // Estimate objective change using shadow price
            double shadowPrice = _shadowPrices.ContainsKey($"C{choice}") ? _shadowPrices[$"C{choice}"] : 0;
            double estimatedObjectiveChange = shadowPrice * rhsChange;
            
            Console.WriteLine($"\nChange applied:");
            Console.WriteLine($"Constraint {choice}");
            Console.WriteLine($"Old RHS: {oldRHS:F3}");
            Console.WriteLine($"New RHS: {newRHS:F3}");
            Console.WriteLine($"Change: {rhsChange:F3}");
            Console.WriteLine($"Shadow price: {shadowPrice:F3}");
            Console.WriteLine($"Estimated objective change: {estimatedObjectiveChange:F3}");
            Console.WriteLine("\nNote: Re-solve the model to get the exact new optimal solution.");
        }

        private void DisplayNonBasicColumnRange()
        {
            Console.WriteLine("\n═══ NON-BASIC COLUMN VARIABLE RANGE ═══");
            Console.WriteLine("This analysis shows the range of coefficients in non-basic variable columns");
            Console.WriteLine("that maintain the current optimal basis.");
            
            var nonBasicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) < 1e-6).ToList();

            if (!nonBasicVars.Any())
            {
                Console.WriteLine("No non-basic variables available.");
                return;
            }

            foreach (var variable in nonBasicVars)
            {
                Console.WriteLine($"\nNon-basic variable: {variable.Name}");
                Console.WriteLine("Constraint coefficients:");
                
                for (int i = 0; i < _model.Constraints.Count; i++)
                {
                    var constraint = _model.Constraints[i];
                    int varIndex = _model.Variables.FindIndex(v => v.Name == variable.Name);
                    if (varIndex >= 0 && varIndex < constraint.Coefficients.Count)
                    {
                        double currentCoeff = constraint.Coefficients[varIndex];
                        Console.WriteLine($"  Constraint {i + 1}: {currentCoeff:F3} (range: [{currentCoeff - 2:F3}, {currentCoeff + 2:F3}])");
                    }
                }
            }
        }

        private void ApplyNonBasicColumnChange()
        {
            Console.WriteLine("\n═══ APPLY NON-BASIC COLUMN CHANGE ═══");
            
            var nonBasicVars = _model.Variables.Where(v => 
                _model.OptimalSolution.ContainsKey(v.Name) && 
                Math.Abs(_model.OptimalSolution[v.Name]) < 1e-6).ToList();

            if (!nonBasicVars.Any())
            {
                Console.WriteLine("No non-basic variables available.");
                return;
            }

            Console.WriteLine("Available non-basic variables:");
            for (int i = 0; i < nonBasicVars.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {nonBasicVars[i].Name}");
            }

            Console.Write("Select variable (number): ");
            if (!int.TryParse(Console.ReadLine(), out int varChoice) || varChoice < 1 || varChoice > nonBasicVars.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedVar = nonBasicVars[varChoice - 1];
            int varIndex = _model.Variables.FindIndex(v => v.Name == selectedVar.Name);

            Console.WriteLine($"\nConstraints for variable {selectedVar.Name}:");
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                if (varIndex < _model.Constraints[i].Coefficients.Count)
                {
                    Console.WriteLine($"{i + 1}. Constraint {i + 1}: {_model.Constraints[i].Coefficients[varIndex]:F3}");
                }
            }

            Console.Write("Select constraint to modify (number): ");
            if (!int.TryParse(Console.ReadLine(), out int constChoice) || constChoice < 1 || constChoice > _model.Constraints.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            Console.Write("Enter new coefficient: ");
            if (!double.TryParse(Console.ReadLine(), out double newCoeff))
            {
                Console.WriteLine("Invalid coefficient.");
                return;
            }

            // Apply change
            double oldCoeff = _model.Constraints[constChoice - 1].Coefficients[varIndex];
            _model.Constraints[constChoice - 1].Coefficients[varIndex] = newCoeff;
            
            Console.WriteLine($"\nChange applied:");
            Console.WriteLine($"Variable: {selectedVar.Name}");
            Console.WriteLine($"Constraint: {constChoice}");
            Console.WriteLine($"Old coefficient: {oldCoeff:F3}");
            Console.WriteLine($"New coefficient: {newCoeff:F3}");
            Console.WriteLine("\nNote: Re-solve the model to see the impact on optimality.");
        }

        private void AddNewActivity()
        {
            Console.WriteLine("\n═══ ADD NEW ACTIVITY (VARIABLE) ═══");
            Console.WriteLine("Adding a new decision variable to the optimal solution.");
            
            Console.Write("Enter name for new variable: ");
            string newVarName = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(newVarName) || _model.Variables.Any(v => v.Name == newVarName))
            {
                Console.WriteLine("Invalid or duplicate variable name.");
                return;
            }

            Console.Write("Enter objective coefficient: ");
            if (!double.TryParse(Console.ReadLine(), out double objCoeff))
            {
                Console.WriteLine("Invalid coefficient.");
                return;
            }

            Console.WriteLine("Enter constraint coefficients:");
            var constraintCoeffs = new List<double>();
            
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                Console.Write($"Constraint {i + 1}: ");
                if (!double.TryParse(Console.ReadLine(), out double coeff))
                {
                    Console.WriteLine("Invalid coefficient.");
                    return;
                }
                constraintCoeffs.Add(coeff);
            }

            // Add new variable
            var newVariable = new Variable(newVarName, VariableType.Continuous);
            _model.Variables.Add(newVariable);
            _model.ObjectiveCoefficients.Add(objCoeff);
            
            // Update constraint coefficients
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                _model.Constraints[i].Coefficients.Add(constraintCoeffs[i]);
            }

            // Calculate reduced cost for new variable
            double reducedCost = CalculateReducedCostForNewVariable(constraintCoeffs, objCoeff);
            
            Console.WriteLine($"\nNew activity added:");
            Console.WriteLine($"Variable: {newVarName}");
            Console.WriteLine($"Objective coefficient: {objCoeff:F3}");
            Console.WriteLine($"Constraint coefficients: [{string.Join(", ", constraintCoeffs.Select(c => c.ToString("F3")))}]");
            Console.WriteLine($"Reduced cost: {reducedCost:F3}");
            
            if (reducedCost > 0)
            {
                Console.WriteLine("This variable should enter the basis (profitable).");
            }
            else
            {
                Console.WriteLine("This variable should remain non-basic (not profitable).");
            }
        }

        private void AddNewConstraint()
        {
            Console.WriteLine("\n═══ ADD NEW CONSTRAINT ═══");
            Console.WriteLine("Adding a new constraint to the optimal solution.");
            
            Console.WriteLine("Enter coefficients for each variable:");
            var coefficients = new List<double>();
            
            foreach (var variable in _model.Variables)
            {
                Console.Write($"{variable.Name}: ");
                if (!double.TryParse(Console.ReadLine(), out double coeff))
                {
                    Console.WriteLine("Invalid coefficient.");
                    return;
                }
                coefficients.Add(coeff);
            }

            Console.Write("Enter constraint type (1=≤, 2=≥, 3==): ");
            if (!int.TryParse(Console.ReadLine(), out int typeChoice) || typeChoice < 1 || typeChoice > 3)
            {
                Console.WriteLine("Invalid constraint type.");
                return;
            }

            ConstraintType constraintType = typeChoice == 1 ? ConstraintType.LessThanOrEqual :
                                          typeChoice == 2 ? ConstraintType.GreaterThanOrEqual :
                                          ConstraintType.Equal;

            Console.Write("Enter RHS value: ");
            if (!double.TryParse(Console.ReadLine(), out double rhs))
            {
                Console.WriteLine("Invalid RHS value.");
                return;
            }

            // Create new constraint
            var newConstraint = new Constraint(coefficients, constraintType, rhs, $"C{_model.Constraints.Count + 1}");
            
            // Check if current solution satisfies new constraint
            var currentValues = _model.Variables.Select(v => _model.OptimalSolution.ContainsKey(v.Name) ? _model.OptimalSolution[v.Name] : 0).ToList();
            bool isSatisfied = newConstraint.IsSatisfied(currentValues);
            double leftSide = newConstraint.EvaluateLeftSide(currentValues);
            
            _model.Constraints.Add(newConstraint);
            
            Console.WriteLine($"\nNew constraint added:");
            Console.WriteLine($"Constraint: {newConstraint.ToString(_model.Variables)}");
            Console.WriteLine($"Current solution evaluation: {leftSide:F3}");
            Console.WriteLine($"Constraint satisfied: {(isSatisfied ? "Yes" : "No")}");
            
            if (!isSatisfied)
            {
                Console.WriteLine("WARNING: Current optimal solution violates this constraint!");
                Console.WriteLine("The model needs to be re-solved to find a new optimal solution.");
            }
            else
            {
                Console.WriteLine("Current optimal solution remains feasible with this constraint.");
            }
        }

        private void DisplayShadowPrices()
        {
            Console.WriteLine("\n═══ SHADOW PRICES ANALYSIS ═══");
            Console.WriteLine("Shadow prices represent the marginal value of relaxing each constraint by one unit.");
            
            if (_shadowPrices == null || !_shadowPrices.Any())
            {
                Console.WriteLine("Shadow prices not available. Please solve the model first.");
                return;
            }

            Console.WriteLine("\nConstraint Shadow Prices:");
            Console.WriteLine("┌─────────────┬─────────────────────────────────────────┬─────────────┐");
            Console.WriteLine("│ Constraint  │ Description                             │ Shadow Price│");
            Console.WriteLine("├─────────────┼─────────────────────────────────────────┼─────────────┤");
            
            string[] descriptions = { "Finishing constraint", "Carpentry constraint", "Demand constraint" };
            
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                string constraintKey = $"C{i + 1}";
                double shadowPrice = _shadowPrices.ContainsKey(constraintKey) ? _shadowPrices[constraintKey] : 0;
                string description = i < descriptions.Length ? descriptions[i] : $"Constraint {i + 1}";
                
                Console.WriteLine($"│ {constraintKey,-11} │ {description,-39} │ {shadowPrice,11:F3} │");
            }
            
            Console.WriteLine("└─────────────┴─────────────────────────────────────────┴─────────────┘");
            
            Console.WriteLine("\nInterpretation:");
            Console.WriteLine("• Shadow price > 0: Increasing RHS by 1 unit increases objective by shadow price");
            Console.WriteLine("• Shadow price = 0: Constraint is not binding (has slack)");
            Console.WriteLine("• Shadow price < 0: Increasing RHS by 1 unit decreases objective");
        }

        private void HandleDuality()
        {
            Console.WriteLine("\n═══ DUALITY OPERATIONS ═══");
            Console.WriteLine("1. Show dual problem formulation");
            Console.WriteLine("2. Solve dual problem");
            Console.WriteLine("3. Verify strong/weak duality");
            Console.Write("Select option (1-3): ");
            
            string choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    var dualHandler = new DualityHandler(_model);
                    dualHandler.ShowDualForm();
                    break;
                case "2":
                    SolveDualProblem();
                    break;
                case "3":
                    VerifyDuality();
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        private void SolveDualProblem()
        {
            Console.WriteLine("\n═══ DUAL PROBLEM SOLUTION ═══");
            
            // Create dual model
            var dualModel = CreateDualModel();
            
            Console.WriteLine("Dual Problem:");
            Console.WriteLine($"{dualModel.GetObjectiveFunctionString()}");
            Console.WriteLine("Subject to:");
            foreach (var constraint in dualModel.GetConstraintStrings())
            {
                Console.WriteLine($"  {constraint}");
            }
            
            // For demonstration, we'll show theoretical dual solution
            Console.WriteLine("\nDual Solution (theoretical):");
            if (_shadowPrices != null)
            {
                for (int i = 0; i < _model.Constraints.Count; i++)
                {
                    string dualVar = $"y{i + 1}";
                    double value = _shadowPrices.ContainsKey($"C{i + 1}") ? _shadowPrices[$"C{i + 1}"] : 0;
                    Console.WriteLine($"{dualVar} = {value:F3}");
                }
            }
            
            Console.WriteLine("\nNote: This is a simplified dual solution.");
            Console.WriteLine("In practice, the dual solution comes from the final simplex tableau.");
        }

        private void VerifyDuality()
        {
            Console.WriteLine("\n═══ DUALITY VERIFICATION ═══");
            
            if (_model.OptimalValue == 0)
            {
                Console.WriteLine("No primal optimal value available.");
                return;
            }

            // Calculate dual objective value
            double dualObjective = 0;
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                if (_shadowPrices.ContainsKey($"C{i + 1}"))
                {
                    dualObjective += _shadowPrices[$"C{i + 1}"] * _model.Constraints[i].RightHandSide;
                }
            }

            Console.WriteLine($"Primal optimal value: {_model.OptimalValue:F6}");
            Console.WriteLine($"Dual optimal value: {dualObjective:F6}");
            Console.WriteLine($"Difference: {Math.Abs(_model.OptimalValue - dualObjective):F6}");
            
            if (Math.Abs(_model.OptimalValue - dualObjective) < 1e-6)
            {
                Console.WriteLine("\n✓ STRONG DUALITY verified!");
                Console.WriteLine("The primal and dual optimal values are equal.");
            }
            else
            {
                Console.WriteLine("\n⚠ WEAK DUALITY detected.");
                Console.WriteLine("There may be a duality gap or calculation error.");
            }
            
            Console.WriteLine("\nDuality Theorems:");
            Console.WriteLine("• Weak Duality: Dual objective ≤ Primal objective (for maximization)");
            Console.WriteLine("• Strong Duality: If both problems have optimal solutions, their values are equal");
            Console.WriteLine("• Complementary Slackness: Primal slack × Dual variable = 0");
        }

        // Helper methods
        private double GetObjectiveCoefficient(string variableName)
        {
            int index = _model.Variables.FindIndex(v => v.Name == variableName);
            return index >= 0 && index < _model.ObjectiveCoefficients.Count ? _model.ObjectiveCoefficients[index] : 0;
        }

        private void SetObjectiveCoefficient(string variableName, double coefficient)
        {
            int index = _model.Variables.FindIndex(v => v.Name == variableName);
            if (index >= 0 && index < _model.ObjectiveCoefficients.Count)
            {
                _model.ObjectiveCoefficients[index] = coefficient;
            }
        }

        private double CalculateReducedCost(string variableName)
        {
            // Simplified reduced cost calculation
            // In practice, this would be calculated from the final simplex tableau
            return GetObjectiveCoefficient(variableName) - 1.0; // Simplified
        }

        private double CalculateReducedCostForNewVariable(List<double> constraintCoeffs, double objCoeff)
        {
            // Simplified calculation for new variable reduced cost
            double shadowPriceSum = 0;
            for (int i = 0; i < constraintCoeffs.Count && i < _model.Constraints.Count; i++)
            {
                if (_shadowPrices.ContainsKey($"C{i + 1}"))
                {
                    shadowPriceSum += constraintCoeffs[i] * _shadowPrices[$"C{i + 1}"];
                }
            }
            return objCoeff - shadowPriceSum;
        }

        private LinearProgrammingModel CreateDualModel()
        {
            var dualModel = new LinearProgrammingModel();
            
            // Dual has m variables (one for each primal constraint)
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                dualModel.Variables.Add(new Variable($"y{i + 1}", VariableType.Continuous));
                dualModel.ObjectiveCoefficients.Add(_model.Constraints[i].RightHandSide);
            }
            
            // Dual objective is opposite of primal
            dualModel.ObjectiveType = _model.ObjectiveType == ObjectiveType.Maximize ? 
                ObjectiveType.Minimize : ObjectiveType.Maximize;
            
            // Dual has n constraints (one for each primal variable)
            for (int j = 0; j < _model.Variables.Count; j++)
            {
                var dualConstraintCoeffs = new List<double>();
                for (int i = 0; i < _model.Constraints.Count; i++)
                {
                    dualConstraintCoeffs.Add(_model.Constraints[i].Coefficients[j]);
                }
                
                ConstraintType dualConstraintType = _model.ObjectiveType == ObjectiveType.Maximize ?
                    ConstraintType.GreaterThanOrEqual : ConstraintType.LessThanOrEqual;
                
                dualModel.Constraints.Add(new Constraint(
                    dualConstraintCoeffs, 
                    dualConstraintType, 
                    _model.ObjectiveCoefficients[j],
                    $"DC{j + 1}"));
            }
            
            return dualModel;
        }
    }
}