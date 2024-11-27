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

        public TSPPrimsApproximation(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.mst = [];
            this.q = [];
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            char root;
            string preTraversal;
            var stopWatch = new Stopwatch();
            int rootIndex;
            double distance;
            Node u, p;

            stopWatch.Start();

            // 1) Calculate Distance Matrix
            this.CalculateDistanceMatrix();

            // Select a random vertex to be the root
            rootIndex = Utils.Random.Next(this.PointsGiven.Count);
            root = (char)('A' + rootIndex);
            this.mst.Add(new Node { Name = root, Parent = '0', Children = "", Value = 0 });
            // Console.WriteLine("Selecting '" + root + "' as root node of MST.");

            // Populate Q with non-root nodes
            for (int i = 0; i < this.PointsGiven.Count; i++)
            {
                if (i != rootIndex)
                {
                    char c = (char)('A' + i);
                    distance = this.distanceMatrix[rootIndex, i];
                    this.q.Add(new Node { Name = c, Parent = root, Children = "", Value = distance });
                }
            }

            // Construct MST using Prim's algorithm
            while (this.q.Count > 0)
            {
                // Sort Q by Value (distance) in ascending order
                this.q = this.q.OrderBy(node => node.Value).ToList();

                // Take closest point (u), add to MST
                u = this.q.First();
                this.mst.Add(u);

                // Remove from Q
                this.q.RemoveAt(0);

                // Add its Name to its parent's Children
                p = this.mst.Find(x => x.Name == u.Parent);
                if (p != null)
                {
                    p.Children += u.Name.ToString();
                }

                // Update distances for remaining points in Q
                foreach (Node v in this.q)
                {
                    int uIndex = u.Name - 'A';
                    int vIndex = v.Name - 'A';
                    distance = this.distanceMatrix[uIndex, vIndex];
                    if (distance < v.Value)
                    {
                        // Update v's Parent and Value if closer to u
                        v.Value = distance;
                        v.Parent = u.Name;
                    }
                }
            }

            // Perform Preorder traversal of MST to get an approximate TSP path
            preTraversal = this.PreOrder(this.mst.First(), this.mst);

            // Add the starting city at the end to complete the cycle
            if (preTraversal[preTraversal.Length - 1] != preTraversal[0])
            {
                preTraversal += preTraversal[0];
            }

            // Calculate total distance of the tour
            this.bestScore = this.FindPathDistance(preTraversal);

            stopWatch.Stop();

            // Convert path string to indices for drawing
            this.PaintPath = Utils.StringToIntArray(preTraversal);

            return (preTraversal, this.bestScore, stopWatch.Elapsed);
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

        public string PreOrder(Node r, List<Node> nodes)
        {
            // Recursive function for preorder traversal
            var sb = new StringBuilder();

            sb.Append(r.Name);

            foreach (char c in r.Children)
            {
                Node child = nodes.Find(x => x.Name == c);
                if (child != null)
                {
                    sb.Append(this.PreOrder(child, nodes));
                }
            }

            return sb.ToString();
        }
    }

    public class Node
    {
        public char Name { get; set; }
        public char Parent { get; set; }
        public string Children { get; set; }
        public double Value { get; set; } // Distance value for sorting
    }
}
