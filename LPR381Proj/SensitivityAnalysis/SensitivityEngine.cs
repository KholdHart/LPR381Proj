
using System;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.SensitivityAnalysis
{
    public class SensitivityEngine
    {
        private LinearProgrammingModel _model;

        public SensitivityEngine(LinearProgrammingModel model)
        {
            _model = model;
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
                Console.WriteLine("==== Sensitivity Analysis Menu ====");
                Console.WriteLine("1. Show shadow prices (approx)");
                Console.WriteLine("2. Change RHS and see new objective");
                Console.WriteLine("3. Show dual problem");
                Console.WriteLine("0. Exit");
                Console.Write("Choice: ");

                string choice = Console.ReadLine();
                if (choice == "1") ShowShadowPrices();
                else if (choice == "2") ChangeRHS();
                else if (choice == "3")
                {
                    var dual = new DualityHandler(_model);
                    dual.ShowDualForm();
                }
                else if (choice == "0") break;
                else Console.WriteLine("Invalid option.");
            }
        }
        private void ShowShadowPrices()
        {
            Console.WriteLine("Shadow prices (very rough):");
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                double slack = _model.Constraints[i].GetSlack(_model.Variables.ConvertAll(v => v.Value));
                Console.WriteLine($"Constraint {i + 1}: shadow price ≈ {slack}");
            }
        }
        private void ChangeRHS()
        {
            Console.Write("Enter constraint index to change: ");
            int index;
            if (!int.TryParse(Console.ReadLine(), out index) || index < 1 || index > _model.Constraints.Count)
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            Console.Write("Enter new RHS value: ");
            double newRhs;
            if (!double.TryParse(Console.ReadLine(), out newRhs))
            {
                Console.WriteLine("Invalid number."); return;
            }

            _model.Constraints[index - 1].RightHandSide = newRhs;
            Console.WriteLine($"Constraint {index} RHS updated to {newRhs}");
            Console.WriteLine("Re-solve the model to see actual effect.");
        }
    }
};
