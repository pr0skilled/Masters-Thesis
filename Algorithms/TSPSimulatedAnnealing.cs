using System.Windows;
using System.Diagnostics;

using Math = System.Math;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPSimulatedAnnealing : TSPAlgorithmBase, ITSPAlgorithm
    {
        private double initialTemperature;
        private double alpha;
        private int pathsChecked;

        public double InitialTemperature { get; private set; }
        public double FinalTemperature { get; private set; }
        public int PathsChecked => this.pathsChecked;

        public TSPSimulatedAnnealing(List<Point> pointsGiven, double initialTemperature = 5000, double alpha = 0.99) : base(pointsGiven)
        {
            this.initialTemperature = initialTemperature;
            this.alpha = alpha;
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopWatch = Stopwatch.StartNew();
            this.CalculateDistanceMatrix();

            var currentSolution = Enumerable.Range(0, this.PointsGiven.Count).ToList();
            var bestSolution = new List<int>(currentSolution);

            this.InitialTemperature = this.initialTemperature;
            double bestCost = this.CalculateRouteCost(bestSolution);
            double currentTemperature = this.initialTemperature;

            int sameSolutionCount = 0;
            int sameCostDifferenceCount = 0;
            this.pathsChecked = 0;

            while (sameSolutionCount < 1500 && sameCostDifferenceCount < 150000)
            {
                var neighbor = GenerateNeighbor(currentSolution);

                double currentCost = this.CalculateRouteCost(currentSolution);
                double neighborCost = this.CalculateRouteCost(neighbor);
                double costDifference = neighborCost - currentCost;

                if (costDifference < 0) // Lower cost is better
                {
                    currentSolution = neighbor;
                    sameSolutionCount = 0;
                    sameCostDifferenceCount = 0;

                    if (neighborCost < bestCost)
                    {
                        bestCost = neighborCost;
                        bestSolution = new List<int>(neighbor);
                    }
                }
                else if (costDifference == 0)
                {
                    currentSolution = neighbor;
                    sameSolutionCount = 0;
                    sameCostDifferenceCount++;
                }
                else if (Utils.Random.NextDouble() <= Math.Exp(-costDifference / currentTemperature))
                {
                    currentSolution = neighbor;
                    sameSolutionCount = 0;
                    sameCostDifferenceCount = 0;
                }
                else
                {
                    sameSolutionCount++;
                    sameCostDifferenceCount++;
                }

                this.pathsChecked++;
                currentTemperature *= this.alpha;
            }

            this.FinalTemperature = currentTemperature;

            bestSolution.Add(bestSolution.First());
            bestCost = this.CalculateRouteCost(bestSolution);

            stopWatch.Stop();
            this.PaintPath = new List<int>(bestSolution);

            return (this.BuildPathString(bestSolution), bestCost, stopWatch.Elapsed);
        }

        private List<int> GenerateNeighbor(List<int> currentSolution)
        {
            var neighbor = new List<int>(currentSolution);

            int method = Utils.Random.Next(4);

            switch (method)
            {
                case 0:
                    Reverse(neighbor);
                    break;
                case 1:
                    Insert(neighbor);
                    break;
                case 2:
                    Swap(neighbor);
                    break;
                case 3:
                    Transport(neighbor);
                    break;
            }

            return neighbor;
        }

        private static void Reverse(List<int> path)
        {
            int start = Utils.Random.Next(path.Count);
            int end = Utils.Random.Next(path.Count);

            if (start > end)
                (start, end) = (end, start);

            path.Reverse(start, end - start + 1);
        }

        private static void Insert(List<int> path)
        {
            int from = Utils.Random.Next(path.Count);
            int to = Utils.Random.Next(path.Count);

            var node = path[from];
            path.RemoveAt(from);
            path.Insert(to, node);
        }

        private static void Swap(List<int> path)
        {
            int indexA = Utils.Random.Next(path.Count);
            int indexB = Utils.Random.Next(path.Count);

            (path[indexA], path[indexB]) = (path[indexB], path[indexA]);
        }

        private static void Transport(List<int> path)
        {
            int start = Utils.Random.Next(path.Count);
            int end = Utils.Random.Next(path.Count);

            if (start > end)
                (start, end) = (end, start);

            var subroute = path.GetRange(start, end - start + 1);
            path.RemoveRange(start, end - start + 1);

            int insertPos = Utils.Random.Next(path.Count);
            path.InsertRange(insertPos, subroute);
        }
    }
}
