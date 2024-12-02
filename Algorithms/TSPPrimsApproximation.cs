using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPPrimsApproximation : TSPAlgorithmBase, ITSPAlgorithm
    {
        private List<Node> mst; // Minimum Spanning Tree
        private List<Node> q;   // Priority Queue for Prim's algorithm

        public double MSTCost { get; private set; }
        public int NumberOfNodes { get; private set; }

        public TSPPrimsApproximation(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.mst = [];
            this.q = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // 1) Calculate Distance Matrix
            this.CalculateDistanceMatrix();

            // Select a random vertex to be the root
            int rootIndex = Utils.Random.Next(this.PointsGiven.Count);
            int root = rootIndex;
            this.mst.Add(new Node { Name = root, Parent = -1, Children = new List<int>(), Value = 0 });
            // Console.WriteLine("Selecting '" + (root + 1) + "' as root node of MST.");

            // Populate Q with non-root nodes
            for (int i = 0; i < this.PointsGiven.Count; i++)
            {
                if (i != rootIndex)
                {
                    double distance = this.distanceMatrix[rootIndex, i];
                    this.q.Add(new Node { Name = i, Parent = root, Children = new List<int>(), Value = distance });
                }
            }

            // Construct MST using Prim's algorithm
            while (this.q.Count > 0)
            {
                // Sort Q by Value (distance) in ascending order
                this.q = this.q.OrderBy(node => node.Value).ToList();

                // Take closest point (u), add to MST
                Node u = this.q.First();
                this.mst.Add(u);

                this.MSTCost += u.Value;

                // Remove from Q
                this.q.RemoveAt(0);

                // Add its Name to its parent's Children
                Node p = this.mst.Find(x => x.Name == u.Parent);
                if (p != null)
                {
                    p.Children.Add(u.Name);
                }

                // Update distances for remaining points in Q
                foreach (Node v in this.q)
                {
                    int uIndex = u.Name;
                    int vIndex = v.Name;
                    double distance = this.distanceMatrix[uIndex, vIndex];
                    if (distance < v.Value)
                    {
                        // Update v's Parent and Value if closer to u
                        v.Value = distance;
                        v.Parent = u.Name;
                    }
                }
            }

            this.NumberOfNodes = this.mst.Count;

            // Perform Preorder traversal of MST to get an approximate TSP path
            List<int> traversal = this.PreOrder(this.mst.First(), this.mst);

            // Add the starting city at the end to complete the cycle
            traversal.Add(traversal[0]);

            // Calculate total distance of the tour
            double totalCost = this.CalculateRouteCost(traversal);

            stopWatch.Stop();

            // Update PaintPath
            this.PaintPath = traversal;

            // Build the path string using numbers
            string bestPathString = this.BuildPathString(traversal);

            return (bestPathString, totalCost, stopWatch.Elapsed);
        }

        private void PrintNode(Node node, List<Node> T)
        {
            this.PrintNode(node, 0, T);
        }

        private void PrintNode(Node node, int indentation, List<Node> T)
        {
            // Print node with indentation (optional for debugging)
            for (int i = 0; i < indentation; i++)
            {
                Console.Write("\t");
            }
            Console.WriteLine("-" + node.Name);

            // Recursively call child nodes
            foreach (char c in node.Children)
            {
                Node childNode = T.Find(x => x.Name == c);
                if (childNode != null)
                {
                    this.PrintNode(childNode, indentation + 1, T);
                }
            }
        }

        public List<int> PreOrder(Node node, List<Node> nodes)
        {
            // Recursive function for preorder traversal
            var route = new List<int> { node.Name };

            foreach (int childName in node.Children)
            {
                Node childNode = nodes.Find(x => x.Name == childName);
                if (childNode != null)
                {
                    route.AddRange(this.PreOrder(childNode, nodes));
                }
            }

            return route;
        }
    }
}
