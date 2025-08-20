using System;

namespace LinearProgrammingProject.Models
{
    // Decision variable for LP/IP models
    public class Variable
    {
        public string Name { get; set; }
        public VariableType Type { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double Value { get; set; }

        public Variable(string name, VariableType type = VariableType.Continuous)
        {
            Name = name;
            Type = type;
            SetDefaultBounds();
            Value = 0.0;
        }

        public Variable(string name, VariableType type, double lowerBound, double upperBound)
        {
            Name = name;
            Type = type;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Value = 0.0;
        }

        // Set default bounds based on type
        private void SetDefaultBounds()
        {
            switch (Type)
            {
                case VariableType.Continuous:
                case VariableType.Integer:
                    LowerBound = 0.0;
                    UpperBound = double.PositiveInfinity;
                    break;
                case VariableType.Binary:
                    LowerBound = 0.0;
                    UpperBound = 1.0;
                    break;
                case VariableType.Unrestricted:
                    LowerBound = double.NegativeInfinity;
                    UpperBound = double.PositiveInfinity;
                    break;
            }
        }

        // Check if value is valid for type and bounds
        public bool IsValid(double value)
        {
            if (value < LowerBound || value > UpperBound)
                return false;
            if (Type == VariableType.Integer)
                return Math.Abs(value - Math.Round(value)) < 1e-6;
            if (Type == VariableType.Binary)
                return value == 0.0 || value == 1.0;
            return true;
        }

        // Set value if valid
        public bool SetValue(double value)
        {
            if (IsValid(value))
            {
                Value = value;
                return true;
            }
            return false;
        }

        // Get bounds as string for output
        public string GetBoundsString()
        {
            switch (Type)
            {
                case VariableType.Binary:
                    return $"{Name} ∈ {{0, 1}}";
                case VariableType.Integer:
                    return $"{Name} ∈ Z, {LowerBound} ≤ {Name} ≤ {UpperBound}";
                case VariableType.Unrestricted:
                    return $"{Name} unrestricted";
                default:
                    return $"{LowerBound} ≤ {Name} ≤ {UpperBound}";
            }
        }
    }

    public enum VariableType
    {
        Continuous,
        Integer,
        Binary,
        Unrestricted
    }
}