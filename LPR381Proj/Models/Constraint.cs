using System.Collections.Generic;
using System.Linq;

namespace LinearProgrammingProject.Models
{
    public class Constraint
    {
        public List<double> Coefficients { get; set; }
        public ConstraintType Type { get; set; }
        public double RightHandSide { get; set; }
        public string Name { get; set; }

        public Constraint(List<double> coefficients, ConstraintType type, double rhs, string name = "")
        {
            Coefficients = coefficients;
            Type = type;
            RightHandSide = rhs;
            Name = name;
        }

        public bool IsValid(int expectedVariableCount)
        {
            return Coefficients != null && Coefficients.Count == expectedVariableCount;
        }

        public double EvaluateLeftSide(List<double> variableValues)
        {
            double result = 0.0;
            for (int i = 0; i < Coefficients.Count; i++)
                result += Coefficients[i] * variableValues[i];
            return result;
        }

        public bool IsSatisfied(List<double> variableValues, double tolerance = 1e-6)
        {
            double left = EvaluateLeftSide(variableValues);
            switch (Type)
            {
                case ConstraintType.LessThanOrEqual: return left <= RightHandSide + tolerance;
                case ConstraintType.GreaterThanOrEqual: return left >= RightHandSide - tolerance;
                case ConstraintType.Equal: return System.Math.Abs(left - RightHandSide) <= tolerance;
                default: return false;
            }
        }

        public double GetSlack(List<double> variableValues)
        {
            double left = EvaluateLeftSide(variableValues);
            switch (Type)
            {
                case ConstraintType.LessThanOrEqual: return RightHandSide - left;
                case ConstraintType.GreaterThanOrEqual: return left - RightHandSide;
                case ConstraintType.Equal: return 0.0;
                default: return 0.0;
            }
        }

        public string ToString(List<Variable> variables)
        {
            var terms = Coefficients.Select((c, i) => $"{(c >= 0 && i > 0 ? "+" : "")}{c:F3}*{variables[i].Name}");
            string typeSymbol = Type == ConstraintType.LessThanOrEqual ? "<=" : Type == ConstraintType.GreaterThanOrEqual ? ">=" : "=";
            return $"{string.Join(" ", terms)} {typeSymbol} {RightHandSide:F3}";
        }
    }

    public enum ConstraintType { LessThanOrEqual, GreaterThanOrEqual, Equal }
}