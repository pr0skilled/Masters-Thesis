using Thesis.Utils;

namespace Thesis.Models
{
    public class AlgorithmData
    {
        public AlgorithmType Type { get; private set; }

        // Stores datasets keyed by dataset ID
        private Dictionary<int, List<double>> costDatasets = new();
        private Dictionary<int, List<double>> runtimeDatasets = new();
        private Dictionary<int, List<double>> iterationDatasets = new();
        private Dictionary<int, List<int>> cityCountDatasets = new();

        // Constructor
        public AlgorithmData(AlgorithmType type)
        {
            this.Type = type;
        }

        // Methods to add data to a specific dataset
        public void AddCostData(int datasetId, double cost)
        {
            if (!this.costDatasets.ContainsKey(datasetId))
                this.costDatasets[datasetId] = new List<double>();
            this.costDatasets[datasetId].Add(cost);
        }

        public void AddRuntimeData(int datasetId, double runtime)
        {
            if (!this.runtimeDatasets.ContainsKey(datasetId))
                this.runtimeDatasets[datasetId] = new List<double>();
            this.runtimeDatasets[datasetId].Add(runtime);
        }

        public void AddIterationData(int datasetId, double iteration)
        {
            if (!this.iterationDatasets.ContainsKey(datasetId))
                this.iterationDatasets[datasetId] = new List<double>();
            this.iterationDatasets[datasetId].Add(iteration);
        }

        public void AddCityCountData(int datasetId, int cityCount)
        {
            if (!this.cityCountDatasets.ContainsKey(datasetId))
                this.cityCountDatasets[datasetId] = new List<int>();
            this.cityCountDatasets[datasetId].Add(cityCount);
        }

        // Methods to retrieve the latest dataset
        public List<double> GetCosts(int datasetId)
        {
            return this.costDatasets.TryGetValue(datasetId, out var costs) ? costs : new List<double>();
        }

        public List<double> GetRuntimes(int datasetId)
        {
            return this.runtimeDatasets.TryGetValue(datasetId, out var runtimes) ? runtimes : new List<double>();
        }

        public List<double> GetIterations(int datasetId)
        {
            return this.iterationDatasets.TryGetValue(datasetId, out var iterations) ? iterations : new List<double>();
        }

        public List<int> GetCityCounts(int datasetId)
        {
            return this.cityCountDatasets.TryGetValue(datasetId, out var count) ? count : new List<int>();
        }

        public IEnumerable<int> GetCityCountsKeys()
        {
            return this.cityCountDatasets.Keys;
        }

        public IEnumerable<int> GetRuntimesKeys()
        {
            return this.runtimeDatasets.Keys;
        }

        // Clear all data
        public void ClearAllData()
        {
            this.costDatasets.Clear();
            this.runtimeDatasets.Clear();
            this.iterationDatasets.Clear();
        }
    }
}
