using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPGeneticAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int pathsChecked;
        private int totalPaths;

        // Public properties to expose PathsChecked and TotalPaths
        public int PathsChecked => this.pathsChecked;
        public int TotalPaths => this.totalPaths;

        public TSPGeneticAlgorithm(List<Point> pointsGiven) : base(pointsGiven)
        {
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            bool hasConverged = false;
            int threshold = 10 * this.PointsGiven.Count; // Number of generations without improvement for convergence
            int dniCount = 0;                            // Tracks generations with no score improvement
            int populationCount = 4 * this.PointsGiven.Count;
            int eliteCount = populationCount / 4;
            int generation = 0;
            int maxGenerations = 1000; // Prevent infinite loops
            double totalScores;
            double minScore;

            var population = new List<string>();
            var children = new string[populationCount];
            var nextGenScores = new double[populationCount];
            double mutationRate = 0.1;
            string bestPath = string.Empty;
            double currentBestScore = double.MaxValue;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            this.CalculateDistanceMatrix();

            // Initialize total paths
            this.totalPaths = populationCount * threshold;

            // Fix the starting city
            char startingCity = 'A';
            int cityCount = this.PointsGiven.Count;

            // Create initial sample population
            for (int i = 0; i < populationCount; i++)
            {
                string individual = GenerateRandomPath(cityCount, startingCity);
                population.Add(individual);
            }

            var rnd = Utils.Random;

            while (!hasConverged && generation < maxGenerations)
            {
                totalScores = 0;

                // Step 1: Crossover and mutate all members of the population
                for (int i = 0; i < populationCount; i += 2)
                {
                    // Select parents
                    string parent1 = population[i];
                    string parent2 = population[(i + 1) % populationCount];

                    // Perform Ordered Crossover excluding the starting city
                    string child1 = this.OrderedCrossover(parent1, parent2, startingCity);
                    string child2 = this.OrderedCrossover(parent2, parent1, startingCity);

                    // Apply Swap Mutation
                    child1 = this.SwapMutation(child1, mutationRate, startingCity);
                    child2 = this.SwapMutation(child2, mutationRate, startingCity);

                    // Assign children to the new population
                    children[i] = child1;
                    children[i + 1] = child2;

                    // Calculate scores
                    nextGenScores[i] = this.FindPathDistance(child1);
                    nextGenScores[i + 1] = this.FindPathDistance(child2);

                    totalScores += nextGenScores[i] + nextGenScores[i + 1];

                    // Increment paths checked
                    this.pathsChecked += 2;
                }

                // Sort the children based on scores (ascending)
                Array.Sort(nextGenScores, children);

                // Update best score
                if (nextGenScores[0] < currentBestScore)
                {
                    dniCount = 0;
                    currentBestScore = nextGenScores[0];
                    bestPath = children[0];
                }
                else
                {
                    dniCount++;
                }

                // Check convergence based on threshold
                if (dniCount > threshold)
                {
                    hasConverged = true;
                }

                // Step 3: Elite Selection
                population.Clear();

                // Preserve elites
                for (int i = 0; i < eliteCount; i++)
                {
                    population.Add(children[i]);
                }

                // Build roulette wheel for selection
                double totalScoreInverse = nextGenScores.Sum(score => 1 / score);
                double[] cumulativeProbabilities = new double[populationCount];
                cumulativeProbabilities[0] = (1 / nextGenScores[0]) / totalScoreInverse;

                for (int i = 1; i < populationCount; i++)
                {
                    cumulativeProbabilities[i] = cumulativeProbabilities[i - 1] + (1 / nextGenScores[i]) / totalScoreInverse;
                }

                // Fill the rest of the population using roulette wheel selection
                while (population.Count < populationCount)
                {
                    double randValue = rnd.NextDouble();
                    for (int i = 0; i < populationCount; i++)
                    {
                        if (randValue <= cumulativeProbabilities[i])
                        {
                            population.Add(children[i]);
                            break;
                        }
                    }
                }

                // Adjust mutation rate
                mutationRate = dniCount < threshold / 2 ? 0.1 : 0.2;

                generation++;
            }

            stopWatch.Stop();

            this.PaintPath = Utils.StringToIntArray(bestPath);

            return (bestPath, currentBestScore, stopWatch.Elapsed);
        }

        private static string GenerateRandomPath(int size, char startingCity)
        {
            var letters = new List<char>(size - 1);
            var sb = new StringBuilder();
            int temp;
            char c;

            // Exclude starting city from the letters
            for (int i = 0; i < size; i++)
            {
                c = (char)(65 + i);
                if (c != startingCity)
                    letters.Add(c);
            }

            sb.Append(startingCity);

            // Randomly arrange the remaining cities
            while (letters.Count > 0)
            {
                temp = Utils.Random.Next(letters.Count);
                c = letters[temp];
                letters.RemoveAt(temp);
                sb.Append(c);
            }

            sb.Append(startingCity); // Add starting city at the end

            return sb.ToString();
        }

        private string OrderedCrossover(string parent1, string parent2, char startingCity)
        {
            int size = parent1.Length - 2; // Exclude starting city at both ends
            int start = Utils.Random.Next(1, size + 1);
            int end = Utils.Random.Next(1, size + 1);

            while (end == start)
            {
                end = Utils.Random.Next(1, size + 1);
            }

            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            // Extract substring from parent1
            string substring = parent1.Substring(start, end - start + 1);

            // Exclude starting city from parent2
            string p2 = parent2.Substring(1, parent2.Length - 2);

            // Remove the cities in substring from parent2
            var remainingCities = p2.Where(c => !substring.Contains(c)).ToList();

            // Build child sequence
            var childBuilder = new StringBuilder();
            childBuilder.Append(startingCity);

            // Initialize child with nulls
            char[] child = new char[size + 2];
            child[0] = startingCity;
            child[size + 1] = startingCity;

            // Place substring into the child
            for (int i = start; i <= end; i++)
            {
                child[i] = substring[i - start];
            }

            // Fill the remaining positions with cities from parent2
            int p2Index = 0;
            for (int i = 1; i <= size; i++)
            {
                if (child[i] == '\0')
                {
                    while (substring.Contains(p2[p2Index]))
                    {
                        p2Index++;
                    }
                    child[i] = p2[p2Index];
                    p2Index++;
                }
            }

            string childString = new string(child);

            // Ensure the starting city is at both ends
            if (childString[0] != startingCity)
            {
                childString = startingCity + childString.Substring(1);
            }
            if (childString[^1] != startingCity)
            {
                childString += startingCity;
            }

            return childString;
        }

        private string SwapMutation(string path, double mutationRate, char startingCity)
        {
            var rnd = Utils.Random;
            double chance = rnd.NextDouble();

            if (chance < mutationRate)
            {
                int size = path.Length - 2; // Exclude starting city at both ends
                int index1 = rnd.Next(1, size + 1);
                int index2 = rnd.Next(1, size + 1);

                while (index1 == index2)
                {
                    index2 = rnd.Next(1, size + 1);
                }

                char[] chars = path.ToCharArray();

                // Swap the cities at index1 and index2
                char temp = chars[index1];
                chars[index1] = chars[index2];
                chars[index2] = temp;

                // Ensure starting city is at both ends
                chars[0] = startingCity;
                chars[^1] = startingCity;

                return new string(chars);
            }

            return path;
        }
    }
}
