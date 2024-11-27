using System.Windows;
using System.Diagnostics;
using System.Text;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPSimulatedAnnealing : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int pathsChecked;
        private int totalPaths;

        public int PathsChecked => this.pathsChecked;
        public int TotalPaths => this.totalPaths;

        public TSPSimulatedAnnealing(List<Point> pointsGiven) : base(pointsGiven)
        {
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            double temperatureReductionFactor = 0.99; // Slow cooling for better exploration
            double randomChoice;
            string candidatePath;
            double acceptanceProbability;

            var stopWatch = new Stopwatch();
            double newScore;

            stopWatch.Start();
            this.CalculateDistanceMatrix();

            // Initialize with the default path: A -> B -> C -> ... -> A
            StringBuilder basePath = new();
            char startingCity = 'A';
            basePath.Append(startingCity);

            for (int i = 1; i < this.PointsGiven.Count; i++)
            {
                char c = (char)(65 + i);
                basePath.Append(c);
            }
            basePath.Append(startingCity); // Return to starting city

            string currentPath = basePath.ToString();
            double initialTemperature = this.FindPathDistance(currentPath);
            double bestScore = initialTemperature;
            double currentTemperature = initialTemperature;
            int maxIterationsPerEpoch = 100 * this.PointsGiven.Count;

            var rnd = Utils.Random;
            this.pathsChecked = 0;
            this.totalPaths = Utils.Factorial(this.PointsGiven.Count);

            // Log the initial state
            Console.WriteLine($"Initial Path: {currentPath}, Initial Distance: {initialTemperature}");

            while (currentTemperature > 1e-5) // Stop when temperature becomes very small
            {
                for (int i = 0; i < maxIterationsPerEpoch; i++)
                {
                    int start = rnd.Next(1, this.PointsGiven.Count);
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
                    newScore = this.FindPathDistance(candidatePath);
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

            this.PaintPath = Utils.StringToIntArray(currentPath);
            Console.WriteLine($"Final Path: {currentPath}, Final Distance: {bestScore:F2}, Time: {stopWatch.Elapsed}");

            return (currentPath, bestScore, stopWatch.Elapsed);
        }


        // Helper method to validate path
        private bool IsPathValid(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var visited = new HashSet<char>();
            foreach (var c in path)
            {
                if (visited.Contains(c) && c != path[^1]) return false; // Detect duplicates
                visited.Add(c);
            }

            return path[0] == path[^1]; // Ensure it starts and ends at the same city
        }

        public static string Reverse(string a, int x, int y)
        {
            // Reverse the subsection of the string between indices x and y (exclusive of start/end cities)
            int start = Math.Min(x, y);
            int end = Math.Max(x, y);

            // Ensure the reversal does not include the starting/ending city
            if (start == 0 || end == a.Length - 1) return a;

            int width = end - start + 1;
            string sub = a.Substring(start, width);
            string head = a[..start];
            string tail = a[(end + 1)..];
            char[] substring = sub.ToCharArray();
            Array.Reverse(substring);
            string rev = new(substring);

            return string.Concat(head, rev, tail);
        }

        public static string Transport(string a, int x, int y)
        {
            // Slice out the substring and move it to another valid position
            int start = Math.Min(x, y);
            int end = Math.Max(x, y);

            // Ensure the transport does not include the starting/ending city
            if (start == 0 || end == a.Length - 1) return a;

            int width = end - start + 1;
            string sub = a.Substring(start, width);
            string head = a[..start];
            string tail = a[(end + 1)..];
            string trans = string.Concat(head, tail);

            // Choose a valid insertion point (excluding the start/end cities)
            int insertPoint = Utils.Random.Next(1, trans.Length - 1);

            return trans.Insert(insertPoint, sub);
        }


        public string GetCostSummary()
        {
            return $"Paths Checked ({this.pathsChecked}/{this.totalPaths}) = {((double)this.pathsChecked / this.totalPaths * 100):F08}%";
        }
    }
}
