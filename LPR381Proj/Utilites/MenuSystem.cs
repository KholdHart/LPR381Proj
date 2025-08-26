using System;
using LinearProgrammingProject.Models;
using LinearProgrammingProject.IO;
using LinearProgrammingProject.Utilities;

namespace LinearProgrammingProject.Utilities
{
    public class MenuSystem
    {
        private LinearProgrammingModel _model;
        private OutputWriter _outputWriter;

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("LP/IP Solver");
                Console.WriteLine("1. Load Model");
                Console.WriteLine("2. Solve Model");
                Console.WriteLine("3. Sensitivity Analysis");
                Console.WriteLine("4. Exit");
                int choice = GetChoice(1, 4);
                if (choice == 1) LoadModel();
                else if (choice == 2) SolveModel();
                else if (choice == 3) SensitivityAnalysis();
                else break;
            }
        }

        private void LoadModel()
        {
            Console.Write("Input file path: ");
            string path = Console.ReadLine();
            try
            {
                var reader = new InputReader(path);
                _model = reader.ReadModel();
                Console.WriteLine("Model loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Wait();
        }

        private void SolveModel()
        {
            if (_model == null) { Console.WriteLine("Load a model first."); Wait(); return; }
            Console.WriteLine("Select Algorithm:");
            Console.WriteLine("1. Primal Simplex");
            Console.WriteLine("2. Revised Primal Simplex");
            Console.WriteLine("3. Branch & Bound Simplex");
            Console.WriteLine("4. Cutting Plane");
            Console.WriteLine("5. Branch & Bound Knapsack");
            int alg = GetChoice(1, 5);
            // Placeholder: Simulate solution
            /*_model.Status = SolutionStatus.Optimal;
            _model.OptimalValue = 123.456;
            foreach (var v in _model.Variables) v.Value = 1.0;*/
            Console.WriteLine("Solving...");
            System.Threading.Thread.Sleep(1000); // Simulate time delay
            var solver = new PrimalSimplexSolver();
            var result = solver.Solve(_model); // Example call to solver
            foreach (var snapshot in result.IterationSnapshots)
            {
                Console.WriteLine(snapshot);
            }
            _outputWriter = new OutputWriter("output.txt");
            _outputWriter.WriteSolution(_model, $"Algorithm {alg}", 5);
            _outputWriter.SaveToFile();
            Console.WriteLine("Solved. Output written to output.txt");
            Wait();
        }

        private void SensitivityAnalysis()
        {
            Console.WriteLine("Sensitivity Analysis (not implemented)");
            Wait();
        }

        private int GetChoice(int min, int max)
        {
            int c;
            while (!int.TryParse(Console.ReadLine(), out c) || c < min || c > max)
                Console.Write($"Enter choice ({min}-{max}): ");
            return c;
        }

        private void Wait()
        {
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
