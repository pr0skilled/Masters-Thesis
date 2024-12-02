using System.Diagnostics;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPGeneticAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int pathsChecked;
        private int totalPaths;

        public int PathsChecked => this.pathsChecked;
        public int TotalPaths => this.totalPaths;

        public int TotalGenerations { get; private set; }
        public int PopulationSize { get; private set; }
        public double MutationRate { get; private set; }
        public double InitialBestScore { get; private set; }

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

            var population = new List<List<int>>();
            var children = new List<List<int>>(populationCount);
            var nextGenScores = new double[populationCount];
            double mutationRate = 0.1;
            List<int> bestPath = null;
            double currentBestScore = double.MaxValue;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            this.CalculateDistanceMatrix();

            // Initialize total paths
            this.totalPaths = populationCount * threshold;

            this.TotalGenerations = 0;
            this.PopulationSize = populationCount;
            this.MutationRate = mutationRate;

            // Fix the starting city index
            int startingCityIndex = 0;

            int cityCount = this.PointsGiven.Count;

            // Create initial sample population
            for (int i = 0; i < populationCount; i++)
            {
                var individual = GenerateRandomPath(cityCount, startingCityIndex);
                population.Add(individual);
            }

            foreach (var individual in population)
            {
                double score = this.CalculateRouteCost(individual);
                if (score < currentBestScore)
                {
                    currentBestScore = score;
                    bestPath = new List<int>(individual);
                }
                this.pathsChecked++;
            }
            this.InitialBestScore = currentBestScore; // Set InitialBestScore

            while (!hasConverged && generation < maxGenerations)
            {
                totalScores = 0;

                // Step 1: Crossover and mutate all members of the population
                children.Clear();
                for (int i = 0; i < populationCount; i += 2)
                {
                    // Select parents
                    var parent1 = population[i];
                    var parent2 = population[(i + 1) % populationCount];

                    // Perform Ordered Crossover excluding the starting city
                    var child1 = this.OrderedCrossover(parent1, parent2, startingCityIndex);
                    var child2 = this.OrderedCrossover(parent2, parent1, startingCityIndex);

                    // Apply Swap Mutation
                    child1 = this.SwapMutation(child1, mutationRate, startingCityIndex);
                    child2 = this.SwapMutation(child2, mutationRate, startingCityIndex);

                    // Assign children to the new population
                    children.Add(child1);
                    children.Add(child2);

                    // Calculate scores
                    nextGenScores[i] = this.CalculateRouteCost(child1);
                    nextGenScores[i + 1] = this.CalculateRouteCost(child2);

                    totalScores += nextGenScores[i] + nextGenScores[i + 1];

                    // Increment paths checked
                    this.pathsChecked += 2;
                }

                // Sort the children based on scores (ascending)
                var sortedChildren = children.Zip(nextGenScores, (child, score) => new { Child = child, Score = score })
                                             .OrderBy(cs => cs.Score)
                                             .ToList();

                // Update best score
                if (sortedChildren[0].Score < currentBestScore)
                {
                    dniCount = 0;
                    currentBestScore = sortedChildren[0].Score;
                    bestPath = new List<int>(sortedChildren[0].Child);
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
                    population.Add(sortedChildren[i].Child);
                }

                // Build roulette wheel for selection
                double totalScoreInverse = sortedChildren.Sum(cs => 1 / cs.Score);
                double[] cumulativeProbabilities = new double[populationCount];
                cumulativeProbabilities[0] = (1 / sortedChildren[0].Score) / totalScoreInverse;

                for (int i = 1; i < populationCount; i++)
                {
                    cumulativeProbabilities[i] = cumulativeProbabilities[i - 1] + (1 / sortedChildren[i].Score) / totalScoreInverse;
                }

                // Fill the rest of the population using roulette wheel selection
                while (population.Count < populationCount)
                {
                    double randValue = Utils.Random.NextDouble();
                    for (int i = 0; i < populationCount; i++)
                    {
                        if (randValue <= cumulativeProbabilities[i])
                        {
                            population.Add(sortedChildren[i].Child);
                            break;
                        }
                    }
                }

                // Adjust mutation rate
                mutationRate = dniCount < threshold / 2 ? 0.1 : 0.2;
                this.MutationRate = mutationRate;

                generation++;
                this.TotalGenerations = generation;
            }

            stopWatch.Stop();

            this.PaintPath = bestPath;

            string bestPathString = this.BuildPathString(bestPath);

            return (bestPathString, currentBestScore, stopWatch.Elapsed);
        }

        private static List<int> GenerateRandomPath(int size, int startingCityIndex)
        {
            var cities = Enumerable.Range(0, size).ToList();
            cities.Remove(startingCityIndex);
            var shuffledCities = cities.OrderBy(x => Utils.Random.Next()).ToList();
            shuffledCities.Insert(0, startingCityIndex);
            shuffledCities.Add(startingCityIndex); // Complete the cycle
            return shuffledCities;
        }

        private List<int> OrderedCrossover(List<int> parent1, List<int> parent2, int startingCityIndex)
        {
            int size = parent1.Count - 2; // Exclude starting city at both ends
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

            // Extract segment from parent1
            var segment = parent1.GetRange(start, end - start + 1);

            // Exclude starting city from parent2
            var p2 = parent2.GetRange(1, parent2.Count - 2);

            // Remove the cities in the segment from parent2
            var remainingCities = p2.Where(city => !segment.Contains(city)).ToList();

            // Build child sequence
            var child = new List<int>(new int[parent1.Count]);
            child[0] = startingCityIndex;
            child[^1] = startingCityIndex;

            // Place segment into the child
            for (int i = start; i <= end; i++)
            {
                child[i] = segment[i - start];
            }

            // Fill the remaining positions with cities from parent2
            int p2Index = 0;
            for (int i = 1; i < child.Count - 1; i++)
            {
                if (child[i] == 0) // Assuming city indices are positive integers
                {
                    child[i] = remainingCities[p2Index];
                    p2Index++;
                }
            }

            return child;
        }

        private List<int> SwapMutation(List<int> path, double mutationRate, int startingCityIndex)
        {
            var rnd = Utils.Random;
            double chance = rnd.NextDouble();

            if (chance < mutationRate)
            {
                int size = path.Count - 2; // Exclude starting city at both ends
                int index1 = rnd.Next(1, size + 1);
                int index2 = rnd.Next(1, size + 1);

                while (index1 == index2)
                {
                    index2 = rnd.Next(1, size + 1);
                }

                // Swap the cities at index1 and index2
                (path[index2], path[index1]) = (path[index1], path[index2]);
            }

            return path;
        }
    }
}
