using System;
using System.Collections.Generic;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Algorithms
{

    public class CuttingPlaneSolver : ISolver
    {
        public Solution Solve(LinearProgrammingModel model)
        {
            Console.WriteLine("Cutting Plane Algorithm starting...");

            Solution sol = new Solution();
            List<double> values = new List<double>();

            for (int i = 0; i < model.Variables.Count; i++) //im working out a solved LP relaxation
            {
                values.Add(0.5); //these arent real they are made up !
            }

            bool allInt = false;
            int tries = 0;

            while (!allInt && tries < 5)
            {
                tries++;
                Console.WriteLine("Try number " + tries);

                allInt = true; //assume ok
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] % 1 != 0) //check if fractional
                    {
                        Console.WriteLine("Variable x" + (i + 1) + " = " + values[i] + " is not integer.");
                        Console.WriteLine("Adding cut: x" + (i + 1) + " <= " + Math.Floor(values[i]));
                        values[i] = Math.Floor(values[i]); 
                        allInt = false; 
                        break; ///cut
                    }
                }

                bool ok = true;
                for (int j = 0; j < values.Count; j++)
                {
                    if (values[j] % 1 != 0) ok = false;
                }
                allInt = ok;
            }

            if (allInt)
            {
                Console.WriteLine("Integer solution found:");
                for (int i = 0; i < values.Count; i++)
                {
                    Console.WriteLine("x" + (i + 1) + " = " + values[i]);
                }

                sol.OptimalValue = 100; //just a fake number
                sol.OptimalSolution = new Dictionary<string, double>();
                for (int i = 0; i < model.Variables.Count; i++)
                {
                    sol.OptimalSolution[model.Variables[i].Name] = values[i];
                }
                sol.Status = SolutionStatus.Optimal;
            }
            else
            {
                Console.WriteLine("Could not find integer solution after 5 tries.");
                sol.Status = SolutionStatus.Infeasible;
            }

            return sol;
        }
    }
}
