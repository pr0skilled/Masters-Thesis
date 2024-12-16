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
            if (!this.algorithms.ContainsKey(type))
            {
                this.algorithms[type] = new AlgorithmData(type);
            }
            return this.algorithms[type];
        }

        // Method to retrieve all AlgorithmData instances
        public IEnumerable<AlgorithmData> GetAllAlgorithmData()
        {
            return this.algorithms.Values;
        }

        // Start a new dataset for all algorithms
        public void StartNewDataset()
        {
            this.currentDatasetId++;
        }

        // Add data to the latest dataset
        public void AddCostData(AlgorithmType type, double cost)
        {
            var algorithmData = this.GetAlgorithmData(type);
            algorithmData.AddCostData(this.currentDatasetId, cost);
        }

        public void AddRuntimeData(AlgorithmType type, int cityCount, double runtime)
        {
            var algorithmData = this.GetAlgorithmData(type);
            algorithmData.AddRuntimeData(this.currentDatasetId, runtime);
            algorithmData.AddCityCountData(this.currentDatasetId, cityCount);
        }

        public void AddIterationData(AlgorithmType type, double iteration)
        {
            var algorithmData = this.GetAlgorithmData(type);
            algorithmData.AddIterationData(this.currentDatasetId, iteration);
        }

        // Retrieve the latest dataset for all algorithms
        public Dictionary<AlgorithmType, List<double>> GetLatestCosts()
        {
            return this.algorithms.ToDictionary(a => a.Key, a => a.Value.GetCosts(this.currentDatasetId));
        }

        public Dictionary<AlgorithmType, List<double>> GetLatestRuntimes()
        {
            return this.algorithms.ToDictionary(a => a.Key, a => a.Value.GetRuntimes(this.currentDatasetId));
        }

        public Dictionary<AlgorithmType, List<double>> GetLatestIterations()
        {
            return this.algorithms.ToDictionary(a => a.Key, a => a.Value.GetIterations(this.currentDatasetId));
        }

        // Clear all data
        public void ClearAllData()
        {
            foreach (var algorithm in this.algorithms.Values)
            {
                algorithm.ClearAllData();
            }
            this.currentDatasetId = 0;
        }
    }
}
