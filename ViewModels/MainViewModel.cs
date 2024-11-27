using System.IO;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Thesis.ViewModels
{
    using System.Collections.Specialized;

    using Algorithms;

    using Models;

    using Utils;

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IFileDialogService fileDialogService;
        private ObservableCollection<Point> pointsGiven;
        private List<int> bestPathIndices;
        private TimeSpan elapsedTime;
        private double bestScore;
        private int createPointsNumber;
        private int userCitiesCount;
        private int currentStepIndex;
        private int optimalKnownScore;
        private bool isRunning;
        private bool canStep;
        private bool isDrawingMode;
        private Visibility sliderVisibility = Visibility.Collapsed;
        private string bestPathString;
        private string resultsSummary;
        private string costSummary;
        private string cursorPosition;
        private string rootDirectory;

        private TSPCustomAlgorithm customAlgorithmInstance;

        public ObservableCollection<Point> UserCanvasPoints { get; set; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ConfirmationRequestEventArgs> ConfirmationRequested;

        #region Properties

        public ObservableCollection<Point> PointsGiven
        {
            get => this.pointsGiven;
            set
            {
                this.pointsGiven = value;
                this.OnPropertyChanged(nameof(this.PointsGiven));
            }
        }

        public List<int> BestPathIndices
        {
            get => this.bestPathIndices;
            set
            {
                this.bestPathIndices = value;
                this.OnPropertyChanged(nameof(this.BestPathIndices));
            }
        }

        public TimeSpan ElapsedTime
        {
            get => this.elapsedTime;
            set
            {
                this.elapsedTime = value;
                this.OnPropertyChanged(nameof(this.ElapsedTime));
            }
        }

        public double BestScore
        {
            get => this.bestScore;
            set
            {
                this.bestScore = value;
                this.OnPropertyChanged(nameof(this.BestScore));
            }
        }

        public int CreatePointsNumber
        {
            get => this.createPointsNumber;
            set
            {
                if (value < 0)
                    value = 0;

                if (this.createPointsNumber != value)
                {
                    this.createPointsNumber = value;
                    this.OnPropertyChanged(nameof(this.CreatePointsNumber));
                }
            }
        }

        public int UserCitiesCount
        {
            get => this.userCitiesCount;
            set
            {
                this.userCitiesCount = value;
                this.OnPropertyChanged(nameof(this.UserCitiesCount));
            }
        }

        public int CurrentStepIndex
        {
            get => this.currentStepIndex;
            set
            {
                if (this.currentStepIndex != value)
                {
                    this.currentStepIndex = value;
                    this.OnPropertyChanged(nameof(this.CurrentStepIndex));
                    this.UpdatePaintPath();
                }
            }
        }

        public int MaxStepIndex => this.customAlgorithmInstance?.IntermediateRoutes?.Count - 1 ?? 0;

        public bool IsRunning
        {
            get => this.isRunning;
            set
            {
                this.isRunning = value;
                this.OnPropertyChanged(nameof(this.IsRunning));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsNotDrawingMode => !this.IsDrawingMode;

        public bool IsDrawingMode
        {
            get => this.isDrawingMode;
            set
            {
                this.isDrawingMode = value;
                this.OnPropertyChanged(nameof(this.IsDrawingMode));
                this.OnPropertyChanged(nameof(this.IsNotDrawingMode)); // For inverse visibility
            }
        }

        public Visibility SliderVisibility
        {
            get => this.sliderVisibility;
            set
            {
                if (this.sliderVisibility != value)
                {
                    this.sliderVisibility = value;
                    this.OnPropertyChanged(nameof(this.SliderVisibility));
                }
            }
        }

        public string BestPathString
        {
            get => this.bestPathString;
            set
            {
                this.bestPathString = value;
                this.OnPropertyChanged(nameof(this.BestPathString));
            }
        }

        public string ResultsSummary
        {
            get => this.resultsSummary;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.resultsSummary = "Results";
                }
                else
                {
                    this.resultsSummary = value;
                }
                this.OnPropertyChanged(nameof(this.ResultsSummary));
            }
        }

        public string CostSummary
        {
            get => this.costSummary;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.costSummary = "Cost";
                }
                else
                {
                    this.costSummary = value;
                }
                this.OnPropertyChanged(nameof(this.CostSummary));
            }
        }

        public string CursorPosition
        {
            get => this.cursorPosition;
            set
            {
                this.cursorPosition = value;
                this.OnPropertyChanged(nameof(this.CursorPosition));
            }
        }

        #endregion

        #region Commands

        public ICommand SelectPointsFileCommand { get; }
        public ICommand CreateDataFileCommand { get; }
        public ICommand LoadSet1Command { get; }
        public ICommand LoadSet2Command { get; }
        public ICommand LoadSet3Command { get; }
        public ICommand RunBruteForceCommand { get; }
        public ICommand RunSimulatedAnnealingCommand { get; }
        public ICommand RunGeneticAlgorithmCommand { get; }
        public ICommand RunPrimsApproximationCommand { get; }
        public ICommand RunCustomAlgorithmCommand { get; }
        public ICommand EnableDrawingModeCommand { get; }
        public ICommand ClearCanvasCommand { get; }
        public ICommand SavePointsCommand { get; }
        public ICommand CancelDrawingCommand { get; }

        #endregion

        public MainViewModel(IFileDialogService fileDialogService)
        {
            this.UserCanvasPoints.CollectionChanged += UserCanvasPointsChanged;
            this.fileDialogService = fileDialogService;
            this.rootDirectory = Path.Combine(Utils.GetSolutionDirectoryPath(), "Data");
            this.PointsGiven = [];
            this.SelectPointsFileCommand = new RelayCommand(this.SelectPointsFile);
            this.CreateDataFileCommand = new RelayCommand(this.CreateDataFile);
            this.LoadSet1Command = new RelayCommand(this.LoadSet1);
            this.LoadSet2Command = new RelayCommand(this.LoadSet2);
            this.LoadSet3Command = new RelayCommand(this.LoadSet3);
            this.RunBruteForceCommand = new RelayCommand(this.RunBruteForceAlgorithm, this.CanExecuteRunAlgorithm);
            this.RunSimulatedAnnealingCommand = new RelayCommand(this.RunSimulatedAnnealingAlgorithm, this.CanExecuteRunAlgorithm);
            this.RunGeneticAlgorithmCommand = new RelayCommand(this.RunGeneticAlgorithm, this.CanExecuteRunAlgorithm);
            this.RunPrimsApproximationCommand = new RelayCommand(this.RunPrimsApproximationAlgorithm, this.CanExecuteRunAlgorithm);
            this.RunCustomAlgorithmCommand = new RelayCommand(this.RunCustomAlgorithm, this.CanExecuteRunAlgorithm);
            this.EnableDrawingModeCommand = new RelayCommand(_ => this.EnableDrawingMode());
            this.ClearCanvasCommand = new RelayCommand(_ => this.ClearCanvas(), _ => this.IsDrawingMode);
            this.SavePointsCommand = new RelayCommand(_ => this.SavePointsToFile());
            this.CancelDrawingCommand = new RelayCommand(_ => this.CancelDrawing());
        }

        #region Non-public Members

        private void SelectPointsFile(object parameter)
        {
            string filter = "Data Files (*.txt)|*.txt";
            string filePath = this.fileDialogService.OpenFile(filter, this.rootDirectory);

            if (!string.IsNullOrEmpty(filePath))
            {
                this.PointsGiven.Clear();

                try
                {
                    this.DataReader(filePath);
                }
                catch (Exception exp)
                {
                    MessageBox.Show("The file could not be read: " + exp.Message);
                }
            }

            this.SliderVisibility = Visibility.Collapsed;
        }

        private void CreateDataFile(object parameter)
        {
            if (this.CreatePointsNumber < 1)
            {
                MessageBox.Show("Error: you need to enter a positive nonzero number");
                return;
            }

            string defaultFileName = $"points{this.CreatePointsNumber}.txt";
            string filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            string filePath = this.fileDialogService.SaveFile(defaultFileName, filter, this.rootDirectory);

            if (!string.IsNullOrEmpty(filePath))
            {
                this.WriteDataToFile(filePath, this.CreatePointsNumber);
                MessageBox.Show("Data file created successfully.");
            }
        }

        private void LoadSet1(object parameter)
        {
            this.pointsGiven.Clear();
            this.pointsGiven.Add(new Point(421, 62));
            this.pointsGiven.Add(new Point(329, 99));
            this.pointsGiven.Add(new Point(266, 97));
            this.pointsGiven.Add(new Point(143, 168));
            this.pointsGiven.Add(new Point(70, 367));
            this.pointsGiven.Add(new Point(55, 362));
            this.pointsGiven.Add(new Point(157, 471));
            this.pointsGiven.Add(new Point(300, 374));
            this.pointsGiven.Add(new Point(417, 354));
            this.pointsGiven.Add(new Point(422, 313));

            this.SliderVisibility = Visibility.Collapsed;

            this.OnPropertyChanged(nameof(this.PointsGiven));
        }

        private void LoadSet2(object parameter)
        {
            this.pointsGiven.Clear();
            this.pointsGiven.Add(new Point(4, 189));
            this.pointsGiven.Add(new Point(225, 176));
            this.pointsGiven.Add(new Point(260, 145));
            this.pointsGiven.Add(new Point(199, 302));
            this.pointsGiven.Add(new Point(209, 248));
            this.pointsGiven.Add(new Point(29, 304));
            this.pointsGiven.Add(new Point(60, 2));
            this.pointsGiven.Add(new Point(83, 82));
            this.pointsGiven.Add(new Point(140, 311));
            this.pointsGiven.Add(new Point(266, 286));

            this.SliderVisibility = Visibility.Collapsed;

            this.OnPropertyChanged(nameof(this.PointsGiven));
        }

        private void LoadSet3(object parameter)
        {
            this.pointsGiven.Clear();
            this.pointsGiven.Add(new Point(259, 498));
            this.pointsGiven.Add(new Point(295, 468));
            this.pointsGiven.Add(new Point(300, 492));
            this.pointsGiven.Add(new Point(76, 113));
            this.pointsGiven.Add(new Point(84, 329));
            this.pointsGiven.Add(new Point(101, 227));
            this.pointsGiven.Add(new Point(155, 359));
            this.pointsGiven.Add(new Point(213, 401));
            this.pointsGiven.Add(new Point(240, 470));
            this.pointsGiven.Add(new Point(150, 418));

            this.SliderVisibility = Visibility.Collapsed;

            this.OnPropertyChanged(nameof(this.PointsGiven));
        }

        private async void RunBruteForceAlgorithm(object parameter)
        {
            if (this.PointsGiven.Count > 13)
            {
                var tcs = new TaskCompletionSource<bool>();

                this.OnConfirmationRequested(
                    "The brute-force algorithm may take a long time with more than 15 points. Do you want to continue?",
                    "Long Computation Time",
                    result => tcs.SetResult(result));

                bool shouldRun = await tcs.Task;

                if (!shouldRun)
                {
                    return;
                }
            }

            this.IsRunning = true;

            this.SliderVisibility = Visibility.Collapsed;

            var algorithm = new TSPBruteForce(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Brute Force: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime.TotalSeconds:F6} seconds";
            this.CostSummary = $"Brute Force Cost vs 1000 paths- Best path = {this.BestPathString}, Best score = {this.BestScore:F0}";

            this.IsRunning = false;
        }

        private void RunSimulatedAnnealingAlgorithm(object parameter)
        {
            this.IsRunning = true;

            this.SliderVisibility = Visibility.Collapsed;

            var algorithm = new TSPSimulatedAnnealing(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Simulated Annealing: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime.TotalSeconds:F6} seconds";
            this.CostSummary = $"SA Cost vs. temp step - Best path = {this.BestPathString}, Best score = {this.BestScore:F0}, Paths checked ({algorithm.PathsChecked}/{algorithm.TotalPaths}) = {(double)algorithm.PathsChecked * 100 / algorithm.TotalPaths:F2}%";

            this.IsRunning = false;
        }

        private void RunGeneticAlgorithm(object parameter)
        {
            this.IsRunning = true;

            this.SliderVisibility = Visibility.Collapsed;

            var algorithm = new TSPGeneticAlgorithm(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Genetic Algorithm: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime.TotalSeconds:F6} seconds";
            this.CostSummary = $"GA Cost vs. temp step - Best path = {this.BestPathString}, Best score = {this.BestScore:F0}, Paths checked ({algorithm.PathsChecked}/{algorithm.TotalPaths}) = {(double)algorithm.PathsChecked * 100 / algorithm.TotalPaths:F2}%";

            this.IsRunning = false;
        }

        private void RunPrimsApproximationAlgorithm(object parameter)
        {
            this.IsRunning = true;

            this.SliderVisibility = Visibility.Collapsed;

            var algorithm = new TSPPrimsApproximation(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Prim's Approx: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime.TotalSeconds:F6} seconds";
            this.CostSummary = string.Empty;

            this.IsRunning = false;
        }

        private void RunCustomAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPCustomAlgorithm(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.customAlgorithmInstance = algorithm;

            this.OnPropertyChanged(nameof(this.MaxStepIndex));

            this.CurrentStepIndex = this.MaxStepIndex;

            this.SliderVisibility = Visibility.Visible;

            this.UpdatePaintPath();

            // Update BestScore and BestPathString for the initial route
            this.BestScore = result.BestScore;
            this.BestPathString = result.BestPath;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Custom Algorithm: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime.TotalSeconds:F6} seconds";
            this.CostSummary = string.Empty;

            this.IsRunning = false;
        }

        private void EnableDrawingMode()
        {
            this.ClearCanvas();
            this.SliderVisibility = Visibility.Collapsed;
            this.IsDrawingMode = true;
            this.PointsGiven.Clear();
            this.OnPropertyChanged(nameof(this.PointsGiven));
        }

        private void ClearCanvas()
        {
            this.UserCanvasPoints.Clear();
            this.ResultsSummary = string.Empty;
            this.CostSummary = string.Empty;
            this.OnPropertyChanged(nameof(this.UserCanvasPoints));
        }

        private void UpdatePaintPath()
        {
            if (this.customAlgorithmInstance?.IntermediateRoutes != null && this.customAlgorithmInstance.IntermediateRoutes.Count > 0)
            {
                var route = this.customAlgorithmInstance.IntermediateRoutes[this.CurrentStepIndex];
                this.BestPathIndices = new List<int>(route);
                // Ensure the path is cyclic by returning to the starting city
                this.BestPathIndices.Add(route[0]);

                // Update BestScore and BestPathString based on the current step
                double totalCost = this.customAlgorithmInstance.CalculateRouteCost(route);
                this.BestScore = totalCost;

                this.BestPathString = this.customAlgorithmInstance.BuildPathString(route);
            }
        }

        private void SavePointsToFile()
        {
            string filter = "Data Files (*.txt)|*.txt";
            string filePath = this.fileDialogService.SaveFile("points.txt", filter, this.rootDirectory);

            if (!string.IsNullOrEmpty(filePath))
            {
                using var writer = new StreamWriter(filePath);
                foreach (var point in this.UserCanvasPoints)
                {
                    writer.WriteLine($"{point.X:F0} {point.Y:F0}");
                }
                MessageBox.Show("Points saved successfully.");
            }

            this.CancelDrawing();
        }

        private void CancelDrawing()
        {
            this.ClearCanvas();

            this.IsDrawingMode = false;
        }

        private bool CanExecuteRunAlgorithm(object parameter)
        {
            return this.PointsGiven.Count > 0 && !this.IsRunning;
        }

        private void DataReader(string filePath)
        {
            try
            {
                using var sr = new StreamReader(filePath);
                string line;
                int counter = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    counter++;
                    string[] points = line.Split(' ');
                    if (!int.TryParse(points[0], out int x))
                    {
                        MessageBox.Show($"Error: Line {counter} x coordinate is not a valid integer");
                        this.PointsGiven.Clear();
                        return;
                    }
                    if (!int.TryParse(points[1], out int y))
                    {
                        MessageBox.Show($"Error: Line {counter} y coordinate is not a valid integer");
                        this.PointsGiven.Clear();
                        return;
                    }
                    if (x < 0 || x > 750 ||
                        y < 0 || y > 750)
                    {
                        MessageBox.Show($"Error: Line {counter} coordinates are not in range");
                        this.PointsGiven.Clear();
                        return;
                    }
                    this.PointsGiven.Add(new Point(x, y));
                }

                this.ClearCanvas();

                var regex = new Regex(@"\(-(\d+)-\)");
                var match = regex.Match(filePath);
                if (match.Success)
                {
                    int.TryParse(match.Groups[1].Value, out int number);
                    this.optimalKnownScore = number;
                }

                this.OnPropertyChanged(nameof(this.PointsGiven));
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                MessageBox.Show("The file could not be read: " + e.Message);
            }
        }

        private void WriteDataToFile(string filePath, int pointsNumber)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var sw = new StreamWriter(fs);
                var rand = new Random();
                for (int i = 0; i < pointsNumber; i++)
                {
                    sw.WriteLine("{0} {1}", rand.Next(750), rand.Next(750));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written:");
                Console.WriteLine(e.Message);
                MessageBox.Show("The file could not be written: " + e.Message);
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void UserCanvasPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UserCitiesCount = UserCanvasPoints.Count;
        }

        protected virtual void OnConfirmationRequested(string message, string caption, Action<bool> callback)
        {
            ConfirmationRequested?.Invoke(this, new ConfirmationRequestEventArgs(message, caption, callback));
        }

        #endregion

        public void AddPoint(Point point)
        {
            this.UserCanvasPoints.Add(point);
            this.OnPropertyChanged(nameof(this.UserCanvasPoints));
        }

        public void RemovePoint(Point point)
        {
            var pointToRemove = this.UserCanvasPoints.FirstOrDefault(p => Math.Abs(p.X - point.X) < 5 && Math.Abs(p.Y - point.Y) < 5);
            if (pointToRemove != default)
            {
                this.UserCanvasPoints.Remove(pointToRemove);
                this.OnPropertyChanged(nameof(this.UserCanvasPoints));
            }
        }
    }
}
