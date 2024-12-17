using System.Diagnostics;
using System.Windows;

namespace Thesis.Algorithms
{
    public class TSPCustomAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        public List<List<int>> IntermediateRoutes { get; private set; }
        public double PreOptimizationsRouteCost { get; private set; }

        public TSPCustomAlgorithm(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.IntermediateRoutes = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopwatch = new Stopwatch();

            // Calculate the distance matrix
            this.CalculateDistanceMatrix();

            stopwatch.Start();
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
            var openCitiesQueue = new PriorityQueue<int, double>();
            foreach (var cityIndex in Enumerable.Range(0, this.PointsGiven.Count).Where(i => !visited[i]))
            {
                openCitiesQueue.Enqueue(cityIndex, this.distanceMatrix[route.Last(), cityIndex]);
            }

            // While Open Cities Queue is not empty
            while (openCitiesQueue.Count > 0)
            {
                // Narrow down to top candidates
                var narrowedOpenCities = openCitiesQueue.UnorderedItems
                    .OrderBy(item => item.Priority)
                    .Take(10) // Adjust subset size
                    .Select(item => item.Element)
                    .ToList();

                var (City, Route, Cost) = narrowedOpenCities.AsParallel()
                    .Select(cityIndex =>
                    {
                        double localBestCost = double.MaxValue;
                        List<int>? localBestRoute = null;

                        foreach (var position in Enumerable.Range(0, route.Count + 1))
                        {
                            var newRoute = new List<int>(route);
                            newRoute.Insert(position, cityIndex);
                            double cost = this.CalculateRouteCost(newRoute);

                            if (cost < localBestCost)
                            {
                                localBestCost = cost;
                                localBestRoute = newRoute;
                            }
                        }

                        return (City: cityIndex, Route: localBestRoute, Cost: localBestCost);
                    })
                    .OrderBy(result => result.Cost)
                    .First();

                route = Route;
                visited[City] = true;

                // Remove City from openCitiesQueue
                openCitiesQueue = new PriorityQueue<int, double>(openCitiesQueue.UnorderedItems
                    .Where(item => item.Element != City));

                this.IntermediateRoutes.Add(new List<int>(route));
            }

            this.PreOptimizationsRouteCost = this.CalculateRouteCost(route) + this.distanceMatrix[route.Last(), route.First()];

            // **Optimization Phase**

            this.ReinsertionOptimization(route);

            route.Add(route[0]);
            stopwatch.Stop();

            // Recalculate total cost and BestPath
            double totalCost = this.CalculateRouteCost(route);

            // Build the best path string using numbers
            string bestPath = this.BuildPathString(route);

            // Update PaintPath and bestScore
            this.PaintPath = new List<int>(route);
            this.bestScore = totalCost;

            return (bestPath, this.bestScore, stopwatch.Elapsed);
        }

        private void ReinsertionOptimization(List<int> route)
        {
            bool improvement = true;

            while (improvement)
            {
                improvement = false;

                for (int i = 0; i < route.Count - 1; i++)
                {
                    int city = route[i];
                    route.RemoveAt(i);

                    double bestInsertionCost = double.MaxValue;
                    int bestPosition = -1;

                    for (int j = 0; j < route.Count; j++)
                    {
                        int prevCity = route[j];
                        int nextCity = route[(j + 1) % route.Count];
                        double insertionCost = CalculateInsertionCost(prevCity, city, nextCity);

                        if (insertionCost < bestInsertionCost)
                        {
                            bestInsertionCost = insertionCost;
                            bestPosition = j + 1;
                        }
                    }

                    route.Insert(bestPosition, city);

                    if (bestInsertionCost < 0) improvement = true;
                }
            }
        }

        private double CalculateInsertionCost(int prevCity, int city, int nextCity)
        {
            return this.distanceMatrix[prevCity, city]
                 + this.distanceMatrix[city, nextCity]
                 - this.distanceMatrix[prevCity, nextCity];
        }
    }
}
