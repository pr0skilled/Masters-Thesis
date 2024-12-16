using OxyPlot;
using OxyPlot.Series;

namespace Thesis.Utils
{
    public class CustomBarItem : BarItem
    {
        public object Tag { get; set; }
    }

    public class CustomBarSeries : BarSeries
    {
        private readonly Dictionary<int, (double Cost, double? StdDev)> costMapping = new Dictionary<int, (double, double?)>();

        /// <summary>
        /// Adds a bar item with a custom cost value and optional standard deviation.
        /// </summary>
        public void AddBarItemWithCost(BarItem item, double cost, double? stdDev = null)
        {
            this.Items.Add(item);
            this.costMapping[this.Items.Count - 1] = (cost, stdDev);
        }

        /// <summary>
        /// Overrides the tracker text to include custom cost and optional standard deviation information.
        /// </summary>
        protected override string GetTrackerText(BarItem barItem, object item, int categoryIndex)
        {
            var categoryAxis = this.GetCategoryAxis();
            var valueAxis = this.XAxis;

            if (!this.costMapping.TryGetValue(categoryIndex, out var data))
                return base.GetTrackerText(barItem, item, categoryIndex);

            var cost = data.Cost.ToString("0.##");
            var stdDev = data.StdDev.HasValue ? $" ±{data.StdDev.Value:0.##}" : string.Empty;

            return StringHelper.Format(
                this.ActualCulture,
                this.TrackerFormatString,
                item,
                this.Title,                          // {0} - Series title
                categoryAxis.FormatValue(categoryIndex), // {1} - Category label
                valueAxis.GetValue(barItem.Value),  // {2} - Bar value
                cost,                               // {3} - Cost
                stdDev                              // {4} - StdDev (only for avgSeries)
            );
        }
    }
}
