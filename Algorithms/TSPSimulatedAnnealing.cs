using System.Diagnostics;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPSimulatedAnnealing : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int pathsChecked;
        private int totalPaths;

        public int PathsChecked => this.pathsChecked;
        public int TotalPaths => this.totalPaths;

        public double InitialTemperature { get; private set; }
        public double FinalTemperature { get; private set; }

        public TSPSimulatedAnnealing(List<Point> pointsGiven) : base(pointsGiven)
        {
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            double temperatureReductionFactor = 0.99; // Slow cooling for better exploration
            double randomChoice;
            List<int> candidatePath;
            double acceptanceProbability;

            var stopWatch = new Stopwatch();
            double newScore;

            stopWatch.Start();
            this.CalculateDistanceMatrix();

            // Initialize the current path with city indices and complete the cycle
            var currentPath = new List<int>();
            for (int i = 0; i < this.PointsGiven.Count; i++)
            {
                currentPath.Add(i);
            }
            currentPath.Add(0); // Return to starting city

            this.InitialTemperature = this.CalculateRouteCost(currentPath);
            double bestScore = this.InitialTemperature;
            double currentTemperature = this.InitialTemperature;
            int maxIterationsPerEpoch = 100 * this.PointsGiven.Count;

            var rnd = Utils.Random;
            this.pathsChecked = 0;
            this.totalPaths = Utils.Factorial(this.PointsGiven.Count);

            while (currentTemperature > 1e-5) // Stop when temperature becomes very small
            {
                for (int i = 0; i < maxIterationsPerEpoch; i++)
                {
                    int start = rnd.Next(1, this.PointsGiven.Count); // Exclude starting city at index 0
                    int end = rnd.Next(1, this.PointsGiven.Count);

                    while (end == start)
                    {
                        end = rnd.Next(1, this.PointsGiven.Count);
                    }

                    // Randomly choose a manipulation method
                    randomChoice = rnd.NextDouble();
                    if (randomChoice < 0.5)
                        candidatePath = Reverse(currentPath, start, end);
                    else
                        candidatePath = Transport(currentPath, start, end);

                    // Evaluate the candidate path
                    newScore = this.CalculateRouteCost(candidatePath);
                    double scoreDifference = newScore - bestScore;
                    acceptanceProbability = Math.Exp(-scoreDifference / currentTemperature);

                    randomChoice = rnd.NextDouble();
                    if (scoreDifference < 0 || acceptanceProbability > randomChoice)
                    {
                        currentPath = candidatePath;
                        if (newScore < bestScore)
                        {
                            bestScore = newScore;
                        }
                    }

                    this.pathsChecked++;
                }

                // Reduce temperature
                currentTemperature *= temperatureReductionFactor;
                Console.WriteLine($"Temperature: {currentTemperature:F2}, Best Score: {bestScore:F2}");
            }

            stopWatch.Stop();

            this.FinalTemperature = currentTemperature;

            this.PaintPath = new List<int>(currentPath);

            string bestPathString = this.BuildPathString(currentPath);

            return (bestPathString, bestScore, stopWatch.Elapsed);
        }

        public static List<int> Reverse(List<int> path, int x, int y)
        {
            int start = Math.Min(x, y);
            int end = Math.Max(x, y);

            var reversedSection = path.GetRange(start, end - start + 1);
            reversedSection.Reverse();

            var newPath = new List<int>(path);
            newPath.RemoveRange(start, end - start + 1);
            newPath.InsertRange(start, reversedSection);

            return newPath;
        }

        public static List<int> Transport(List<int> path, int x, int y)
        {
            int start = Math.Min(x, y);
            int end = Math.Max(x, y);

            // Ensure the transport does not include the starting/ending city
            if (start == 0 || end == path.Count - 1) return path;

            var sub = path.GetRange(start, end - start + 1);
            var trans = new List<int>(path);
            trans.RemoveRange(start, end - start + 1);

            // Choose a valid insertion point (excluding the start/end cities)
            int insertPoint = Utils.Random.Next(1, trans.Count - 1);

            trans.InsertRange(insertPoint, sub);

            return trans;
        }

        public string GetCostSummary()
        {
            return $"Paths Checked ({this.pathsChecked}/{this.totalPaths}) = {((double)this.pathsChecked / this.totalPaths * 100):F08}%";
        }
    }
}
