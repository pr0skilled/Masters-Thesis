using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPCustomAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        private Dictionary<string, double> nearestPoints;
        private Dictionary<string, double> subPaths;
        private string freePoints;
        private int maxCost;
        private string bestPath;
        private bool hasConverged;
        private Stopwatch stopWatch;

        public TSPCustomAlgorithm(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.nearestPoints = [];
            this.subPaths = [];
            this.freePoints = string.Empty;
            this.bestPath = string.Empty;
            this.hasConverged = false;
            this.stopWatch = new Stopwatch();
            this.maxCost = 0;

            this.Initialize();
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            this.stopWatch.Start();

            while (!this.hasConverged)
            {
                this.Step();
            }

            this.stopWatch.Stop();

            return (this.bestPath, this.bestScore, this.stopWatch.Elapsed);
        }

        private void Initialize()
        {
            this.numberOfPoints = this.PointsGiven.Count;
            this.distanceMatrix = new double[this.numberOfPoints, this.numberOfPoints];
            this.nearestPoints.Clear();
            this.subPaths.Clear();
            this.freePoints = string.Empty;
            this.bestPath = string.Empty;
            this.hasConverged = false;
            this.stopWatch.Reset();

            this.CalculateDistanceMatrix();

            var sb = new StringBuilder();

            for (int i = 0; i < this.numberOfPoints; i++)
            {
                sb.Append((char)(65 + i));
            }

            this.freePoints = sb.ToString();

            this.InitializeNearestPoints();
        }

        private void InitializeNearestPoints()
        {
            this.maxCost = int.MaxValue;

            for (int i = 0; i < this.numberOfPoints; i++)
            {
                char pointA = (char)(65 + i);
                double minPathCost = this.maxCost;
                char pointB = '\0';

                for (int j = 0; j < this.numberOfPoints; j++)
                {
                    if (i != j)
                    {
                        char tempPoint = (char)(65 + j);
                        string path = $"{pointA}{tempPoint}";
                        string revPath = $"{tempPoint}{pointA}";

                        if (!this.nearestPoints.ContainsKey(path) && !this.nearestPoints.ContainsKey(revPath))
                        {
                            double dist = this.distanceMatrix[i, j];
                            if (dist < minPathCost)
                            {
                                minPathCost = dist;
                                pointB = tempPoint;
                            }
                        }
                    }
                }

                if (pointB != '\0')
                {
                    string minPath = $"{pointA}{pointB}";
                    this.nearestPoints.Add(minPath, minPathCost);
                }
            }

            // Sort nearestPoints by value (distance) in ascending order
            this.nearestPoints = this.nearestPoints.OrderBy(kvp => kvp.Value)
                                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void Step()
        {
            if (this.hasConverged)
                return;

            this.TSP_NEW_STEP(ref this.bestPath);

            if (this.subPaths.Count == 1 && this.subPaths.First().Key.Length == this.numberOfPoints)
            {
                this.hasConverged = true;

                // Complete the cycle by adding the starting city at the end
                if (this.bestPath[0] != this.bestPath[^1])
                {
                    this.bestPath += this.bestPath[0];
                }

                this.bestScore = this.FindPathDistance(this.bestPath);
                this.PaintPath = Utils.StringToIntArray(this.bestPath);
            }
        }

        public bool HasConverged => this.hasConverged;

        public (string BestPath, double BestScore, TimeSpan ElapsedTime) GetResult()
        {
            return (this.bestPath, this.bestScore, this.stopWatch.Elapsed);
        }

        private void TSP_NEW_STEP(ref string bestPath)
        {
            if (!this.nearestPoints.Any())
            {
                // All nearest points have been processed
                return;
            }

            string nearestPoint = this.nearestPoints.First().Key;
            this.nearestPoints.Remove(nearestPoint);

            List<string> matchedPaths = this.subPaths.Keys
                .Where(p => p.Contains(nearestPoint[0]) || p.Contains(nearestPoint[1]))
                .ToList();

            if (matchedPaths.Count == 0)
            {
                // Create a new subpath
                this.subPaths.Add(nearestPoint, this.FindPathDistance(nearestPoint));
            }
            else if (matchedPaths.Count == 1)
            {
                // Merge the nearest point with the existing subpath
                string existingPath = matchedPaths[0];
                string newPath = this.MergePoint(existingPath, nearestPoint);

                if (newPath != null)
                {
                    this.subPaths.Remove(existingPath);
                    this.subPaths.Add(newPath, this.FindPathDistance(newPath));
                }
                // If newPath is null, cannot merge; skip this edge
            }
            else if (matchedPaths.Count == 2)
            {
                // Attempt to merge two subpaths
                string path1 = matchedPaths[0];
                string path2 = matchedPaths[1];
                string newPath = this.MergePaths(path1, path2, nearestPoint);

                if (newPath != null)
                {
                    this.subPaths.Remove(path1);
                    this.subPaths.Remove(path2);
                    this.subPaths.Add(newPath, this.FindPathDistance(newPath));
                }
                else
                {
                    // Cannot merge the subpaths with this edge; skip it
                    // Optionally, you can decide whether to re-add the edge back to nearestPoints
                }
            }

            // Update freePoints
            this.UpdateFreePoints();

            // Remove any paths that would create a cycle prematurely
            this.RemoveInvalidNearestPoints();
        }

        private void UpdateFreePoints()
        {
            var usedPoints = new HashSet<char>(this.subPaths.Keys.SelectMany(p => p));
            var allPoints = new HashSet<char>(Enumerable.Range(0, this.numberOfPoints).Select(i => (char)(65 + i)));
            var freePointsSet = allPoints.Except(usedPoints);
            this.freePoints = new string(freePointsSet.ToArray());
        }

        private void RemoveInvalidNearestPoints()
        {
            var invalidPaths = new List<string>();

            foreach (var path in this.nearestPoints.Keys)
            {
                foreach (var subPath in this.subPaths.Keys)
                {
                    if (subPath.Contains(path[0]) && subPath.Contains(path[1]))
                    {
                        invalidPaths.Add(path);
                        break;
                    }
                }
            }

            foreach (var path in invalidPaths)
            {
                this.nearestPoints.Remove(path);
            }
        }

        private bool IsNotInNearestPoints(char c)
        {
            return !this.nearestPoints.Keys.Any(key => key[0] == c || key[1] == c);
        }

        private string MergePaths(string str1, string str2, string connectingEdge)
        {
            char str1_start = str1[0];
            char str1_end = str1[^1];
            char str2_start = str2[0];
            char str2_end = str2[^1];
            char edge_start = connectingEdge[0];
            char edge_end = connectingEdge[1];

            // Align str1 with connectingEdge
            if (str1_end == edge_start)
            {
                // Already aligned
            }
            else if (str1_start == edge_start)
            {
                str1 = ReverseString(str1);
            }
            else if (str1_end == edge_end)
            {
                connectingEdge = ReverseString(connectingEdge);
            }
            else if (str1_start == edge_end)
            {
                str1 = ReverseString(str1);
                connectingEdge = ReverseString(connectingEdge);
            }
            else
            {
                // Cannot align str1 with connectingEdge
                return null;
            }

            // Update edge_start and edge_end after potential reversal
            edge_start = connectingEdge[0];
            edge_end = connectingEdge[1];

            // Align str2 with connectingEdge
            if (str2_start == edge_end)
            {
                // Already aligned
            }
            else if (str2_end == edge_end)
            {
                str2 = ReverseString(str2);
            }
            else if (str2_start == edge_start)
            {
                str2 = ReverseString(str2);
                connectingEdge = ReverseString(connectingEdge);
            }
            else if (str2_end == edge_start)
            {
                connectingEdge = ReverseString(connectingEdge);
            }
            else
            {
                // Cannot align str2 with connectingEdge
                return null;
            }

            // Merge the paths
            string mergedPath = str1 + str2.Substring(1);
            return mergedPath;
        }

        private string MergePoint(string existingPath, string edge)
        {
            char mergePoint = edge[0] == existingPath[0] ? edge[1] : edge[0];

            if (existingPath[0] == edge[0] || existingPath[0] == edge[1])
            {
                return mergePoint + existingPath;
            }
            else if (existingPath[^1] == edge[0] || existingPath[^1] == edge[1])
            {
                return existingPath + mergePoint;
            }
            else
            {
                // Cannot merge edge with existing path
                return null;
            }
        }

        private bool MatchString(string str1, string str2)
        {
            return str1.Contains(str2[0]) || str1.Contains(str2[1]);
        }

        private static string ReverseString(string s)
        {
            char[] arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
    }
}
