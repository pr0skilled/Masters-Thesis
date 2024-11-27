using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    public class TSPIAMPlus : TSPIAM
    {
        private int k;

        public TSPIAMPlus(List<Point> pointsGiven, int k = 5) : base(pointsGiven)
        {
            this.k = k;
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            // Start by calling the base Solve() method to get the initial route
            var initialResult = base.Solve();
            var route = new List<int>(this.PaintPath);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int n = route.Count;
            for (int i = 0; i <= n - k; i++)
            {
                var segment = route.GetRange(i, k);
                var permutations = GetPermutations(segment, 0, k - 1);
                double bestCost = double.MaxValue;
                List<int> bestSegment = null;

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

                if (bestSegment != null)
                {
                    route.RemoveRange(i, k);
                    route.InsertRange(i, bestSegment);
                }
            }

            // Recalculate total cost and BestPath
            double totalCost = CalculateRouteCost(route);
            var bestPathBuilder = new StringBuilder();
            foreach (int cityIndex in route)
            {
                bestPathBuilder.Append((char)(cityIndex + 65));
            }
            bestPathBuilder.Append((char)(route[0] + 65));
            string bestPath = bestPathBuilder.ToString();

            stopwatch.Stop();

            // Update PaintPath and bestScore
            this.PaintPath = new List<int>(route);
            this.bestScore = totalCost;

            return (bestPath, this.bestScore, stopwatch.Elapsed);
        }

        private List<List<int>> GetPermutations(List<int> list, int l, int r)
        {
            var result = new List<List<int>>();
            if (l == r)
                result.Add(new List<int>(list));
            else
            {
                for (int i = l; i <= r; i++)
                {
                    Swap(list, l, i);
                    result.AddRange(GetPermutations(list, l + 1, r));
                    Swap(list, l, i); // backtrack
                }
            }
            return result;
        }

        private void Swap(List<int> list, int i, int j)
        {
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}