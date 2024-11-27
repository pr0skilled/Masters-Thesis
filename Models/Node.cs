namespace Thesis.Models
{
    public class Node
    {
        public char Name { get; set; }

        public char Parent { get; set; }

        public string? Children { get; set; }

        public double Value { get; set; }

        public Node()
        {
        }

        public Node(char name, char parent, double value = 0)
        {
            this.Name = name;
            this.Parent = parent;
            this.Value = value;
        }
    }
}
