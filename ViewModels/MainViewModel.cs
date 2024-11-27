using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Thesis.ViewModels
{
    using System.IO;

    using Algorithms;

    using Models;

    using Utils;

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IFileDialogService fileDialogService;
        private string rootDirectory;

        private ObservableCollection<Point> pointsGiven;
        private List<int> bestPathIndices;
        private double bestScore;
        private TimeSpan elapsedTime;
        private string bestPathString;
        private bool isRunning;
        private int createPointsNumber;
        private bool isStepping;
        private bool canStep;
        private string resultsSummary;
        private string costSummary;
        private bool isDrawingMode;
        private string cursorPosition;

        private TSPCustomAlgorithm customAlgorithmInstance;

        public ObservableCollection<Point> UserCanvasPoints { get; set; } = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ConfirmationRequestEventArgs> ConfirmationRequested;

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

        public string BestPathString
        {
            get => this.bestPathString;
            set
            {
                this.bestPathString = value;
                this.OnPropertyChanged(nameof(this.BestPathString));
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

        public TimeSpan ElapsedTime
        {
            get => this.elapsedTime;
            set
            {
                this.elapsedTime = value;
                this.OnPropertyChanged(nameof(this.ElapsedTime));
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

        public string ResultsSummary
        {
            get => this.resultsSummary;
            set
            {
                if(string.IsNullOrWhiteSpace(value))
                {
                    this.resultsSummary = "Results";
                }
                else
                {
                    this.resultsSummary = value;
                    this.OnPropertyChanged(nameof(this.ResultsSummary));
                }
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
                    this.OnPropertyChanged(nameof(this.CostSummary));
                }
            }
        }

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

        public bool IsNotDrawingMode => !this.IsDrawingMode;

        public string CursorPosition
        {
            get => this.cursorPosition;
            set
            {
                this.cursorPosition = value;
                this.OnPropertyChanged(nameof(this.CursorPosition));
            }
        }

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
        public ICommand RunCustomAlgorithmStepCommand { get; }
        public ICommand EnableDrawingModeCommand { get; }
        public ICommand ClearCanvasCommand { get; }
        public ICommand SavePointsCommand { get; }
        public ICommand CancelDrawingCommand { get; }
        public ICommand RunTestAlgorithmCommand { get; }
        public ICommand RunTestPlusAlgorithmCommand { get; }

        public MainViewModel(IFileDialogService fileDialogService)
        {
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
            this.RunCustomAlgorithmStepCommand = new RelayCommand(this.ExecuteStep, this.CanExecuteRunAlgorithm);
            this.RunTestAlgorithmCommand = new RelayCommand(this.RunTestAlgorithm, this.CanExecuteRunAlgorithm);
            this.RunTestPlusAlgorithmCommand = new RelayCommand(this.RunTestPlusAlgorithm, this.CanExecuteRunAlgorithm);
            this.EnableDrawingModeCommand = new RelayCommand(_ => this.EnableDrawingMode());
            this.ClearCanvasCommand = new RelayCommand(_ => this.ClearCanvas(), _ => this.IsDrawingMode);
            this.SavePointsCommand = new RelayCommand(_ => this.SavePointsToFile());
            this.CancelDrawingCommand = new RelayCommand(_ => this.CancelDrawing());
        }

        private bool CanExecuteRunAlgorithm(object parameter)
        {
            return this.PointsGiven.Count > 0 && !this.IsRunning;
        }

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
            /*this.pointsGiven.Add(new Point(421, 62));
            this.pointsGiven.Add(new Point(329, 99));
            this.pointsGiven.Add(new Point(266, 97));
            this.pointsGiven.Add(new Point(143, 168));
            this.pointsGiven.Add(new Point(70, 367));
            this.pointsGiven.Add(new Point(55, 362));
            this.pointsGiven.Add(new Point(157, 471));
            this.pointsGiven.Add(new Point(300, 374));
            this.pointsGiven.Add(new Point(417, 354));
            this.pointsGiven.Add(new Point(422, 313));*/
            this.pointsGiven.Add(new Point(0, 0));
            this.pointsGiven.Add(new Point(0, 500));
            this.pointsGiven.Add(new Point(500, 0));
            this.pointsGiven.Add(new Point(500, 500));

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

            var algorithm = new TSPBruteForce(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Brute Force:Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = $"Brute Force Cost vs 1000 paths- Best path = {this.BestPathString}, Best score = {this.BestScore:F0}";

            this.IsRunning = false;
        }

        private void RunSimulatedAnnealingAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPSimulatedAnnealing(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Simulated Annealing: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = $"SA Cost vs. temp step - Best path = {this.BestPathString}, Best score = {this.BestScore:F0}, Paths checked ({algorithm.PathsChecked}/{algorithm.TotalPaths}) = {(double)algorithm.PathsChecked * 100 / algorithm.TotalPaths:F2}%";

            this.IsRunning = false;
        }

        private void RunGeneticAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPGeneticAlgorithm(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Genetic Algorithm: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = $"GA Cost vs. temp step - Best path = {this.BestPathString}, Best score = {this.BestScore:F0}, Paths checked ({algorithm.PathsChecked}/{algorithm.TotalPaths}) = {(double)algorithm.PathsChecked * 100 / algorithm.TotalPaths:F2}%";

            this.IsRunning = false;
        }

        private void RunPrimsApproximationAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPPrimsApproximation(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Prim's Approximate: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = string.Empty; // No specific cost summary mentioned, leave as empty or add if needed.

            this.IsRunning = false;
        }

        private async void RunCustomAlgorithm(object parameter)
        {
            this.IsRunning = true;

            this.customAlgorithmInstance = new TSPCustomAlgorithm(new List<Point>(this.PointsGiven));

            var result = this.customAlgorithmInstance.Solve();

            this.BestPathIndices = this.customAlgorithmInstance.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = string.Empty;

            this.IsRunning = false;
        }

        private void ExecuteStep(object parameter)
        {
            this.customAlgorithmInstance ??= new TSPCustomAlgorithm(new List<Point>(this.PointsGiven));

            if (!this.customAlgorithmInstance.HasConverged)
            {
                this.customAlgorithmInstance.Step();

                this.BestPathIndices = this.customAlgorithmInstance.PaintPath;

                if (this.customAlgorithmInstance.HasConverged)
                {
                    var result = this.customAlgorithmInstance.GetResult();

                    this.BestPathString = result.BestPath;
                    this.BestScore = result.BestScore;
                    this.ElapsedTime = result.ElapsedTime;

                    this.IsRunning = false;
                }
            }
        }

        private void RunTestAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPIAM(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Test: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = string.Empty; // No specific cost summary mentioned, leave as empty or add if needed.

            this.IsRunning = false;
        }

        private void RunTestPlusAlgorithm(object parameter)
        {
            this.IsRunning = true;

            var algorithm = new TSPIAMPlus(new List<Point>(this.PointsGiven));
            var result = algorithm.Solve();

            this.BestPathIndices = algorithm.PaintPath;
            this.BestPathString = result.BestPath;
            this.BestScore = result.BestScore;
            this.ElapsedTime = result.ElapsedTime;

            this.ResultsSummary = $"Test+: Best path = {this.BestPathString}, Best distance = {this.BestScore:F0}, RunTime = {this.ElapsedTime}";
            this.CostSummary = string.Empty; // No specific cost summary mentioned, leave as empty or add if needed.

            this.IsRunning = false;
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
                    this.PointsGiven.Add(new Point(x, y));
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
                var rand = new Random((int)DateTime.Now.Ticks);
                for (int i = 0; i < pointsNumber; i++)
                {
                    sw.WriteLine("{0} {1}", rand.Next(10, 500), rand.Next(10, 500));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written:");
                Console.WriteLine(e.Message);
                MessageBox.Show("The file could not be written: " + e.Message);
            }
        }

        private void EnableDrawingMode()
        {
            this.ClearCanvas();
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

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnConfirmationRequested(string message, string caption, Action<bool> callback)
        {
            ConfirmationRequested?.Invoke(this, new ConfirmationRequestEventArgs(message, caption, callback));
        }

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
