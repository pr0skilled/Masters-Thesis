using System.Diagnostics;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPBruteForce : TSPAlgorithmBase, ITSPAlgorithm
    {
        private List<List<int>> paths;

        public TSPBruteForce(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.paths = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            List<int> bestRoute = null;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            this.CalculateDistanceMatrix();

            // Generate the list of city indices excluding the starting city (index 0)
            var cityIndices = new List<int>();
            for (int i = 1; i < this.PointsGiven.Count; i++)
            {
                cityIndices.Add(i);
            }

            // Generate all permutations of the intermediate cities
            this.Permute(cityIndices, 0, cityIndices.Count - 1);

            this.bestScore = double.MaxValue;

            foreach (var path in this.paths)
            {
                // Construct the full route with the starting city at the beginning and end
                var fullRoute = new List<int> { 0 };
                fullRoute.AddRange(path);
                fullRoute.Add(0);

                double newScore = this.CalculateRouteCost(fullRoute);
                if (newScore < this.bestScore)
                {
                    this.bestScore = newScore;
                    bestRoute = new List<int>(fullRoute);
                }
            }

            stopWatch.Stop();

            this.PaintPath = bestRoute;

            // Build the best path string using numbers
            string bestPathString = this.BuildPathString(bestRoute);

            return (bestPathString, this.bestScore, stopWatch.Elapsed);
        }

        private void Permute(List<int> list, int l, int r)
        {
            if (l == r)
            {
                this.paths.Add(new List<int>(list));
            }
            else
            {
                for (int i = l; i <= r; i++)
                {
                    Utils.Swap(list, l, i);
                    this.Permute(list, l + 1, r);
                    Utils.Swap(list, l, i); // backtrack
                }
            }
        }
    }
}
