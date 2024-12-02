using System.Diagnostics;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPCustomAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int k;

        public List<List<int>> IntermediateRoutes { get; private set; }
        public double PreOptimizationsRouteCost { get; private set; }

        public TSPCustomAlgorithm(List<Point> pointsGiven, int k = 5) : base(pointsGiven)
        {
            this.k = k;
            this.IntermediateRoutes = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Calculate the distance matrix
            this.CalculateDistanceMatrix();

            // **Initial Route Construction Phase**

            // Find East, North, West, and South cities
            int eastCityIndex = 0;
            int westCityIndex = 0;
            int northCityIndex = 0;
            int southCityIndex = 0;

            double maxX = this.PointsGiven[0].X;
            double minX = this.PointsGiven[0].X;
            double maxY = this.PointsGiven[0].Y;
            double minY = this.PointsGiven[0].Y;

            for (int i = 0; i < this.PointsGiven.Count; i++)
            {
                double x = this.PointsGiven[i].X;
                double y = this.PointsGiven[i].Y;

                if (x > maxX)
                {
                    maxX = x;
                    eastCityIndex = i;
                }
                if (x < minX)
                {
                    minX = x;
                    westCityIndex = i;
                }
                if (y > maxY)
                {
                    maxY = y;
                    northCityIndex = i;
                }
                if (y < minY)
                {
                    minY = y;
                    southCityIndex = i;
                }
            }

            // Initialize Route with unique city indices
            var route = new List<int>();
            var initialCities = new HashSet<int> { eastCityIndex, northCityIndex, westCityIndex, southCityIndex };
            route.AddRange(initialCities);

            // Add the initial route to IntermediateRoutes
            this.IntermediateRoutes.Add(new List<int>(route));

            // Initialize Visited list
            bool[] visited = new bool[this.PointsGiven.Count];
            foreach (var cityIndex in route)
            {
                visited[cityIndex] = true;
            }

            // Initialize Open list
            var openCities = new List<int>();
            for (int i = 0; i < this.PointsGiven.Count; i++)
            {
                if (!visited[i])
                {
                    openCities.Add(i);
                }
            }

            // While Open is not empty
            while (openCities.Count > 0)
            {
                double bestCost = double.MaxValue;
                List<int>? bestRoute = null;
                int bestCityIndex = -1;

                // For each city in Open
                foreach (int cityIndex in openCities)
                {
                    // For each possible insertion position
                    for (int position = 0; position <= route.Count; position++)
                    {
                        var newRoute = new List<int>(route);
                        newRoute.Insert(position, cityIndex);

                        // Calculate the cost of the new route
                        double cost = CalculateRouteCost(newRoute);

                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            bestRoute = newRoute;
                            bestCityIndex = cityIndex;
                        }
                    }
                }

                // Update Route and Visited status
                route = bestRoute;
                visited[bestCityIndex] = true;
                openCities.Remove(bestCityIndex);

                // Add the updated route to IntermediateRoutes
                this.IntermediateRoutes.Add(new List<int>(route));
            }

            this.PreOptimizationsRouteCost = this.CalculateRouteCost(route) + this.distanceMatrix[route.Last(), route.First()];

            // **Optimization Phase**

            int n = route.Count;
            for (int i = 0; i <= n - k; i++)
            {
                var segment = route.GetRange(i, k);
                var permutations = GetPermutations(segment);
                double bestCost = double.MaxValue;
                List<int>? bestSegment = null;

                foreach (var perm in permutations)
                {
                    var newRoute = new List<int>(route);
                    newRoute.RemoveRange(i, k);
                    newRoute.InsertRange(i, perm);

                    double cost = CalculateRouteCost(newRoute);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestSegment = new List<int>(perm);
                    }
                }

                if (bestSegment != null && !bestSegment.SequenceEqual(route.GetRange(i, k)))
                {
                    route.RemoveRange(i, k);
                    route.InsertRange(i, bestSegment);

                    // Add the updated route to IntermediateRoutes
                    this.IntermediateRoutes.Add(new List<int>(route));
                }
            }

            route.Add(route[0]);

            // Recalculate total cost and BestPath
            double totalCost = this.CalculateRouteCost(route);

            // Build the best path string using numbers
            string bestPath = this.BuildPathString(route);

            stopwatch.Stop();

            // Update PaintPath and bestScore
            this.PaintPath = new List<int>(route);
            this.bestScore = totalCost;

            return (bestPath, this.bestScore, stopwatch.Elapsed);
        }

        private List<List<int>> GetPermutations(List<int> list)
        {
            var result = new List<List<int>>();
            Permute(list, 0, list.Count - 1, result);
            return result;
        }

        private void Permute(List<int> list, int l, int r, List<List<int>> result)
        {
            if (l == r)
            {
                result.Add(new List<int>(list));
            }
            else
            {
                for (int i = l; i <= r; i++)
                {
                    Utils.Swap(list, l, i);
                    Permute(list, l + 1, r, result);
                    Utils.Swap(list, l, i); // backtrack
                }
            }
        }
    }
}
