using System.Windows;
using System.Diagnostics;

namespace Thesis.Algorithms
{
    public class TSPConvexHullAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        // Properties to store Convex Hull metrics
        public double HullPerimeter { get; private set; }
        public int HullPointCount { get; private set; }

        public TSPConvexHullAlgorithm(List<Point> pointsGiven) : base(pointsGiven)
        {
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            this.CalculateDistanceMatrix();

            // Calculate the Convex Hull
            var convexHull = this.GetConvexHull(this.PointsGiven);
            this.HullPointCount = convexHull.Count;

            var route = convexHull.Select(p => this.PointsGiven.IndexOf(p)).ToList();

            // Calculate Hull Perimeter
            this.HullPerimeter = this.CalculateHullPerimeter(convexHull);

            // Add remaining points to the route
            var visited = new HashSet<int>(route);
            var remainingPoints = Enumerable.Range(0, this.PointsGiven.Count)
                                            .Where(i => !visited.Contains(i))
                                            .ToList();

            foreach (var pointIndex in remainingPoints)
            {
                int bestPosition = -1;
                double bestCost = double.MaxValue;

                for (int i = 0; i < route.Count; i++)
                {
                    var newRoute = new List<int>(route);
                    newRoute.Insert(i, pointIndex);
                    double cost = this.CalculateRouteCost(newRoute);

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestPosition = i;
                    }
                }

                route.Insert(bestPosition, pointIndex);
            }

            // Return to the start to complete the route
            route.Add(route[0]);

            // Calculate total cost and build the path
            double totalCost = this.CalculateRouteCost(route);
            string bestPath = this.BuildPathString(route);

            stopwatch.Stop();

            this.PaintPath = route;

            return (bestPath, totalCost, stopwatch.Elapsed);
        }

        private List<Point> GetConvexHull(List<Point> points)
        {
            if (points.Count <= 1)
                return new List<Point>(points);

            // Sort points by X, then by Y
            var sortedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

            // Build the lower hull
            var lowerHull = new List<Point>();
            foreach (var point in sortedPoints)
            {
                while (lowerHull.Count >= 2 && this.CrossProduct(lowerHull[^2], lowerHull[^1], point) <= 0)
                {
                    lowerHull.RemoveAt(lowerHull.Count - 1);
                }
                lowerHull.Add(point);
            }

            // Build the upper hull
            var upperHull = new List<Point>();
            foreach (var point in sortedPoints.AsEnumerable().Reverse())
            {
                while (upperHull.Count >= 2 && this.CrossProduct(upperHull[^2], upperHull[^1], point) <= 0)
                {
                    upperHull.RemoveAt(upperHull.Count - 1);
                }
                upperHull.Add(point);
            }

            // Remove the last point of each half because it’s repeated
            lowerHull.RemoveAt(lowerHull.Count - 1);
            upperHull.RemoveAt(upperHull.Count - 1);

            // Combine lower and upper hulls
            lowerHull.AddRange(upperHull);
            return lowerHull;
        }

        private double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        }

        private double CalculateHullPerimeter(List<Point> hullPoints)
        {
            double perimeter = 0.0;
            for (int i = 0; i < hullPoints.Count; i++)
            {
                var current = hullPoints[i];
                var next = hullPoints[(i + 1) % hullPoints.Count];
                perimeter += this.FindPointDistance(current, next);
            }
            return perimeter;
        }
    }
}
