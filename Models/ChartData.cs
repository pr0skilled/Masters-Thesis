using Thesis.Utils;

namespace Thesis.Models
{
    public class ChartData
    {
        private Dictionary<AlgorithmType, AlgorithmData> algorithms = new();

        private int currentDatasetId = 0; // Single counter for all algorithms

        // Method to get or create an AlgorithmData instance
        public AlgorithmData GetAlgorithmData(AlgorithmType type)
        {
            if (!algorithms.ContainsKey(type))
            {
                algorithms[type] = new AlgorithmData(type);
            }
            return algorithms[type];
        }

        // Method to retrieve all AlgorithmData instances
        public IEnumerable<AlgorithmData> GetAllAlgorithmData()
        {
            return algorithms.Values;
        }

        // Start a new dataset for all algorithms
        public void StartNewDataset()
        {
            currentDatasetId++;
        }

        // Add data to the latest dataset
        public void AddCostData(AlgorithmType type, double cost)
        {
            var algorithmData = GetAlgorithmData(type);
            algorithmData.AddCostData(currentDatasetId, cost);
        }

        public void AddRuntimeData(AlgorithmType type, int cityCount, double runtime)
        {
            var algorithmData = GetAlgorithmData(type);
            algorithmData.AddRuntimeData(currentDatasetId, runtime);
            algorithmData.AddCityCountData(currentDatasetId, cityCount);
        }

        public void AddIterationData(AlgorithmType type, double iteration)
        {
            var algorithmData = GetAlgorithmData(type);
            algorithmData.AddIterationData(currentDatasetId, iteration);
        }

        // Retrieve the latest dataset for all algorithms
        public Dictionary<AlgorithmType, List<double>> GetLatestCosts()
        {
            return algorithms.ToDictionary(a => a.Key, a => a.Value.GetCosts(currentDatasetId));
        }

        public Dictionary<AlgorithmType, List<double>> GetLatestRuntimes()
        {
            return algorithms.ToDictionary(a => a.Key, a => a.Value.GetRuntimes(currentDatasetId));
        }

        public Dictionary<AlgorithmType, List<double>> GetLatestIterations()
        {
            return algorithms.ToDictionary(a => a.Key, a => a.Value.GetIterations(currentDatasetId));
        }

        // Clear all data
        public void ClearAllData()
        {
            foreach (var algorithm in algorithms.Values)
            {
                algorithm.ClearAllData();
            }
            currentDatasetId = 0;
        }
    }
}
