using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPBruteForce : TSPAlgorithmBase, ITSPAlgorithm
    {
        private List<string> paths;

        public TSPBruteForce(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.paths = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            string bestPath = string.Empty;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            this.CalculateDistanceMatrix();

            // Generate the base path (excluding the starting city at the end)
            var basePath = new StringBuilder();
            for (int i = 1; i < this.PointsGiven.Count; i++)
            {
                basePath.Append((char)(65 + i));
            }

            // The starting city is fixed at the beginning and end
            char startingCity = 'A';

            // Generate all permutations of the intermediate cities
            this.Permute(basePath.ToString(), 0, basePath.Length - 1);

            this.bestScore = double.MaxValue;

            foreach (var path in this.paths)
            {
                // Construct the full path with the starting city at the beginning and end
                string fullPath = startingCity + path + startingCity;

                double newScore = this.FindPathDistance(fullPath);
                if (newScore < this.bestScore)
                {
                    this.bestScore = newScore;
                    bestPath = fullPath;
                }
            }

            stopWatch.Stop();

            this.PaintPath = Utils.StringToIntArray(bestPath);

            return (bestPath, this.bestScore, stopWatch.Elapsed);
        }

        private void Permute(string str, int l, int r)
        {
            if (l == r)
            {
                this.paths.Add(str);
            }
            else
            {
                for (int i = l; i <= r; i++)
                {
                    str = Utils.Swap(str, l, i);
                    this.Permute(str, l + 1, r);
                    str = Utils.Swap(str, l, i); // backtrack
                }
            }
        }
    }
}
