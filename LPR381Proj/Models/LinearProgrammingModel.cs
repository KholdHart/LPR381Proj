using System.Collections.Generic;
using System.Linq;

namespace LinearProgrammingProject.Models
{
    public class LinearProgrammingModel
    {
        public ObjectiveType ObjectiveType { get; set; }
        public List<Variable> Variables { get; set; } = new List<Variable>();
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public List<double> ObjectiveCoefficients { get; set; } = new List<double>();
        public double OptimalValue { get; set; }
        public Dictionary<string, double> OptimalSolution { get; set; } = new Dictionary<string, double>();
        public SolutionStatus Status { get; set; } = SolutionStatus.Unknown;

        public int VariableCount => Variables.Count;
        public int ConstraintCount => Constraints.Count;

        public bool IsValid()
        {
            if (Variables.Count == 0 || ObjectiveCoefficients.Count != Variables.Count)
                return false;
            foreach (var c in Constraints)
                if (!c.IsValid(Variables.Count)) return false;
            return true;
        }

        public string GetObjectiveFunctionString()
        {
            var objType = ObjectiveType == ObjectiveType.Maximize ? "Maximize" : "Minimize";
            var terms = Variables.Select((v, i) => $"{(ObjectiveCoefficients[i] >= 0 ? "+" : "")}{ObjectiveCoefficients[i]:F3}*{v.Name}");
            return $"{objType}: {string.Join(" ", terms)}";
        }

        public List<string> GetConstraintStrings()
        {
            return Constraints.Select((c, i) => $"C{i + 1}: {c.ToString(Variables)}").ToList();
        }

        public bool IsIntegerProgram()
        {
            return Variables.Any(v => v.Type == VariableType.Integer || v.Type == VariableType.Binary);
        }

        public bool IsBinaryProgram()
        {
            return Variables.Any(v => v.Type == VariableType.Binary);
        }
    }

    public enum ObjectiveType { Maximize, Minimize }
    public enum SolutionStatus { Unknown, Optimal, Infeasible, Unbounded, Error }
}