using System.Text;
using System.Windows;
using System;

namespace Thesis.Algorithms
{
    public abstract class TSPAlgorithmBase : ITSPAlgorithm
    {
        protected double[,] distanceMatrix;
        protected double bestScore;

        public List<Point> PointsGiven { get; private set; }
        public List<int> PaintPath { get; set; }

        public TSPAlgorithmBase(List<Point> pointsGiven)
        {
            this.PointsGiven = pointsGiven;
            this.PaintPath = [];
            this.distanceMatrix = new double[pointsGiven.Count, pointsGiven.Count];
        }

        public abstract (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve();

        protected void CalculateDistanceMatrix()
        {
            int count = this.PointsGiven.Count;
            for (int i = 0; i < count; i++)
            {
                for (int j = i; j < count; j++)
                {
                    if (i == j)
                    {
                        this.distanceMatrix[i, j] = 0;
                    }
                    else
                    {
                        double distance = FindPointDistance(this.PointsGiven[i], this.PointsGiven[j]);
                        this.distanceMatrix[i, j] = distance;
                        this.distanceMatrix[j, i] = distance;
                    }
                }
            }
        }

        protected double FindPointDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow((a.X - b.X), 2) + Math.Pow((a.Y - b.Y), 2));
        }

        public double CalculateRouteCost(List<int> route)
        {
            double totalCost = 0;
            int n = route.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int city1 = route[i];
                int city2 = route[i + 1];
                totalCost += this.distanceMatrix[city1, city2];
            }
            return totalCost;
        }

        public string BuildPathString(List<int> route)
        {
            var bestPath = string.Join("->", route.Select(r => r + 1));
            return bestPath;
        }
    }
}
