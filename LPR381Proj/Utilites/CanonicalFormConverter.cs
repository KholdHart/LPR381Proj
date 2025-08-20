using System;
using System.Collections.Generic;
using LinearProgrammingProject.Models;

namespace LinearProgrammingProject.Utilities
{
    public class CanonicalFormConverter
    {
        private LinearProgrammingModel _model;
        public CanonicalFormConverter(LinearProgrammingModel model) { _model = model; }

        public double[,] CreateInitialTableau()
        {
            int m = _model.Constraints.Count, n = _model.Variables.Count;
            double[,] tableau = new double[m + 1, n + 1];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    tableau[i, j] = _model.Constraints[i].Coefficients[j];
                tableau[i, n] = _model.Constraints[i].RightHandSide;
            }
            for (int j = 0; j < n; j++)
                tableau[m, j] = -_model.ObjectiveCoefficients[j];
            tableau[m, n] = 0.0;
            return tableau;
        }
    }
}