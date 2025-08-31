using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;
using LinearProgrammingProject.Solver;

namespace LinearProgrammingProject.Algorithms
{
    /// <summary>
    /// Branch & Bound Simplex Algorithm Implementation
    /// Features:
    /// - Displays canonical form
    /// - Implements backtracking
    /// - Creates all possible sub-problems
    /// - Fathoms nodes appropriately
    /// - Shows all table iterations
    /// - Tracks best candidate solution
    /// </summary>
    public class BranchAndBoundSimplex
    {
        public class BranchAndBoundNode
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public LinearProgrammingModel Model { get; set; }
            public double ObjectiveValue { get; set; }
            public Dictionary<string, double> Solution { get; set; }
            public SolutionStatus Status { get; set; }
            public List<Constraint> AdditionalConstraints { get; set; }
            public string BranchingVariable { get; set; }
            public double BranchingValue { get; set; }
            public string BranchDirection { get; set; } // "≤" or "≥"
            public bool IsFathomed { get; set; }
            public string FathomReason { get; set; }
            public PrimalSimplexSolver.SimplexReport SimplexReport { get; set; }
            public int Depth { get; set; }

            public BranchAndBoundNode()
            {
                AdditionalConstraints = new List<Constraint>();
                Solution = new Dictionary<string, double>();
            }
        }

        public class BranchAndBoundReport
        {
            public List<BranchAndBoundNode> AllNodes { get; set; }
            public BranchAndBoundNode BestIntegerSolution { get; set; }
            public double BestIntegerValue { get; set; }
            public List<BranchAndBoundNode> AllCandidates { get; set; }
            public BranchAndBoundNode BestCandidate { get; set; }
            public int TotalNodes { get; set; }
            public int NodesExplored { get; set; }
            public int NodesFathomed { get; set; }
            public List<string> IterationLogs { get; set; }
            public string CanonicalForm { get; set; }

            public BranchAndBoundReport()
            {
                AllNodes = new List<BranchAndBoundNode>();
                AllCandidates = new List<BranchAndBoundNode>();
                BestIntegerValue = double.NegativeInfinity; // Will be updated based on problem type
                IterationLogs = new List<string>();
            }
        }

        private readonly PrimalSimplexSolver _simplexSolver;
        private readonly double _tolerance = 1e-6;

        public BranchAndBoundSimplex()
        {
            _simplexSolver = new PrimalSimplexSolver();
        }

        public BranchAndBoundReport Solve(LinearProgrammingModel originalModel)
        {
            var report = new BranchAndBoundReport();
            bool isMaximization = originalModel.ObjectiveType == ObjectiveType.Maximize;
            
            // Initialize best value based on problem type
            report.BestIntegerValue = isMaximization ? double.NegativeInfinity : double.PositiveInfinity;
            
            // Display canonical form
            report.CanonicalForm = GenerateCanonicalForm(originalModel);
            report.IterationLogs.Add("=== CANONICAL FORM ===");
            report.IterationLogs.Add(report.CanonicalForm);
            report.IterationLogs.Add("");

            // Validate model for integer programming
            if (!originalModel.IsIntegerProgram())
            {
                throw new ArgumentException("Model must contain integer or binary variables for Branch & Bound");
            }

            // Initialize with root node
            var rootNode = CreateRootNode(originalModel);
            var nodeQueue = new Queue<BranchAndBoundNode>();
            nodeQueue.Enqueue(rootNode);
            
            int nodeCounter = 0;
            report.IterationLogs.Add("=== BRANCH & BOUND TREE EXPLORATION ===");

            while (nodeQueue.Count > 0)
            {
                var currentNode = nodeQueue.Dequeue();
                currentNode.Id = ++nodeCounter;
                report.AllNodes.Add(currentNode);
                report.NodesExplored++;

                report.IterationLogs.Add($"\n--- NODE {currentNode.Id} (Depth: {currentNode.Depth}) ---");
                if (currentNode.ParentId > 0)
                {
                    report.IterationLogs.Add($"Parent: Node {currentNode.ParentId}");
                    report.IterationLogs.Add($"Branch: {currentNode.BranchingVariable} {currentNode.BranchDirection} {currentNode.BranchingValue}");
                }

                // Solve LP relaxation for current node
                try
                {
                    var simplexReport = _simplexSolver.Solve(currentNode.Model);
                    currentNode.SimplexReport = simplexReport;
                    currentNode.Status = currentNode.Model.Status;
                    currentNode.ObjectiveValue = currentNode.Model.OptimalValue;
                    currentNode.Solution = new Dictionary<string, double>(currentNode.Model.OptimalSolution);

                    // Display simplex iterations for this node
                    report.IterationLogs.Add("Simplex Iterations:");
                    foreach (var snapshot in simplexReport.IterationSnapshots)
                    {
                        report.IterationLogs.Add(snapshot);
                    }

                    // Check fathoming conditions
                    if (ShouldFathomNode(currentNode, report.BestIntegerValue, report, isMaximization))
                    {
                        currentNode.IsFathomed = true;
                        report.NodesFathomed++;
                        report.IterationLogs.Add($"NODE {currentNode.Id} FATHOMED: {currentNode.FathomReason}");
                        continue;
                    }

                    // Add to candidates list for z-value tracking
                    report.AllCandidates.Add(currentNode);
                    
                    // Update best candidate based on z-value (highest for max, lowest for min)
                    bool isBetterCandidate = false;
                    if (report.BestCandidate == null)
                    {
                        isBetterCandidate = true;
                    }
                    else if (isMaximization && currentNode.ObjectiveValue > report.BestCandidate.ObjectiveValue)
                    {
                        isBetterCandidate = true;
                    }
                    else if (!isMaximization && currentNode.ObjectiveValue < report.BestCandidate.ObjectiveValue)
                    {
                        isBetterCandidate = true;
                    }
                    
                    if (isBetterCandidate)
                    {
                        report.BestCandidate = currentNode;
                        string candidateType = isMaximization ? "highest" : "lowest";
                        report.IterationLogs.Add($"NEW BEST CANDIDATE ({candidateType} z-value): z = {currentNode.ObjectiveValue:F6}");
                    }

                    // Check if solution is integer feasible
                    if (IsIntegerFeasible(currentNode, originalModel))
                    {
                        // Found integer solution - check if it's better than current best
                        bool isBetterInteger = false;
                        if (isMaximization && currentNode.ObjectiveValue > report.BestIntegerValue)
                        {
                            isBetterInteger = true;
                        }
                        else if (!isMaximization && currentNode.ObjectiveValue < report.BestIntegerValue)
                        {
                            isBetterInteger = true;
                        }
                        
                        if (isBetterInteger)
                        {
                            report.BestIntegerSolution = currentNode;
                            report.BestIntegerValue = currentNode.ObjectiveValue;
                            string solutionType = isMaximization ? "maximum" : "minimum";
                            report.IterationLogs.Add($"NEW BEST INTEGER SOLUTION FOUND! ({solutionType} z-value)");
                            report.IterationLogs.Add($"Objective Value (z): {currentNode.ObjectiveValue:F6}");
                            report.IterationLogs.Add("Solution:");
                            foreach (var kvp in currentNode.Solution)
                            {
                                report.IterationLogs.Add($"  {kvp.Key} = {kvp.Value:F6}");
                            }
                        }
                        currentNode.IsFathomed = true;
                        currentNode.FathomReason = "Integer feasible solution found";
                        report.NodesFathomed++;
                    }
                    else
                    {
                        // Branch on fractional variable
                        var branchingVar = SelectBranchingVariable(currentNode, originalModel);
                        if (branchingVar.HasValue)
                        {
                            var childNodes = CreateBranchingNodes(currentNode, branchingVar.Value, originalModel);
                            foreach (var child in childNodes)
                            {
                                nodeQueue.Enqueue(child);
                            }
                            report.IterationLogs.Add($"BRANCHING on {branchingVar.Value.Key} = {branchingVar.Value.Value:F6}");
                            report.IterationLogs.Add($"Created {childNodes.Count} child nodes");
                        }
                    }
                }
                catch (Exception ex)
                {
                    currentNode.Status = SolutionStatus.Error;
                    currentNode.IsFathomed = true;
                    currentNode.FathomReason = $"Solver error: {ex.Message}";
                    report.NodesFathomed++;
                    report.IterationLogs.Add($"NODE {currentNode.Id} FATHOMED: {currentNode.FathomReason}");
                }
            }

            report.TotalNodes = nodeCounter;
            
            // Final summary
            report.IterationLogs.Add("\n=== BRANCH & BOUND SUMMARY ===");
            report.IterationLogs.Add($"Total nodes created: {report.TotalNodes}");
            report.IterationLogs.Add($"Nodes explored: {report.NodesExplored}");
            report.IterationLogs.Add($"Nodes fathomed: {report.NodesFathomed}");
            report.IterationLogs.Add($"Total candidates evaluated: {report.AllCandidates.Count}");
            
            // Display best candidate (highest z-value for max, lowest for min)
            if (report.BestCandidate != null)
            {
                string candidateType = isMaximization ? "Highest" : "Lowest";
                report.IterationLogs.Add($"\nBEST CANDIDATE ({candidateType} z-value):");
                report.IterationLogs.Add($"Objective Value (z): {report.BestCandidate.ObjectiveValue:F6}");
                report.IterationLogs.Add("Variables:");
                foreach (var kvp in report.BestCandidate.Solution)
                {
                    report.IterationLogs.Add($"  {kvp.Key} = {kvp.Value:F6}");
                }
                report.IterationLogs.Add($"Integer Feasible: {IsIntegerFeasible(report.BestCandidate, originalModel)}");
            }
            
            if (report.BestIntegerSolution != null)
            {
                report.IterationLogs.Add($"\nBEST INTEGER SOLUTION:");
                report.IterationLogs.Add($"Objective Value (z): {report.BestIntegerValue:F6}");
                report.IterationLogs.Add("Variables:");
                foreach (var kvp in report.BestIntegerSolution.Solution)
                {
                    report.IterationLogs.Add($"  {kvp.Key} = {kvp.Value:F6}");
                }
            }
            else
            {
                report.IterationLogs.Add("\nNo integer feasible solution found.");
            }

            return report;
        }

        private string GenerateCanonicalForm(LinearProgrammingModel model)
        {
            var form = new System.Text.StringBuilder();
            
            // Objective function
            form.AppendLine("CANONICAL FORM:");
            form.AppendLine();
            form.AppendLine(model.GetObjectiveFunctionString());
            form.AppendLine();
            
            // Constraints
            form.AppendLine("Subject to:");
            var constraintStrings = model.GetConstraintStrings();
            foreach (var constraint in constraintStrings)
            {
                form.AppendLine($"  {constraint}");
            }
            form.AppendLine();
            
            // Variable bounds and types
            form.AppendLine("Variable bounds and types:");
            foreach (var variable in model.Variables)
            {
                form.AppendLine($"  {variable.GetBoundsString()}, Type: {variable.Type}");
            }
            
            return form.ToString();
        }

        private BranchAndBoundNode CreateRootNode(LinearProgrammingModel originalModel)
        {
            var rootNode = new BranchAndBoundNode
            {
                ParentId = 0,
                Model = CloneModel(originalModel),
                Depth = 0
            };
            
            // Convert integer/binary constraints to continuous for LP relaxation
            foreach (var variable in rootNode.Model.Variables)
            {
                if (variable.Type == VariableType.Integer || variable.Type == VariableType.Binary)
                {
                    variable.Type = VariableType.Continuous;
                }
            }
            
            return rootNode;
        } 
       private bool ShouldFathomNode(BranchAndBoundNode node, double bestIntegerValue, BranchAndBoundReport report, bool isMaximization = true)
        {
            // Fathom by infeasibility
            if (node.Status == SolutionStatus.Infeasible)
            {
                node.FathomReason = "Infeasible";
                return true;
            }
            
            // Fathom by unboundedness (shouldn't happen with proper bounds)
            if (node.Status == SolutionStatus.Unbounded)
            {
                node.FathomReason = "Unbounded";
                return true;
            }
            
            // Fathom by bound - different logic for max vs min problems
            if (node.Status == SolutionStatus.Optimal)
            {
                if (isMaximization && bestIntegerValue != double.NegativeInfinity && node.ObjectiveValue <= bestIntegerValue + _tolerance)
                {
                    node.FathomReason = $"Bound: {node.ObjectiveValue:F6} ≤ {bestIntegerValue:F6}";
                    return true;
                }
                else if (!isMaximization && bestIntegerValue != double.PositiveInfinity && node.ObjectiveValue >= bestIntegerValue - _tolerance)
                {
                    node.FathomReason = $"Bound: {node.ObjectiveValue:F6} ≥ {bestIntegerValue:F6}";
                    return true;
                }
            }
            
            return false;
        }

        private bool IsIntegerFeasible(BranchAndBoundNode node, LinearProgrammingModel originalModel)
        {
            if (node.Status != SolutionStatus.Optimal)
                return false;

            foreach (var variable in originalModel.Variables)
            {
                if (variable.Type == VariableType.Integer || variable.Type == VariableType.Binary)
                {
                    if (node.Solution.ContainsKey(variable.Name))
                    {
                        double value = node.Solution[variable.Name];
                        if (Math.Abs(value - Math.Round(value)) > _tolerance)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private KeyValuePair<string, double>? SelectBranchingVariable(BranchAndBoundNode node, LinearProgrammingModel originalModel)
        {
            // Find the integer/binary variable with the most fractional value (closest to 0.5)
            string bestVar = null;
            double bestValue = 0;
            double bestFractionalPart = 0;

            foreach (var variable in originalModel.Variables)
            {
                if (variable.Type == VariableType.Integer || variable.Type == VariableType.Binary)
                {
                    if (node.Solution.ContainsKey(variable.Name))
                    {
                        double value = node.Solution[variable.Name];
                        double fractionalPart = Math.Abs(value - Math.Round(value));
                        
                        if (fractionalPart > _tolerance && fractionalPart > bestFractionalPart)
                        {
                            bestVar = variable.Name;
                            bestValue = value;
                            bestFractionalPart = fractionalPart;
                        }
                    }
                }
            }

            if (bestVar != null)
            {
                return new KeyValuePair<string, double>(bestVar, bestValue);
            }
            return null;
        }

        private List<BranchAndBoundNode> CreateBranchingNodes(BranchAndBoundNode parentNode, KeyValuePair<string, double> branchingVar, LinearProgrammingModel originalModel)
        {
            var nodes = new List<BranchAndBoundNode>();
            double branchValue = branchingVar.Value;
            string varName = branchingVar.Key;

            // Create left branch: x ≤ floor(value)
            var leftNode = new BranchAndBoundNode
            {
                ParentId = parentNode.Id,
                Model = CloneModel(parentNode.Model),
                Depth = parentNode.Depth + 1,
                BranchingVariable = varName,
                BranchingValue = Math.Floor(branchValue),
                BranchDirection = "≤"
            };

            // Add constraint x ≤ floor(value)
            var leftConstraint = CreateBranchingConstraint(varName, Math.Floor(branchValue), ConstraintType.LessThanOrEqual, originalModel);
            leftNode.Model.Constraints.Add(leftConstraint);
            leftNode.AdditionalConstraints.Add(leftConstraint);

            // Create right branch: x ≥ ceil(value)
            var rightNode = new BranchAndBoundNode
            {
                ParentId = parentNode.Id,
                Model = CloneModel(parentNode.Model),
                Depth = parentNode.Depth + 1,
                BranchingVariable = varName,
                BranchingValue = Math.Ceiling(branchValue),
                BranchDirection = "≥"
            };

            // Add constraint x ≥ ceil(value)
            var rightConstraint = CreateBranchingConstraint(varName, Math.Ceiling(branchValue), ConstraintType.GreaterThanOrEqual, originalModel);
            rightNode.Model.Constraints.Add(rightConstraint);
            rightNode.AdditionalConstraints.Add(rightConstraint);

            nodes.Add(leftNode);
            nodes.Add(rightNode);

            return nodes;
        }

        private Constraint CreateBranchingConstraint(string variableName, double value, ConstraintType type, LinearProgrammingModel originalModel)
        {
            var coefficients = new List<double>();
            
            // Find the variable index
            int varIndex = -1;
            for (int i = 0; i < originalModel.Variables.Count; i++)
            {
                if (originalModel.Variables[i].Name == variableName)
                {
                    varIndex = i;
                    break;
                }
            }

            // Create coefficient vector (1 for the branching variable, 0 for others)
            for (int i = 0; i < originalModel.Variables.Count; i++)
            {
                coefficients.Add(i == varIndex ? 1.0 : 0.0);
            }

            return new Constraint(coefficients, type, value, $"Branch_{variableName}_{type}_{value}");
        }

        private LinearProgrammingModel CloneModel(LinearProgrammingModel original)
        {
            var clone = new LinearProgrammingModel
            {
                ObjectiveType = original.ObjectiveType,
                ObjectiveCoefficients = new List<double>(original.ObjectiveCoefficients)
            };

            // Clone variables
            foreach (var variable in original.Variables)
            {
                clone.Variables.Add(new Variable(variable.Name, variable.Type, variable.LowerBound, variable.UpperBound)
                {
                    Value = variable.Value
                });
            }

            // Clone constraints
            foreach (var constraint in original.Constraints)
            {
                clone.Constraints.Add(new Constraint(
                    new List<double>(constraint.Coefficients),
                    constraint.Type,
                    constraint.RightHandSide,
                    constraint.Name
                ));
            }

            return clone;
        }

        public void DisplayBranchAndBoundTree(BranchAndBoundReport report)
        {
            Console.WriteLine("\n=== BRANCH & BOUND TREE STRUCTURE ===");
            
            // Group nodes by depth for better visualization
            var nodesByDepth = report.AllNodes.GroupBy(n => n.Depth).OrderBy(g => g.Key);
            
            foreach (var depthGroup in nodesByDepth)
            {
                Console.WriteLine($"\nDepth {depthGroup.Key}:");
                foreach (var node in depthGroup.OrderBy(n => n.Id))
                {
                    string status = node.IsFathomed ? $"FATHOMED ({node.FathomReason})" : "ACTIVE";
                    string branchInfo = node.ParentId > 0 ? $" [{node.BranchingVariable} {node.BranchDirection} {node.BranchingValue}]" : "";
                    string objValue = node.Status == SolutionStatus.Optimal ? $" Obj: {node.ObjectiveValue:F3}" : "";
                    
                    Console.WriteLine($"  Node {node.Id}{branchInfo} - {status}{objValue}");
                    
                    if (node.Status == SolutionStatus.Optimal && !node.IsFathomed)
                    {
                        Console.Write("    Solution: ");
                        var integerVars = node.Solution.Where(kvp => 
                            report.AllNodes[0].Model.Variables.Any(v => v.Name == kvp.Key && 
                            (v.Type == VariableType.Integer || v.Type == VariableType.Binary)));
                        
                        foreach (var kvp in integerVars)
                        {
                            Console.Write($"{kvp.Key}={kvp.Value:F3} ");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }

        public void DisplayAllTableIterations(BranchAndBoundReport report)
        {
            Console.WriteLine("\n=== ALL SIMPLEX TABLE ITERATIONS ===");
            
            foreach (var node in report.AllNodes)
            {
                Console.WriteLine($"\n--- NODE {node.Id} SIMPLEX ITERATIONS ---");
                if (node.ParentId > 0)
                {
                    Console.WriteLine($"Branch: {node.BranchingVariable} {node.BranchDirection} {node.BranchingValue}");
                }
                
                if (node.SimplexReport != null && node.SimplexReport.IterationSnapshots != null)
                {
                    foreach (var snapshot in node.SimplexReport.IterationSnapshots)
                    {
                        Console.WriteLine(snapshot);
                    }
                    
                    if (node.SimplexReport.Pivots.Count > 0)
                    {
                        Console.WriteLine("Pivot Operations:");
                        foreach (var pivot in node.SimplexReport.Pivots)
                        {
                            Console.WriteLine($"  {pivot}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No simplex iterations available for this node.");
                }
                
                Console.WriteLine($"Final Status: {node.Status}");
                if (node.Status == SolutionStatus.Optimal)
                {
                    Console.WriteLine($"Objective Value: {node.ObjectiveValue:F6}");
                }
                
                if (node.IsFathomed)
                {
                    Console.WriteLine($"Fathomed: {node.FathomReason}");
                }
                
                Console.WriteLine(new string('-', 50));
            }
        }
    }
}