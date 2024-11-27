using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    public class TSPIAM : TSPAlgorithmBase, ITSPAlgorithm
    {
        public TSPIAM(List<Point> pointsGiven) : base(pointsGiven)
        {
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Calculate the distance matrix
            this.CalculateDistanceMatrix();

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
            List<int> route = new List<int>();
            HashSet<int> initialCities = new HashSet<int> { eastCityIndex, northCityIndex, westCityIndex, southCityIndex };
            route.AddRange(initialCities);

            // Initialize Visited list
            bool[] visited = new bool[this.PointsGiven.Count];
            foreach (var cityIndex in route)
            {
                visited[cityIndex] = true;
            }

            // Initialize Open list
            List<int> openCities = new List<int>();
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
                List<int> bestRoute = null;
                int bestCityIndex = -1;

                // For each city in Open
                foreach (int cityIndex in openCities)
                {
                    // For each possible insertion position
                    for (int position = 0; position <= route.Count; position++)
                    {
                        List<int> newRoute = new List<int>(route);
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
            }

            // Calculate the total cost
            double totalCost = CalculateRouteCost(route);

            // Build the BestPath string
            StringBuilder bestPathBuilder = new StringBuilder();
            foreach (int cityIndex in route)
            {
                bestPathBuilder.Append((char)(cityIndex + 65));
            }
            // Close the route by returning to the starting city
            bestPathBuilder.Append((char)(route[0] + 65));
            string bestPath = bestPathBuilder.ToString();

            this.bestScore = totalCost;

            stopwatch.Stop();

            // Set PaintPath
            this.PaintPath = new List<int>(route);
            // Ensure the path is cyclic by returning to the starting city
            this.PaintPath.Add(route[0]);

            return (bestPath, this.bestScore, stopwatch.Elapsed);
        }

        protected double CalculateRouteCost(List<int> route)
        {
            double totalCost = 0;
            int n = route.Count;
            for (int i = 0; i < n; i++)
            {
                int city1 = route[i];
                int city2 = route[(i + 1) % n]; // Ensure the route is a cycle
                totalCost += this.distanceMatrix[city1, city2];
            }
            return totalCost;
        }
    }
}
