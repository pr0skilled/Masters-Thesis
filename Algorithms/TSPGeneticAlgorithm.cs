using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Thesis.Algorithms
{
    using Models;

    public class TSPGeneticAlgorithm : TSPAlgorithmBase, ITSPAlgorithm
    {
        private int populationSize;
        private int generations;
        private double temperature;
        private const int CoolingRate = 90; // Cooling rate for simulated annealing
        private const double ElitismRate = 0.1; // Percentage of elite solutions to retain

        public TSPGeneticAlgorithm(List<Point> pointsGiven) : base(pointsGiven)
        {
            this.populationSize = Math.Max(70, pointsGiven.Count * 8); // Increased population size
            this.generations = Math.Max(100, pointsGiven.Count);       // More generations
            this.temperature = 10000; // Initial temperature
        }

        public override (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            this.CalculateDistanceMatrix();

            var population = new List<(List<int> Gnome, double Fitness)>();

            // Generate the initial population with some greedy heuristics
            for (int i = 0; i < populationSize; i++)
            {
                var gnome = i < populationSize / 2 ? GenerateGreedyPath() : GenerateRandomPath(this.PointsGiven.Count, 0);
                var fitness = this.CalculateRouteCost(gnome);
                population.Add((gnome, fitness));
            }

            int generation = 1;
            while (temperature > 1000 && generation <= generations)
            {
                // Sort population by fitness (ascending order)
                population = population.OrderBy(x => x.Fitness).ToList();

                var newPopulation = new List<(List<int> Gnome, double Fitness)>();

                // Preserve elite solutions
                int eliteCount = (int)(populationSize * ElitismRate);
                newPopulation.AddRange(population.Take(eliteCount));

                // Generate the rest of the new population
                while (newPopulation.Count < populationSize)
                {
                    var parent = RouletteWheelSelection(population);
                    var mutatedGnome = MutateGnome(parent.Gnome);
                    //var optimizedGnome = LocalOptimization(mutatedGnome);
                    var fitness = this.CalculateRouteCost(mutatedGnome);
                    newPopulation.Add((mutatedGnome, fitness));
                }

                // Cool down the temperature
                temperature = CoolDown(temperature, generation, generations);

                // Replace old population with new population
                population = newPopulation;

                generation++;
            }

            // Get the best solution from the final population
            var bestSolution = population.OrderBy(x => x.Fitness).First();

            stopWatch.Stop();

            this.PaintPath = bestSolution.Gnome;

            // Ensure the path returns to the initial city
            if (bestSolution.Gnome[bestSolution.Gnome.Count - 1] != bestSolution.Gnome[0])
            {
                bestSolution.Gnome.Add(bestSolution.Gnome[0]);
            }

            return (BuildPathString(bestSolution.Gnome), bestSolution.Fitness, stopWatch.Elapsed);
        }

        private List<int> MutateGnome(List<int> gnome)
        {
            var rnd = Utils.Random.NextDouble();
            var mutatedGnome = new List<int>(gnome);
            int size = gnome.Count - 2; // Exclude start and end cities

            if (rnd < 0.5)
            {
                // Swap mutation
                int index1 = Utils.Random.Next(1, size + 1);
                int index2 = Utils.Random.Next(1, size + 1);

                while (index1 == index2)
                {
                    index2 = Utils.Random.Next(1, size + 1);
                }

                (mutatedGnome[index1], mutatedGnome[index2]) = (mutatedGnome[index2], mutatedGnome[index1]);
            }
            else
            {
                // Reverse mutation
                int index1 = Utils.Random.Next(1, size + 1);
                int index2 = Utils.Random.Next(1, size + 1);

                if (index1 > index2)
                {
                    (index1, index2) = (index2, index1);
                }

                mutatedGnome = mutatedGnome.Take(index1)
                                           .Concat(mutatedGnome.Skip(index1).Take(index2 - index1 + 1).Reverse())
                                           .Concat(mutatedGnome.Skip(index2 + 1))
                                           .ToList();
            }

            return mutatedGnome;
        }

        private List<int> LocalOptimization(List<int> gnome)
        {
            for (int i = 1; i < gnome.Count - 2; i++)
            {
                for (int j = i + 1; j < gnome.Count - 1; j++)
                {
                    var optimizedGnome = new List<int>(gnome);
                    optimizedGnome.Reverse(i, j - i + 1);

                    if (this.CalculateRouteCost(optimizedGnome) < this.CalculateRouteCost(gnome))
                    {
                        gnome = optimizedGnome;
                    }
                }
            }
            return gnome;
        }

        private List<int> GenerateGreedyPath()
        {
            var path = new List<int> { 0 };
            var remainingCities = new HashSet<int>(Enumerable.Range(1, this.PointsGiven.Count - 1));

            while (remainingCities.Count > 0)
            {
                int lastCity = path[^1];
                int nextCity = remainingCities.OrderBy(city => this.distanceMatrix[lastCity, city]).First();
                path.Add(nextCity);
                remainingCities.Remove(nextCity);
            }

            path.Add(0); // Return to the starting city
            return path;
        }

        private (List<int> Gnome, double Fitness) RouletteWheelSelection(List<(List<int> Gnome, double Fitness)> population)
        {
            double totalFitness = population.Sum(individual => 1 / individual.Fitness);
            double rand = Utils.Random.NextDouble() * totalFitness;

            double cumulativeFitness = 0;
            foreach (var individual in population)
            {
                cumulativeFitness += 1 / individual.Fitness;
                if (cumulativeFitness >= rand)
                {
                    return individual;
                }
            }

            return population.Last();
        }

        private double CoolDown(double currentTemperature, int generation, int maxGenerations)
        {
            return currentTemperature * (1 - (0.1 * generation / maxGenerations));
        }

        private static List<int> GenerateRandomPath(int size, int startingCityIndex)
        {
            var cities = Enumerable.Range(0, size).ToList();
            cities.Remove(startingCityIndex);
            var shuffledCities = cities.OrderBy(_ => Utils.Random.Next()).ToList();
            shuffledCities.Insert(0, startingCityIndex);
            shuffledCities.Add(startingCityIndex); // Complete the cycle
            return shuffledCities;
        }
    }
}