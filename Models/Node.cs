namespace Thesis.Models
{
    public class Node
    {
        public int Name { get; set; } // City index
        public int Parent { get; set; } // Parent city index (-1 for root)
        public List<int> Children { get; set; } // List of child city indices
        public double Value { get; set; } // Distance value for sorting

        public Node()
        {
        }

        public Node(int name, int parent, double value = 0)
        {
            this.Name = name;
            this.Parent = parent;
            this.Value = value;
        }
    }
}
