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

            this.costChart.Model = new PlotModel();
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
            }
            else if (e.PropertyName == nameof(MainViewModel.BestPathIndices))
            {
                this.Dispatcher.Invoke(this.DrawPath);
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
                    Text = ((char)(65 + i)).ToString(),
                    Foreground = Brushes.Red,
                    FontSize = 12, // Static font size
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

        public void PlotCostData(List<double> scores, string seriesName)
        {
            var plotModel = this.costChart.Model ?? new PlotModel { Title = "Cost Data" };

            // Check if the series with the specified name already exists
            var existingSeries = plotModel.Series
                .OfType<LineSeries>()
                .FirstOrDefault(s => s.Title == seriesName);

            if (existingSeries == null)
            {
                // Create a new series if it doesn't exist
                var lineSeries = new LineSeries { Title = seriesName };
                for (int i = 0; i < scores.Count; i++)
                {
                    lineSeries.Points.Add(new DataPoint(i, scores[i]));
                }
                plotModel.Series.Add(lineSeries);
            }
            else
            {
                // Update the points if the series exists
                existingSeries.Points.Clear();
                for (int i = 0; i < scores.Count; i++)
                {
                    existingSeries.Points.Add(new DataPoint(i, scores[i]));
                }
            }

            // Update the PlotView
            this.costChart.Model = plotModel;
        }
    }
}