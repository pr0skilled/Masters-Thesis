using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using OxyPlot;
using OxyPlot.Series;

namespace Thesis
{
    using OxyPlot.Axes;
    using OxyPlot.Legends;

    using Utils;

    using ViewModels;

    public partial class MainWindow : Window
    {
        const int POINT_SIZE = 15;

        private MainViewModel viewModel;

        public MainWindow()
        {
            this.InitializeComponent();

            this.viewModel = new(new FileDialogService());
            this.DataContext = this.viewModel;
            this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            this.viewModel.ConfirmationRequested += this.ViewModel_ConfirmationRequested;

            this.viewModel.UserCanvasPoints.CollectionChanged += this.UserCanvasPoints_CollectionChanged;

            this.runtimeVsCitiesChart.Model = new PlotModel();
        }

        private void UserCanvasPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.drawingCanvas.Children.Clear();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.PointsGiven))
            {
                this.Dispatcher.Invoke(this.DrawPoints);

                if (this.percentageOfOptimalityChart != null)
                {
                    this.percentageOfOptimalityChart.Model = new();
                    this.efficiencyChart.Model = new();
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.BestPathIndices))
            {
                this.Dispatcher.Invoke(this.DrawPath);
                this.Dispatcher.Invoke(this.PlotRuntimeVsNumberOfCities);
                this.Dispatcher.Invoke(this.UpdatePercentageOfOptimalityChart);
                this.Dispatcher.Invoke(this.UpdateEfficiencyChart);
            }
        }

        private void DrawPoints()
        {
            this.drawingCanvas.Children.Clear();

            if (this.viewModel.PointsGiven == null || this.viewModel.PointsGiven.Count == 0)
                return;

            var pointsGiven = this.viewModel.PointsGiven;
            int n = pointsGiven.Count;

            double canvasWidth = this.drawingCanvas.ActualWidth;
            double canvasHeight = this.drawingCanvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0)
            {
                // Canvas has not been rendered yet
                return;
            }

            // Draw points
            for (int i = 0; i < n; i++)
            {
                var point = pointsGiven[i];

                // Create ellipse for point
                Ellipse ellipse = new()
                {
                    Width = POINT_SIZE,
                    Height = POINT_SIZE,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(ellipse, point.X - (POINT_SIZE / 2));
                Canvas.SetTop(ellipse, point.Y - (POINT_SIZE / 2));
                this.drawingCanvas.Children.Add(ellipse);

                // Create text for label
                TextBlock text = new()
                {
                    Text = (i + 1).ToString(),
                    Foreground = Brushes.Red,
                    FontSize = 10, // Static font size
                };

                // Measure the size of the text
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size textSize = text.DesiredSize;

                // Center the text inside the ellipse
                Canvas.SetLeft(text, point.X - (textSize.Width / 2));
                Canvas.SetTop(text, point.Y - (textSize.Height / 2));
                this.drawingCanvas.Children.Add(text);
            }
        }

        private void DrawPath()
        {
            var pointsGiven = this.viewModel.PointsGiven;
            var paintPath = this.viewModel.BestPathIndices;

            if (pointsGiven == null || paintPath == null || paintPath.Count <= 1)
                return;

            var lines = this.drawingCanvas.Children.OfType<Line>().ToList();
            foreach (var line in lines)
            {
                this.drawingCanvas.Children.Remove(line);
            }

            // Draw path
            for (int i = 0; i < paintPath.Count; i++)
            {
                int index1 = paintPath[i];
                int index2 = paintPath[(i + 1) % paintPath.Count];

                Line line = new()
                {
                    X1 = pointsGiven[index1].X,
                    Y1 = pointsGiven[index1].Y,
                    X2 = pointsGiven[index2].X,
                    Y2 = pointsGiven[index2].Y,
                    Stroke = Brushes.Yellow,
                    StrokeThickness = 2
                };
                this.drawingCanvas.Children.Add(line);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.viewModel.IsDrawingMode)
            {
                var position = e.GetPosition(this.drawingCanvas);

                var cappedX = Math.Clamp(position.X, 0, this.drawingCanvas.ActualWidth);
                var cappedY = Math.Clamp(position.Y, 0, this.drawingCanvas.ActualHeight);

                this.viewModel.CursorPosition = $"Cursor Position: ({cappedX:F0}, {cappedY:F0})";
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.viewModel.IsDrawingMode)
            {
                var position = e.GetPosition(this.drawingCanvas);
                if (position.X < 0 || position.X > this.drawingCanvas.ActualWidth || position.Y < 0 || position.Y > this.drawingCanvas.ActualHeight)
                {
                    return;
                }

                this.viewModel.AddPoint(new Point(position.X, position.Y));
                this.DrawCanvasPoints();
            }
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.viewModel.IsDrawingMode)
            {
                var position = e.GetPosition(this.drawingCanvas);
                this.viewModel.RemovePoint(new Point(position.X, position.Y));
                this.DrawCanvasPoints();
            }
        }

        private void DrawCanvasPoints()
        {
            this.drawingCanvas.Children.Clear();

            foreach (var point in this.viewModel.UserCanvasPoints)
            {
                var ellipse = new Ellipse
                {
                    Width = POINT_SIZE,
                    Height = POINT_SIZE,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(ellipse, point.X - POINT_SIZE / 2);
                Canvas.SetTop(ellipse, point.Y - POINT_SIZE / 2);
                this.drawingCanvas.Children.Add(ellipse);
            }
        }

        private void ViewModel_ConfirmationRequested(object? sender, ConfirmationRequestEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(e.Message, e.Caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                e.Callback(result == MessageBoxResult.Yes);
            });
        }

        private void PlotRuntimeVsNumberOfCities()
        {
            // Create a new PlotModel
            var plotModel = new PlotModel { Title = "Runtime vs. Number of Cities" };

            // Configure the legend
            plotModel.IsLegendVisible = true;
            plotModel.Legends.Add(new Legend
            {
                LegendTitle = "Algorithms",
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorderThickness = 0
            });

            // Create the axes
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Number of Cities (n)",
                MinimumPadding = 1,
                AbsoluteMinimum = 0
            };
            plotModel.Axes.Add(xAxis);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Runtime (ms)",
                MinimumPadding = 1,
                AbsoluteMinimum = 0
            };
            plotModel.Axes.Add(yAxis);

            // Iterate over all algorithms
            foreach (var algorithmData in this.viewModel.ChartData.GetAllAlgorithmData())
            {
                // Combine and accumulate all datasets for city counts and runtimes
                var combinedCityCounts = new List<int>();
                var combinedRuntimes = new List<double>();

                foreach (var datasetId in algorithmData.GetCityCountsKeys())
                {
                    combinedCityCounts.AddRange(algorithmData.GetCityCounts(datasetId));
                    combinedRuntimes.AddRange(algorithmData.GetRuntimes(datasetId));
                }

                if (combinedCityCounts.Count == 0 || combinedRuntimes.Count == 0)
                    continue; // Skip if no data

                var lineSeries = new LineSeries
                {
                    Title = algorithmData.Type.ToString(),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerStroke = OxyColors.Black
                };

                var dataPoints = combinedCityCounts.Zip(combinedRuntimes, (n, runtime) => new { n, runtime })
                                                   .OrderBy(dp => dp.n);

                foreach (var dp in dataPoints)
                {
                    lineSeries.Points.Add(new DataPoint(dp.n, dp.runtime));
                }

                plotModel.Series.Add(lineSeries);
            }

            this.runtimeVsCitiesChart.Model = plotModel;
        }

        public void UpdatePercentageOfOptimalityChart()
        {
            // Create a new PlotModel
            var plotModel = new PlotModel { Title = "Percentage of Optimal Cost" };

            plotModel.IsLegendVisible = true;
            plotModel.Legends.Add(new Legend
            {
                LegendTitle = "Metrics",
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorderThickness = 0
            });

            // Create the category axis (X-axis)
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Algorithms" };
            plotModel.Axes.Add(categoryAxis);

            // Create the value axis (Y-axis)
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Percentage of Optimal (%)",
                MinimumPadding = 0,
                AbsoluteMinimum = 0,
                Maximum = 100 // Ensure the maximum is 100%
            };
            plotModel.Axes.Add(valueAxis);

            // Retrieve the latest dataset for the Brute Force algorithm
            var bruteForceCosts = this.viewModel.ChartData.GetLatestCosts().TryGetValue(AlgorithmType.BruteForce, out var costs) ? costs : null;

            if (bruteForceCosts == null || bruteForceCosts.Count == 0)
            {
                // Handle the case where the optimal cost is not available
                return;
            }

            double optimalCost = bruteForceCosts.Min();

            // Create BarSeries for best, worst, and average
            var bestSeries = new BarSeries
            {
                Title = "Best",
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0:0.##}%",
                FillColor = OxyColors.Green
            };

            var worstSeries = new BarSeries
            {
                Title = "Worst",
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0:0.##}%",
                FillColor = OxyColors.Red
            };

            var avgSeries = new BarSeries
            {
                Title = "Average",
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0:0.##}%",
                FillColor = OxyColors.Blue
            };

            // Clear labels to prevent issues
            categoryAxis.Labels.Clear();

            // Iterate over all algorithms and use the latest dataset
            foreach (var algorithmData in this.viewModel.ChartData.GetAllAlgorithmData())
            {
                if (algorithmData.Type == AlgorithmType.BruteForce)
                    continue; // Skip the Brute Force algorithm since it represents the optimal cost

                // Get the latest costs for the algorithm
                var latestCosts = this.viewModel.ChartData.GetLatestCosts().TryGetValue(algorithmData.Type, out var data) ? data : null;
                if (latestCosts == null || latestCosts.Count == 0) continue;

                double bestCost = latestCosts.Min();
                double worstCost = latestCosts.Max();
                double avgCost = latestCosts.Average();

                double avgPercentageOfOptimal = 100.0 - ((avgCost - optimalCost) / optimalCost) * 100.0;
                double worstPercentageOfOptimal = 100.0 - ((worstCost - optimalCost) / optimalCost) * 100.0;
                double bestPercentageOfOptimal = 100.0 - ((bestCost - optimalCost) / optimalCost) * 100.0;

                // Add algorithm name
                categoryAxis.Labels.Add(algorithmData.Type.ToString());

                // Add values to respective series
                bestSeries.Items.Add(new BarItem { Value = bestPercentageOfOptimal });
                worstSeries.Items.Add(new BarItem { Value = worstPercentageOfOptimal });
                avgSeries.Items.Add(new BarItem { Value = avgPercentageOfOptimal });
            }

            // Add the series to the PlotModel
            plotModel.Series.Add(bestSeries);
            plotModel.Series.Add(worstSeries);
            plotModel.Series.Add(avgSeries);

            // Update the property
            this.percentageOfOptimalityChart.Model = plotModel;
        }

        public void UpdateEfficiencyChart()
        {
            // Clear the existing model
            this.efficiencyChart.Model = new PlotModel();

            // Create a new PlotModel
            var plotModel = new PlotModel { Title = "Algorithm Efficiency (Cost vs. Runtime)" };

            // Configure legend
            plotModel.Legends.Add(new Legend
            {
                LegendTitle = "Algorithms",
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorderThickness = 0
            });

            // Define axes
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Runtime (ms)",
                MinimumPadding = 1,
                AbsoluteMinimum = 0
            };
            plotModel.Axes.Add(xAxis);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Cost",
                MinimumPadding = 0.1,
                AbsoluteMinimum = 0
            };
            plotModel.Axes.Add(yAxis);

            // Retrieve the latest dataset for runtime and cost
            var latestRuntimes = this.viewModel.ChartData.GetLatestRuntimes();
            var latestCosts = this.viewModel.ChartData.GetLatestCosts();

            foreach (var algorithmType in this.viewModel.ChartData.GetAllAlgorithmData().Select(a => a.Type))
            {
                if (!latestRuntimes.TryGetValue(algorithmType, out var runtimes) || !latestCosts.TryGetValue(algorithmType, out var costs))
                {
                    continue; // Skip algorithms without data
                }

                if (runtimes.Count == 0 || costs.Count == 0) continue;

                // Create a series for the algorithm
                var series = new ScatterSeries
                {
                    Title = algorithmType.ToString(),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 5
                };

                // Add data points to the series
                for (int i = 0; i < Math.Min(runtimes.Count, costs.Count); i++)
                {
                    series.Points.Add(new ScatterPoint(runtimes[i], costs[i]));
                }

                // Add the series to the plot model
                plotModel.Series.Add(series);
            }

            // Update the chart
            this.efficiencyChart.Model = plotModel;
        }
    }
}