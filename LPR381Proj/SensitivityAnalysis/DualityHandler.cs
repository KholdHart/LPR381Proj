using System;
using System.Collections.Generic;
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

            Console.WriteLine("==== Dual Problem ====");
            var dualObjType = _model.ObjectiveType == ObjectiveType.Maximize ? "Minimize" : "Maximize";
            Console.WriteLine($"{dualObjType}:");

            int m = _model.Constraints.Count;
            int n = _model.Variables.Count;
            for (int i = 0; i < m; i++)
            {
                Console.Write($" y{i + 1} ");
            }
            Console.WriteLine();

            for (int j = 0; j < n; j++)
            {
                Console.Write("Constraint for x" + (j + 1) + ": ");
                for (int i = 0; i < m; i++)
                {
                    Console.Write(_model.Constraints[i].Coefficients[j] + " * y" + (i + 1) + " ");
                }
                string symbol = _model.ObjectiveType == ObjectiveType.Maximize ? ">=" : "<=";
                Console.WriteLine(symbol + " " + _model.ObjectiveCoefficients[j]);
            }
            Console.Write("Dual objective RHS = ");
            for (int i = 0; i < m; i++)
            {
                Console.Write(_model.Constraints[i].RightHandSide + " * y" + (i + 1) + " ");
            }
            Console.WriteLine();

        }
    }
}