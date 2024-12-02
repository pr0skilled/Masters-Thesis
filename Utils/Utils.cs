using System.IO;
using System.Text;

namespace Thesis.Models
{
    public static class Utils
    {
        public static Random Random = new();

        public static void Swap(List<int> list, int i, int j)
        {
            (list[j], list[i]) = (list[i], list[j]);
        }

        public static int Factorial(int n)
        {
            int ans = 1;

            for (int i = 2; i <= n; i++)
            {
                ans *= i;
            }

            return ans;
        }

        public static string PrintList(List<char> cList)
        {
            var sb = new StringBuilder();

            foreach (char c in cList)
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static List<char> IntersectList(List<char> A, List<char> B)
        {
            //Return a list of all chars contained in both lists
            List<char> bothContain = [];

            foreach (char c in A)
            {
                if (B.Contains(c))
                {
                    bothContain.Add(c);
                }
            }

            return bothContain;
        }

        public static string? GetSolutionDirectoryPath()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            var solutionDirectory = directory?.FullName;

            return solutionDirectory;
        }

        public static List<int> StringToIntArray(string path)
        {
            var indices = new List<int>();
            var tokens = path.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out int index))
                {
                    indices.Add(index - 1);
                }
            }
            return indices;
        }

    }
}
