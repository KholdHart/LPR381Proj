using System;
using System.Collections.Generic;
using System.Linq;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Algorithms
{
    /// <summary>
    /// Branch & Bound Knapsack Algorithm Implementation
    /// Features:
    /// - Implements backtracking with complete tree exploration
    /// - Creates all possible sub-problems to branch on
    /// - Fathoms all possible nodes appropriately
    /// - Displays all table iterations of sub-problems
    /// - Tracks and displays best candidate solution
    /// - Outputs canonical form and all iterations
    /// </summary>
    public class KnapsackBranchAndBound
    {
        public class KnapsackNode
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public int Level { get; set; }
            public List<int> IncludedItems { get; set; }
            public List<int> ExcludedItems { get; set; }
            public double UpperBound { get; set; }
            public double CurrentValue { get; set; }
            public double CurrentWeight { get; set; }
            public bool IsFathomed { get; set; }
            public string FathomReason { get; set; }
            public string BranchDecision { get; set; }
            public List<string> TableIterations { get; set; }

            public KnapsackNode()
            {
                IncludedItems = new List<int>();
                ExcludedItems = new List<int>();
                TableIterations = new List<string>();
            }
        }

        public class KnapsackReport
        {
            public List<KnapsackNode> AllNodes { get; set; }
            public KnapsackNode BestSolution { get; set; }
            public double BestValue { get; set; }
            public List<KnapsackNode> AllCandidates { get; set; }
            public KnapsackNode BestCandidate { get; set; }
            public int TotalNodes { get; set; }
            public int NodesExplored { get; set; }
            public int NodesFathomed { get; set; }
            public List<string> IterationLogs { get; set; }
            public string CanonicalForm { get; set; }
            public List<KnapsackItem> Items { get; set; }
            public double Capacity { get; set; }

            public KnapsackReport()
            {
                AllNodes = new List<KnapsackNode>();
                AllCandidates = new List<KnapsackNode>();
                BestValue = 0;
                IterationLogs = new List<string>();
                Items = new List<KnapsackItem>();
            }
        }

        public class KnapsackItem
        {
            public int Index { get; set; }
            public double Value { get; set; }
            public double Weight { get; set; }
            public double Ratio { get; set; }
            public string Name { get; set; }

            public KnapsackItem(int index, double value, double weight, string name = null)
            {
                Index = index;
                Value = value;
                Weight = weight;
                Ratio = weight > 0 ? value / weight : 0;
                Name = name ?? $"x{index + 1}";
            }
        }

        private readonly double _tolerance = 1e-6;

        public KnapsackReport Solve(LinearProgrammingModel model)
        {
            var report = new KnapsackReport();
            
            // Convert LP model to knapsack format
            var knapsackData = ConvertToKnapsackFormat(model, report);
            if (knapsackData == null)
            {
                throw new ArgumentException("Model is not suitable for knapsack algorithm");
            }

            // Generate canonical form
            report.CanonicalForm = GenerateCanonicalForm(report.Items, report.Capacity);
            report.IterationLogs.Add("╔═══════════════════════════════════════════════════════════════════════════════╗");
            report.IterationLogs.Add("║                              CANONICAL FORM                                   ║");
            report.IterationLogs.Add("╚═══════════════════════════════════════════════════════════════════════════════╝");
            report.IterationLogs.Add(report.CanonicalForm);
            report.IterationLogs.Add("");

            // Sort items by value-to-weight ratio (descending)
            report.Items = report.Items.OrderByDescending(item => item.Ratio).ToList();
            
            report.IterationLogs.Add("  ╔═════════════════════════════════════════════════════════════════════════════════════════════════╗");
            report.IterationLogs.Add("  ║                    ITEMS SORTED BY VALUE/WEIGHT RATIO                                           ║");
            report.IterationLogs.Add("  ╠════════╤═════════╤═════════╤═════════╤══════════════════════════════════════════════════════════╣");
            report.IterationLogs.Add("  ║  Item  │  Value  │ Weight  │  Ratio  │           Description                                    ║");
            report.IterationLogs.Add("  ╠════════╪═════════╪═════════╪═════════╪══════════════════════════════════════════════════════════╣");
            foreach (var item in report.Items)
            {
                string desc = $"Higher ratio = better efficiency";
                report.IterationLogs.Add($"  ║  {item.Name,-4}  │ {item.Value,7:F3} │ {item.Weight,7:F3} │ {item.Ratio,7:F3} │ {desc,-56} ║");
            }
            report.IterationLogs.Add("  ╚════════╧═════════╧═════════╧═════════╧══════════════════════════════════════════════════════════╝");
            report.IterationLogs.Add("");

            // Initialize with root node
            var rootNode = CreateRootNode();
            var nodeQueue = new Queue<KnapsackNode>();
            nodeQueue.Enqueue(rootNode);
            
            int nodeCounter = 0;
            report.IterationLogs.Add("╔═══════════════════════════════════════════════════════════════════════════════╗");
            report.IterationLogs.Add("║                    BRANCH & BOUND TREE EXPLORATION                            ║");
            report.IterationLogs.Add("╚═══════════════════════════════════════════════════════════════════════════════╝");

            while (nodeQueue.Count > 0)
            {
                var currentNode = nodeQueue.Dequeue();
                currentNode.Id = ++nodeCounter;
                report.AllNodes.Add(currentNode);
                report.NodesExplored++;

                report.IterationLogs.Add($"\n╔═══════════════════════════════════════════════════════════════════════════════╗");
                report.IterationLogs.Add($"║                              NODE {currentNode.Id,2}                                          ║");
                report.IterationLogs.Add($"╚═══════════════════════════════════════════════════════════════════════════════╝");
                
                if (currentNode.ParentId > 0)
                {
                    report.IterationLogs.Add($"┌─ Parent Node: {currentNode.ParentId}");
                    report.IterationLogs.Add($"└─ Branch Decision: {currentNode.BranchDecision}");
                    report.IterationLogs.Add("");
                }

                // Calculate current state
                CalculateNodeState(currentNode, report.Items, report.Capacity);
                
                // Add to candidates list for z-value tracking (feasible nodes only)
                if (currentNode.CurrentWeight <= report.Capacity)
                {
                    report.AllCandidates.Add(currentNode);
                    
                    // Update best candidate based on highest z-value (for maximization)
                    if (report.BestCandidate == null || currentNode.CurrentValue > report.BestCandidate.CurrentValue)
                    {
                        report.BestCandidate = currentNode;
                        report.IterationLogs.Add($"NEW BEST CANDIDATE: z = {currentNode.CurrentValue:F3}");
                    }
                }
                
                // Display table iteration for this node
                DisplayNodeTable(currentNode, report);

                // Check fathoming conditions
                if (ShouldFathomNode(currentNode, report.BestValue, report))
                {
                    currentNode.IsFathomed = true;
                    report.NodesFathomed++;
                    report.IterationLogs.Add("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                    report.IterationLogs.Add($"  │  NODE {currentNode.Id,2} FATHOMED: {currentNode.FathomReason,-50}       │");
                    report.IterationLogs.Add("  └─────────────────────────────────────────────────────────────────────────────┘");
                    continue;
                }

                // Check if this is a complete solution
                if (currentNode.Level == report.Items.Count)
                {
                    // Complete solution - check if it's better than current best
                    if (currentNode.CurrentValue > report.BestValue)
                    {
                        report.BestSolution = currentNode;
                        report.BestValue = currentNode.CurrentValue;
                        report.IterationLogs.Add("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                        report.IterationLogs.Add("  │  NEW BEST SOLUTION FOUND! (Highest z-value)                                 │");
                        report.IterationLogs.Add($"  │    Value (z): {currentNode.CurrentValue,7:F3}   Weight: {currentNode.CurrentWeight,7:F3}                                     │");
                        report.IterationLogs.Add($"  │    Items: [{string.Join(", ", currentNode.IncludedItems.Select(i => report.Items[i].Name))}]                                                          │");
                        report.IterationLogs.Add("  └─────────────────────────────────────────────────────────────────────────────┘");
                    }
                    else
                    {
                        report.IterationLogs.Add("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                        report.IterationLogs.Add($"  │ Complete solution (z-value: {currentNode.CurrentValue:F3}) - Not better than current best │");
                        report.IterationLogs.Add("  └─────────────────────────────────────────────────────────────────────────────┘");
                    }
                    currentNode.IsFathomed = true;
                    currentNode.FathomReason = "Complete solution";
                    report.NodesFathomed++;
                }
                else
                {
                    // Branch on next item
                    var childNodes = CreateBranchingNodes(currentNode, report.Items, report.Capacity);
                    foreach (var child in childNodes)
                    {
                        nodeQueue.Enqueue(child);
                    }
                    report.IterationLogs.Add("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                    report.IterationLogs.Add($"  │ BRANCHING on item {report.Items[currentNode.Level].Name,-10}                                                │");
                    report.IterationLogs.Add("  └─────────────────────────────────────────────────────────────────────────────┘");
                }
            }

            report.TotalNodes = nodeCounter;
            
            // Final summary with enhanced formatting
            report.IterationLogs.Add("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            report.IterationLogs.Add("  ║                         BRANCH & BOUND SUMMARY                                ║");
            report.IterationLogs.Add("  ╠═══════════════════════════════════════════════════════════════════════════════╣");
            report.IterationLogs.Add($"  ║ Total nodes created:     {report.TotalNodes,3}                                                  ║");
            report.IterationLogs.Add($"  ║ Nodes explored:          {report.NodesExplored,3}                                                  ║");
            report.IterationLogs.Add($"  ║ Nodes fathomed:          {report.NodesFathomed,3}                                                  ║");
            report.IterationLogs.Add($"  ║ Total candidates:        {report.AllCandidates.Count,3}                                                  ║");
            report.IterationLogs.Add($"  ║ Efficiency:              {(double)report.NodesFathomed / report.TotalNodes * 100,6:F1}%                                              ║");
            report.IterationLogs.Add("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            
            // Display best candidate (highest z-value for maximization)
            if (report.BestCandidate != null)
            {
                report.IterationLogs.Add("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
                report.IterationLogs.Add("  ║                        BEST CANDIDATE (Highest z-value)                       ║");
                report.IterationLogs.Add("  ╠═══════════════════════════════════════════════════════════════════════════════╣");
                report.IterationLogs.Add($"  ║ Best z-value:            {report.BestCandidate.CurrentValue,7:F3}                                              ║");
                report.IterationLogs.Add($"  ║ Weight:                  {report.BestCandidate.CurrentWeight,7:F3}                                              ║");
                report.IterationLogs.Add($"  ║ Upper Bound:             {report.BestCandidate.UpperBound,7:F3}                                              ║");
                report.IterationLogs.Add("  ║                                                                               ║");
                report.IterationLogs.Add("  ║ Items in Best Candidate:                                                      ║");
                
                foreach (var itemIndex in report.BestCandidate.IncludedItems)
                {
                    var item = report.Items[itemIndex];
                    report.IterationLogs.Add($"  ║   {item.Name}: Value={item.Value,7:F3}, Weight={item.Weight,7:F3}, Ratio={item.Ratio,7:F3}                            ║");
                }
                report.IterationLogs.Add("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            }
            
            if (report.BestSolution != null)
            {
                report.IterationLogs.Add("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
                report.IterationLogs.Add("  ║                             OPTIMAL SOLUTION                                  ║");
                report.IterationLogs.Add("  ╠═══════════════════════════════════════════════════════════════════════════════╣");
                report.IterationLogs.Add($"  ║ Optimal Value:           {report.BestValue,7:F3}                                              ║");
                report.IterationLogs.Add($"  ║ Total Weight:            {report.BestSolution.CurrentWeight,7:F3}                                              ║");
                report.IterationLogs.Add($"  ║ Capacity Utilization:    {(report.BestSolution.CurrentWeight / report.Capacity * 100),6:F1}%                                              ║");
                report.IterationLogs.Add("  ║                                                                               ║");
                report.IterationLogs.Add("  ║ Items Selected:                                                               ║");
                
                foreach (var itemIndex in report.BestSolution.IncludedItems)
                {
                    var item = report.Items[itemIndex];
                    report.IterationLogs.Add($"  ║   {item.Name}: Value={item.Value,7:F3}, Weight={item.Weight,7:F3}, Ratio={item.Ratio,7:F3}                            ║");
                }
                
                report.IterationLogs.Add("  ║                                                                               ║");
                report.IterationLogs.Add("  ║ Complete Variable Assignment:                                                 ║");
                for (int i = 0; i < report.Items.Count; i++)
                {
                    var item = report.Items[i];
                    int value = report.BestSolution.IncludedItems.Contains(i) ? 1 : 0;
                    report.IterationLogs.Add($"  ║   {item.Name} = {value}                                                                      ║");
                }
                report.IterationLogs.Add("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            }
            else
            {
                report.IterationLogs.Add("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
                report.IterationLogs.Add("  ║                           NO FEASIBLE SOLUTION                                ║");
                report.IterationLogs.Add("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            }

            return report;
        }

        private object ConvertToKnapsackFormat(LinearProgrammingModel model, KnapsackReport report)
        {
            // Validate model for knapsack format
            if (model.ObjectiveType != ObjectiveType.Maximize)
            {
                throw new ArgumentException("Knapsack problems must be maximization problems");
            }

            // Check for single constraint (knapsack capacity)
            if (model.Constraints.Count != 1)
            {
                throw new ArgumentException("Knapsack problems must have exactly one constraint (capacity)");
            }

            var constraint = model.Constraints[0];
            if (constraint.Type != ConstraintType.LessThanOrEqual)
            {
                throw new ArgumentException("Knapsack constraint must be ≤ type");
            }

            // Check for binary variables
            foreach (var variable in model.Variables)
            {
                if (variable.Type != VariableType.Binary)
                {
                    throw new ArgumentException("Knapsack problems require binary variables");
                }
            }

            // Extract knapsack data
            report.Capacity = constraint.RightHandSide;
            for (int i = 0; i < model.Variables.Count; i++)
            {
                var item = new KnapsackItem(
                    i,
                    model.ObjectiveCoefficients[i],
                    constraint.Coefficients[i],
                    model.Variables[i].Name
                );
                report.Items.Add(item);
            }

            return true;
        }

        private string GenerateCanonicalForm(List<KnapsackItem> items, double capacity)
        {
            var form = new System.Text.StringBuilder();
            
            form.AppendLine("KNAPSACK PROBLEM CANONICAL FORM:");
            form.AppendLine();
            
            // Objective function
            form.Append("Maximize: ");
            var terms = items.Select(item => $"{item.Value:F3}*{item.Name}");
            form.AppendLine(string.Join(" + ", terms));
            form.AppendLine();
            
            // Constraint
            form.AppendLine("Subject to:");
            var constraintTerms = items.Select(item => $"{item.Weight:F3}*{item.Name}");
            form.AppendLine($"  {string.Join(" + ", constraintTerms)} ≤ {capacity:F3}");
            form.AppendLine();
            
            // Variable bounds
            form.AppendLine("Variable bounds:");
            foreach (var item in items)
            {
                form.AppendLine($"  {item.Name} ∈ {{0, 1}}");
            }
            
            return form.ToString();
        }

        private KnapsackNode CreateRootNode()
        {
            return new KnapsackNode
            {
                ParentId = 0,
                Level = 0,
                CurrentValue = 0,
                CurrentWeight = 0
            };
        }

        private void CalculateNodeState(KnapsackNode node, List<KnapsackItem> items, double capacity)
        {
            // Calculate current value and weight
            node.CurrentValue = 0;
            node.CurrentWeight = 0;
            
            foreach (var itemIndex in node.IncludedItems)
            {
                node.CurrentValue += items[itemIndex].Value;
                node.CurrentWeight += items[itemIndex].Weight;
            }

            // Calculate upper bound using fractional knapsack
            node.UpperBound = CalculateUpperBound(node, items, capacity);
        }

        private double CalculateUpperBound(KnapsackNode node, List<KnapsackItem> items, double capacity)
        {
            double upperBound = node.CurrentValue;
            double remainingCapacity = capacity - node.CurrentWeight;

            // Add items not yet decided (greedy fractional approach)
            for (int i = node.Level; i < items.Count; i++)
            {
                if (node.ExcludedItems.Contains(i)) continue;

                if (items[i].Weight <= remainingCapacity)
                {
                    // Can include entire item
                    upperBound += items[i].Value;
                    remainingCapacity -= items[i].Weight;
                }
                else
                {
                    // Include fractional part
                    upperBound += (remainingCapacity / items[i].Weight) * items[i].Value;
                    break;
                }
            }

            return upperBound;
        }

        private void DisplayNodeTable(KnapsackNode node, KnapsackReport report)
        {
            var table = new System.Text.StringBuilder();
            
            // Enhanced table header with better formatting
            table.AppendLine("  ┌────────────────────────────────────────────────────────────────────────────┐");
            table.AppendLine("  │                             NODE STATE TABLE                               │");
            table.AppendLine("  ├──────────┬──────────┬──────────┬──────────┬──────────┬──────────┬──────────┤");
            table.AppendLine("  │  Item    │ Status   │  Value   │ Weight   │ Ratio    │ CumVal   │ CumWeight│");
            table.AppendLine("  ├──────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────┤");

            double cumValue = 0;
            double cumWeight = 0;

            for (int i = 0; i < report.Items.Count; i++)
            {
                var item = report.Items[i];
                string status = "   ?   ";
                
                if (node.IncludedItems.Contains(i))
                {
                    status = "   1   ";
                    cumValue += item.Value;
                    cumWeight += item.Weight;
                }
                else if (node.ExcludedItems.Contains(i))
                {
                    status = "   0   ";
                }

                table.AppendLine($"  │ {item.Name,-5}    │ {status}  │{item.Value,7:F3}   │{item.Weight,7:F3}   │ {item.Ratio,7:F3}  │ {cumValue,7:F3}  │{cumWeight,7:F3}   │");
            }

            table.AppendLine("  ├──────────┴──────────┴──────────┴──────────┴──────────┴──────────┴──────────┤");
            table.AppendLine($"  │ Current Solution: Value = {node.CurrentValue,7:F3}  Weight = {node.CurrentWeight,7:F3}                        │");
            table.AppendLine($"  │ Upper Bound:      {node.UpperBound,7:F3}  Remaining Cap = {(report.Capacity - node.CurrentWeight),7:F3}                         │");
            table.AppendLine($"  │ Capacity Used:    {(node.CurrentWeight / report.Capacity * 100),6:F1}%   Available = {(report.Capacity - node.CurrentWeight),7:F3}                            │");
            table.AppendLine("  └────────────────────────────────────────────────────────────────────────────┘");

            node.TableIterations.Add(table.ToString());
            report.IterationLogs.Add(table.ToString());
        }

        private bool ShouldFathomNode(KnapsackNode node, double bestValue, KnapsackReport report)
        {
            // Fathom by infeasibility (weight exceeds capacity)
            if (node.CurrentWeight > report.Capacity + _tolerance)
            {
                node.FathomReason = $"Infeasible: Weight {node.CurrentWeight:F3} > Capacity {report.Capacity:F3}";
                return true;
            }
            
            // Fathom by bound (upper bound ≤ current best)
            if (node.UpperBound <= bestValue + _tolerance)
            {
                node.FathomReason = $"Bound: Upper bound {node.UpperBound:F3} ≤ Best value {bestValue:F3}";
                return true;
            }
            
            return false;
        }

        private List<KnapsackNode> CreateBranchingNodes(KnapsackNode parentNode, List<KnapsackItem> items, double capacity)
        {
            var nodes = new List<KnapsackNode>();
            int nextItemIndex = parentNode.Level;

            if (nextItemIndex >= items.Count) return nodes;

            // Create left branch: exclude item
            var leftNode = new KnapsackNode
            {
                ParentId = parentNode.Id,
                Level = parentNode.Level + 1,
                IncludedItems = new List<int>(parentNode.IncludedItems),
                ExcludedItems = new List<int>(parentNode.ExcludedItems),
                BranchDecision = $"Exclude {items[nextItemIndex].Name}"
            };
            leftNode.ExcludedItems.Add(nextItemIndex);

            // Create right branch: include item
            var rightNode = new KnapsackNode
            {
                ParentId = parentNode.Id,
                Level = parentNode.Level + 1,
                IncludedItems = new List<int>(parentNode.IncludedItems),
                ExcludedItems = new List<int>(parentNode.ExcludedItems),
                BranchDecision = $"Include {items[nextItemIndex].Name}"
            };
            rightNode.IncludedItems.Add(nextItemIndex);

            nodes.Add(leftNode);
            nodes.Add(rightNode);

            return nodes;
        }

        public void DisplayBranchAndBoundTree(KnapsackReport report)
        {
            Console.WriteLine("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                    KNAPSACK BRANCH & BOUND TREE STRUCTURE                     ║");
            Console.WriteLine("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            
            // Group nodes by level for better visualization
            var nodesByLevel = report.AllNodes.GroupBy(n => n.Level).OrderBy(g => g.Key);
            
            foreach (var levelGroup in nodesByLevel)
            {
                Console.WriteLine($"\n┌─ DEPTH {levelGroup.Key} ─────────────────────────────────────────────────────────────────┐");
                foreach (var node in levelGroup.OrderBy(n => n.Id))
                {
                    string status = node.IsFathomed ? "FATHOMED" : "ACTIVE";
                    string statusIcon = node.IsFathomed ? "❌" : "✅";
                    string branchInfo = node.ParentId > 0 ? $"[{node.BranchDecision}]" : "[ROOT]";
                    
                    Console.WriteLine($"│ {statusIcon} Node {node.Id,2}: {branchInfo,-20} Status: {status,-10}");
                    Console.WriteLine($"│    Value: {node.CurrentValue,7:F3}  Weight: {node.CurrentWeight,7:F3}  Upper Bound: {node.UpperBound,7:F3}");
                    
                    if (node.IsFathomed)
                    {
                        Console.WriteLine($"│    Fathom Reason: {node.FathomReason}");
                    }
                    
                    if (node.IncludedItems.Count > 0)
                    {
                        var includedNames = node.IncludedItems.Select(i => report.Items[i].Name);
                        Console.WriteLine($"│    Items Included: [{string.Join(", ", includedNames)}]");
                    }
                    Console.WriteLine("│");
                }
                Console.WriteLine("└───────────────────────────────────────────────────────────────────────────┘");
            }
        }

        public void DisplayAllTableIterations(KnapsackReport report)
        {
            Console.WriteLine("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                         ALL NODE TABLE ITERATIONS                             ║");
            Console.WriteLine("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            
            foreach (var node in report.AllNodes)
            {
                Console.WriteLine($"\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine($"  ║                           NODE {node.Id,2} DETAILED TABLE                     ║");
                Console.WriteLine($"  ╚═══════════════════════════════════════════════════════════════════════════════╝");
                
                if (node.ParentId > 0)
                {
                    Console.WriteLine($"┌─ Parent: Node {node.ParentId}");
                    Console.WriteLine($"└─ Branch Decision: {node.BranchDecision}");
                    Console.WriteLine();
                }
                
                foreach (var table in node.TableIterations)
                {
                    Console.WriteLine(table);
                }
                
                if (node.IsFathomed)
                {
                    Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"  │  FATHOMED: {node.FathomReason,-60}                                          │");
                    Console.WriteLine("  └─────────────────────────────────────────────────────────────────────────────┘");
                }
                
                Console.WriteLine("\n" + new string('═', 79));
            }
        }

        public void DisplayBacktrackingProcess(KnapsackReport report)
        {
            Console.WriteLine("\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                           BACKTRACKING PROCESS                                ║");
            Console.WriteLine("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            
            Console.WriteLine("┌──────────────────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ The algorithm explores ALL possible combinations through systematic branching        │");
            Console.WriteLine("│ Each node represents a decision point: Include or Exclude the next item              │");
            Console.WriteLine("│ Backtracking occurs when nodes are fathomed (pruned) for efficiency                  │");
            Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────────────┘");
            
            Console.WriteLine("\nEXPLORATION STATISTICS:");
            Console.WriteLine($"┌─ Total possible combinations: 2^{report.Items.Count} = {Math.Pow(2, report.Items.Count)}");
            Console.WriteLine($"├─ Nodes actually created: {report.TotalNodes}");
            Console.WriteLine($"├─ Nodes fathomed (pruned): {report.NodesFathomed}");
            Console.WriteLine($"└─ Efficiency gained: {(1 - (double)report.TotalNodes / Math.Pow(2, report.Items.Count)) * 100:F1}% reduction in search space");
            
            Console.WriteLine("\nBRANCHING STRATEGY:");
            Console.WriteLine("┌─ Depth 0: Root node (no decisions made)");
            for (int i = 0; i < report.Items.Count; i++)
            {
                Console.WriteLine($"├─ Depth {i + 1}: Decision on item {report.Items[i].Name} (Include=1 or Exclude=0)");
            }
            Console.WriteLine($"└─ Depth {report.Items.Count + 1}: Complete solutions (all decisions made)");
        }
    }
}