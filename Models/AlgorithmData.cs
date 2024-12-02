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
            if (!costDatasets.ContainsKey(datasetId))
                costDatasets[datasetId] = new List<double>();
            costDatasets[datasetId].Add(cost);
        }

        public void AddRuntimeData(int datasetId, double runtime)
        {
            if (!runtimeDatasets.ContainsKey(datasetId))
                runtimeDatasets[datasetId] = new List<double>();
            runtimeDatasets[datasetId].Add(runtime);
        }

        public void AddIterationData(int datasetId, double iteration)
        {
            if (!iterationDatasets.ContainsKey(datasetId))
                iterationDatasets[datasetId] = new List<double>();
            iterationDatasets[datasetId].Add(iteration);
        }

        public void AddCityCountData(int datasetId, int cityCount)
        {
            if (!cityCountDatasets.ContainsKey(datasetId))
                cityCountDatasets[datasetId] = new List<int>();
            cityCountDatasets[datasetId].Add(cityCount);
        }

        // Methods to retrieve the latest dataset
        public List<double> GetCosts(int datasetId)
        {
            return costDatasets.TryGetValue(datasetId, out var costs) ? costs : new List<double>();
        }

        public List<double> GetRuntimes(int datasetId)
        {
            return runtimeDatasets.TryGetValue(datasetId, out var runtimes) ? runtimes : new List<double>();
        }

        public List<double> GetIterations(int datasetId)
        {
            return iterationDatasets.TryGetValue(datasetId, out var iterations) ? iterations : new List<double>();
        }

        public List<int> GetCityCounts(int datasetId)
        {
            return cityCountDatasets.TryGetValue(datasetId, out var count) ? count : new List<int>();
        }

        public IEnumerable<int> GetCityCountsKeys()
        {
            return cityCountDatasets.Keys;
        }

        public IEnumerable<int> GetRuntimesKeys()
        {
            return runtimeDatasets.Keys;
        }

        // Clear all data
        public void ClearAllData()
        {
            costDatasets.Clear();
            runtimeDatasets.Clear();
            iterationDatasets.Clear();
        }
    }
}
