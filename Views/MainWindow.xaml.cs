using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using OxyPlot;
using OxyPlot.Series;

using Thesis.Models;

namespace Thesis
{
    using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

    public partial class MainWindow : Window
    {
        private List<Point> pointsGiven = new();
        private int pointSize = 30;
        private int[] paintPath = { 0 };     // List of all that will be be painted
        private List<string> paths = new();
        private Dictionary<string, double> nearestPoints;
        private Dictionary<string, double> subPaths;
        private List<Node> MST;
        private List<Node> Q;
        private string freePoints;
        private int n;
        private int maxCost;
        private Random rand = new();
        private bool _drawPath = false;
        private double[,] distanceMatrix;
        private double bestScore = 0;   //Best path score

        public MainWindow()
        {
            InitializeComponent();

            costChart.Model = new PlotModel();
        }

        #region Click events

        private void selectPointsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Browse Point Data Files",
                Filter = "Data Files (*.txt)|*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                pointsGiven.Clear();
                try
                {
                    DataReader(openFileDialog.FileName);
                    distanceMatrix = new double[n, n];
                    DrawPoints();
                }
                catch (Exception exp)
                {
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(exp.Message);
                }
            }
        }

        private void set1Button_Click(object sender, RoutedEventArgs e)
        {
            pointsGiven.Clear();
            pointsGiven.Add(new Point(842, 123));
            pointsGiven.Add(new Point(657, 197));
            pointsGiven.Add(new Point(531, 194));
            pointsGiven.Add(new Point(286, 336));
            pointsGiven.Add(new Point(140, 733));
            pointsGiven.Add(new Point(109, 723));
            pointsGiven.Add(new Point(314, 941));
            pointsGiven.Add(new Point(600, 747));
            pointsGiven.Add(new Point(834, 707));
            pointsGiven.Add(new Point(843, 626));
            n = pointsGiven.Count;
            distanceMatrix = new double[n, n];
            DrawPoints();
        }

        private void set2Button_Click(object sender, RoutedEventArgs e)
        {
            pointsGiven.Clear();
            pointsGiven.Add(new Point(8, 377));
            pointsGiven.Add(new Point(450, 352));
            pointsGiven.Add(new Point(519, 290));
            pointsGiven.Add(new Point(398, 604));
            pointsGiven.Add(new Point(417, 496));
            pointsGiven.Add(new Point(57, 607));
            pointsGiven.Add(new Point(119, 4));
            pointsGiven.Add(new Point(166, 163));
            pointsGiven.Add(new Point(280, 622));
            pointsGiven.Add(new Point(531, 571));
            n = pointsGiven.Count;
            distanceMatrix = new double[n, n];
            DrawPoints();
        }

        private void set3Button_Click(object sender, RoutedEventArgs e)
        {
            pointsGiven.Clear();
            pointsGiven.Add(new Point(518, 995));
            pointsGiven.Add(new Point(590, 935));
            pointsGiven.Add(new Point(600, 985));
            pointsGiven.Add(new Point(151, 225));
            pointsGiven.Add(new Point(168, 657));
            pointsGiven.Add(new Point(202, 454));
            pointsGiven.Add(new Point(310, 717));
            pointsGiven.Add(new Point(425, 802));
            pointsGiven.Add(new Point(480, 940));
            pointsGiven.Add(new Point(300, 1035));
            n = pointsGiven.Count;
            distanceMatrix = new double[n, n];
            DrawPoints();
        }

        private void createDataButton_Click(object sender, RoutedEventArgs e)
        {
            var pointsNumber = createPointsNumberSelector.Value ?? 0;

            if (pointsNumber < 1)
            {
                MessageBox.Show(string.Format("Error: you need to enter a positive nonzero number"));
                return;
            }

            var filePath = GetSaveFilePath(pointsNumber);
            if (!string.IsNullOrEmpty(filePath))
            {
                WriteDataToFile(filePath, pointsNumber);
                System.Windows.MessageBox.Show("Data file created successfully.");
            }
        }

        private void bruteButton_Click(object sender, RoutedEventArgs e)
        {
            DrawPoints();
            TSP_BF();
            DrawPath();
        }

        private void simulatedButton_Click(object sender, RoutedEventArgs e)
        {
            DrawPoints();
            TSP_SA();
            DrawPath();
        }

        private void geneticButton_Click(object sender, RoutedEventArgs e)
        {
            DrawPoints();
            TSP_GA();
            DrawPath();
        }

        private void primsButton_Click(object sender, RoutedEventArgs e)
        {
            TSP_PRIMS_APPRX();

            //DrawMST
            // DrawTree(MST);
            //Draw path
            DrawPath();
        }

        private void newAlgoButton_Click(object sender, RoutedEventArgs e)
        {
            DrawPoints();
            //Initialize and sort nearestPoints dictionary
            TS_NEW();
            if (stepCheckBox.IsChecked == true)
            {
                DrawPath();
            }
        }

        private void stepButton_Click(object sender, RoutedEventArgs e)
        {
            string bestPath = "";
            //Increments the algorithm by choosing the next subpath and plots result
            TSP_NEW_STEP(ref bestPath);
            //get last path from subPaths
            DrawPoints();
            foreach (var path in subPaths)
            {
                Console.WriteLine(String.Format("Drawing path : {0}, path distance= {1}", path.Key, path.Value));
                StringToIntArray(path.Key, ref paintPath);
                DrawPath();
            }
        }

        #endregion

        #region Algorithms

        public void TSP_BF()
        {
            string bestPath;

            Stopwatch stopWatch = new Stopwatch();
            List<double> scores = new List<double>();
            double newScore;
            //build basePath String
            StringBuilder basePath = new StringBuilder("", n);
            char c;
            stopWatch.Start();
            CalculateDistanceMatrix();
            //PrintDistanceMAtrix();
            for (int i = 0; i < n; i++)
            {
                c = (char)(65 + i);
                basePath.Append(c);
            }

            bestPath = basePath.ToString();
            bestScore = FindPathDistance(bestPath);
            //Console.WriteLine("Base path: " + basePath.ToString() + ", best distance = "+ bestScore.ToString());

            //Console.WriteLine("Finding Paths");
            Utils.Permute(paths, basePath.ToString(), 0, n - 1);

            for (int i = 1; i < paths.Count; i++)
            {
                newScore = FindPathDistance(paths[i]);
                if (newScore < bestScore)
                {
                    bestScore = newScore;
                    bestPath = paths[i];
                }
                if (i % 1000 == 0)
                {
                    scores.Add(bestScore);
                }

            };
            //foreach(string path in paths)
            //{
            //    Console.WriteLine(path);
            //}
            stopWatch.Stop();
            string results1 = String.Format("Brute Force:Best path ={0}, Best distance ={1}", bestPath, bestScore);
            Console.WriteLine(results1);
            StringToIntArray(bestPath, ref paintPath);
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            string results2 = "RunTime " + elapsedTime;
            Console.WriteLine(results2);
            resultsLabel.Content = results1 + ", " + results2;
            PlotCostData(scores, "Brute Force");
            costLabel.Content = String.Format("Brute Force Cost vs 1000 paths- Best path = {0}, Best score = {1}", bestPath, bestScore);
        }

        public void TSP_SA()
        {
            bool hasConverged = false;
            int pathsChecked;     //tally of total number of paths checked in this algorithm
            int counter;
            int improveCount;
            int epochCounter = 0;
            double startT;       // SA start temperature
            double T;
            double Tfactor = 0.95;
            double deltaT;
            int epochN;
            int threshold;
            double costDifference = 0;
            double coin;
            string newPath;
            double tempParameter;
            string bestPath;

            Stopwatch stopWatch = new Stopwatch();
            List<double> scores = new List<double>();
            double newScore;
            //build basePath String
            StringBuilder basePath = new StringBuilder("", n);
            char c;

            Console.WriteLine("Start SA algorithm");
            stopWatch.Start();
            CalculateDistanceMatrix();
            Console.WriteLine("Calculated Distance MAtrix");
            //PrintDistanceMAtrix();
            for (int i = 0; i < n; i++)
            {
                c = (char)(65 + i);
                basePath.Append(c);
            }
            //initialize path temperature, and theshld values
            bestPath = basePath.ToString();
            int start;
            int end;
            int temp;
            startT = FindPathDistance(bestPath);
            bestScore = startT;
            T = startT;
            deltaT = startT * Tfactor;
            epochN = 100 * n;
            threshold = 10 * n;
            improveCount = 0;
            counter = 0;
            pathsChecked = 0;

            ///////////////  Begin Simualted Annealing algorithm

            //While the system has not converged
            while (!hasConverged)
            {
                //Select two different random points in the sequence
                start = rand.Next(n);
                temp = rand.Next(n);
                while (temp == start)
                {
                    temp = rand.Next(n);
                }
                end = temp;
                //Based on Rand(0-1) choose to reverse or Transport the string
                coin = rand.NextDouble();
                if (coin > 0.5)
                {
                    newPath = Utils.Reverse(bestPath, start, end);
                }
                else
                {
                    newPath = Utils.Transport(bestPath, start, end, rand);
                }
                //Calculate the cost difference between the previous bestScore and the new string
                newScore = FindPathDistance(newPath);
                costDifference = newScore - bestScore;
                tempParameter = Math.Exp(-(costDifference / T));
                //if the cost diffference is negative keep the new string, increment improved, and go to the next step
                coin = rand.NextDouble();
                if (costDifference < 0)
                {
                    improveCount++;
                    bestPath = newPath;
                    bestScore = newScore;
                }
                else if (tempParameter > coin)
                //if the cost difference is positive if e^- (costDifference)/T) >  random number from 0-1
                {
                    bestPath = newPath;
                    bestScore = newScore;
                }
                //else discard the new string and go to the next step
                //increment counter and n 
                counter++;
                pathsChecked++;
                //end of loop check condition
                //if counter > epochN && improved ==0
                if (counter > epochN && improveCount == 0)
                {
                    hasConverged = true;
                    scores.Add(bestScore);
                    epochCounter++;
                }
                else if (counter > epochN || improveCount > threshold)
                {
                    /*double Ttest = costDifference / startT;
                    if ( Ttest<= 0.1 && Ttest>0.05)
                    {
                        Tfactor = .95;
                    } else if ( Ttest <=0.05 && Ttest > 0.025) {
                        Tfactor = 0.975;
                    } else if (Ttest <= 0.025)
                    {
                        Tfactor = 0.99;
                    }*/
                    T *= Tfactor;
                    counter = 0;
                    improveCount = 0;
                    scores.Add(bestScore);
                    epochCounter++;
                    //Console.WriteLine(String.Format("New epoch: best Path ={0}, best score = {1}, T ={2}", bestPath, bestScore,T));
                }

            }
            //////////////// End Simualted Annealing Algorithm
            //  print cost to chart
            Console.WriteLine(String.Format("Starting temp : {0}, final temp: {1}", startT, T));
            Console.WriteLine(String.Format("Total paths checked = {0}, total epochs = {1}", pathsChecked, epochCounter));
            Console.WriteLine(String.Format("Final:  best Path ={0}, best score = {1}", bestPath, bestScore));
            PlotCostData(scores, "Simulated Annealing");
            int totalPaths = Utils.Factorial(n);
            costLabel.Content = String.Format("SA Cost vs. temp step- best path = {0}, best score = {1}\nPaths checked ({2}/{3})={4:F08}%", bestPath, bestScore, pathsChecked, totalPaths, (double)pathsChecked * 100 / totalPaths);

            //print out times
            stopWatch.Stop();
            string results1 = String.Format("Simulated Annealing:Best path ={0}, Best distance ={1:F08}", bestPath, bestScore);
            Console.WriteLine(results1);
            StringToIntArray(bestPath, ref paintPath);
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            string results2 = "RunTime " + elapsedTime;
            Console.WriteLine(results2);
            resultsLabel.Content = results1 + ", " + results2;
        }

        public void TSP_GA()
        {
            bool hasConverged = false;
            int pathsChecked;     //tally of total number of paths checked in this algorithm
            int threshold;          // number of generation that must not imp50ve best score for convergence
            int dniCount;           //tacks the number of generations where the score did not improve
            int populationCount;
            int eliteCount;
            int temp;
            int start, end;
            int totalPaths;
            double totalScores;
            double eliteTotal;
            double minScore;
            double avgScore;
            List<string> population = new List<String>();
            string[] children;
            string bestPath;
            double[] nextGenScores;         //score of each child in the population
            double[] roulette;              //The Distribution of top boundaries of probability values of a certain population
            double mutationRate = 0.1;
            String parent1 = "";
            String child1 = "";
            String child2 = "";
            List<double> bestScores = new List<double>();
            List<double> avgScores = new List<double>();
            Stopwatch stopWatch = new Stopwatch();

            //****************Beign GA algorithm
            Console.WriteLine("Starting GA");
            stopWatch.Start();
            //initialize population
            populationCount = 4 * n;
            eliteCount = populationCount / 4;

            threshold = 10 * n;
            children = new string[populationCount];
            nextGenScores = new double[populationCount];
            roulette = new double[eliteCount];
            dniCount = 0;


            CalculateDistanceMatrix();
            //create initial sample population
            for (int i = 0; i < populationCount; i++)
            {
                parent1 = GenerateRandomPath(n, rand);
                while (population.Contains(parent1))
                {
                    parent1 = GenerateRandomPath(populationCount, rand);
                }
                population.Add(parent1);
            }
            minScore = FindPathDistance(population[0]);   // set minScore to the first path score to initialize
            bestPath = population[0];
            //Console.WriteLine(population[0] + ":Initial score set to " + minScore.ToString());
            bestScores.Add(minScore);


            while (!hasConverged)
            {

                totalScores = 0;
                //Step 1 crossover and mutate all members of a population
                for (int i = 0; i < populationCount; i += 2)
                {
                    minScore = bestScores.Last();
                    start = rand.Next(n);
                    temp = rand.Next(n);
                    while (temp == start)
                    {
                        temp = rand.Next(n);
                    }
                    end = temp;
                    Utils.OrderedCrossover(population[i], population[i + 1], start, end, ref child1, ref child2);
                    //run SwapMutation
                    children[i] = Utils.SwapMutation(child1, mutationRate, rand);
                    children[i + 1] = Utils.SwapMutation(child2, mutationRate, rand);
                    //step2 - check the performance of new members and find their score  
                    nextGenScores[i] = FindPathDistance(children[i]);
                    nextGenScores[i + 1] = FindPathDistance(children[i + 1]);
                    //   record best, average score, and sum of scores
                    totalScores += nextGenScores[i] + nextGenScores[i + 1];
                    //find min
                    if (nextGenScores[i] < minScore)
                    {
                        minScore = nextGenScores[i];
                        bestPath = children[i];
                    }
                    if (nextGenScores[i + 1] < minScore)
                    {
                        minScore = nextGenScores[i + 1];
                        bestPath = children[i + 1];
                    }
                }
                Array.Sort(nextGenScores, children);
                /*Console.WriteLine("Sorted children by scores:");
                for (int i=0; i< children.Length; i++)
                {
                    Console.WriteLine(String.Format("rank:{0}, path:{1}, score:{2}", i + 1, children[i], nextGenScores[i]));
                }*/
                //Add average Score
                avgScore = totalScores / populationCount;
                avgScores.Add(avgScore);
                bestScore = bestScores.Last();
                if (bestScore > minScore)
                {
                    dniCount = 0;
                    bestScores.Add(minScore);
                }
                else
                {
                    dniCount++;
                    bestScores.Add(bestScore);
                }


                Console.WriteLine(String.Format("Gen : {6}\nTotal Score = {0}, Avg Score = {1}, bestScore(minScore) = {2}({3}), bestPath = {4} ,did not improve = {5}", totalScores, avgScore, bestScores.Last(), minScore, bestPath, dniCount, bestScores.Count));
                eliteTotal = 0;
                for (int i = 0; i < eliteCount; i++)
                {
                    eliteTotal += nextGenScores[i];
                }
                //Build roulette table
                //Console.WriteLine("Roulette Table");
                for (int i = 0; i < eliteCount; i++)
                {
                    //Console.WriteLine(String.Format("i({0} == last gen :{1}, next gen:{2}, Score:{3}",i,population[i],children[i],nextGenScores[i] ));
                    if (i == 0) { roulette[i] = nextGenScores[i] / eliteTotal; }
                    else { roulette[i] = roulette[i - 1] + nextGenScores[i] / eliteTotal; }
                    //Console.WriteLine(String.Format("i:{0}, score:{1}, roulette[{0}]={2}", i, nextGenScores[i] / totalScores, roulette[i]));
                }
                // if best score has not improved in 'threshold' # of generations  hasConverged = true
                if (dniCount > threshold)
                {
                    hasConverged = true;
                    Console.WriteLine(String.Format("Score Converged!!\nBest path :{0}, best Score {1}, generations:{2}", bestPath, bestScores.Last(), bestScores.Count));


                }
                else
                {
                    // else  step 3 select new population based on roulette
                    for (int i = 0; i < populationCount; i++)
                    {
                        population[i] = SelectPath(children, roulette, rand);
                    }
                }
                if (dniCount < threshold / 2) { mutationRate = .1; }
                else { mutationRate = 0.2; }
            }

            //************End GA Algorithm

            pathsChecked = bestScores.Count * populationCount;
            totalPaths = Utils.Factorial(n - 1) / 2;
            Console.WriteLine(String.Format("Final:  best Path ={0}, best score = {1}", bestPath, bestScore));
            PlotCostData(bestScores, "GA -Best Scores");
            PlotCostData(avgScores, "GA -Average Scores");
            costLabel.Content = String.Format("GA Cost vs. temp step- best path = {0}, best score = {1}\nPaths checked ({2}/{3})={4:F3}%", bestPath, bestScore, pathsChecked, totalPaths, (double)pathsChecked * 100 / totalPaths);

            //print out times
            stopWatch.Stop();
            string results1 = String.Format("GeneticAlgorithm :Best path ={0}, Best distance ={1:F02}", bestPath, bestScore);
            Console.WriteLine(results1);
            StringToIntArray(bestPath, ref paintPath);
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            string results2 = "RunTime " + elapsedTime;
            Console.WriteLine(results2);
            resultsLabel.Content = results1 + ", " + results2;
        }

        public void TSP_PRIMS_APPRX()
        {
            char root;
            String preTraversal;
            Random rand = new Random((int)DateTime.Now.Ticks);
            Stopwatch stopWatch = new Stopwatch();
            int temp;
            double distance;
            Node u, p;
            MST = new List<Node>();
            Q = new List<Node>();

            stopWatch.Start();
            //1)Calculate Distance MAtrix
            maxCost = CalculateDistanceMatrix();
            //PrintDistanceMatrix();

            //Select a random Vertex to be the root
            temp = rand.Next(n);
            //temp = 0;
            //root = (char)(temp + 65);
            root = 'A';
            MST.Add(new Node { Name = root, Parent = '0', Children = "" });
            Console.WriteLine("Selecting '" + root + " as root node of MST.");
            //Find MST using Prims
            //populate Q with all other non-root nodes and their Value is their distance from root
            //sort Q by its nodes' Value property in ascending order
            var sortedQ = from node in Q orderby node.Value ascending select node;
            Q = sortedQ.ToList();
            //Console.WriteLine("Q List");
            for (int i = 0; i < n; i++)
            {
                if (i != (int)root - 65)
                {
                    char c = (char)(i + 65);
                    Q.Add(new Node { Name = c, Parent = root, Children = "", Value = distanceMatrix[temp, i] });
                    //Console.WriteLine(String.Format("Name:{0}, Parent:{1}. Value {2}", Q.Last().Name, Q.Last().Parent, Q.Last().Value));
                }

            }


            //while Q.Count>0
            while (Q.Count > 0)
            {

                //take closest point(u), add to MST,           
                u = Q.First();
                MST.Add(u);
                //remove from Q 
                Q.RemoveAt(0);
                //add it's Name to it's parents children
                p = MST.Find(x => x.Name.Equals(u.Parent));
                p.Children = p.Children + u.Name.ToString();
                //search all remaining points in Q -v, to see if v's distance to u is less than their current Value
                foreach (Node v in Q)
                {
                    distance = distanceMatrix[(int)(u.Name) - 65, (int)(v.Name) - 65];
                    if (v.Value > distance)
                    {
                        //if so update v's Parent and Value
                        v.Value = distance;
                        v.Parent = u.Name;
                    }
                }
                //Console.WriteLine("Printing MST: step #"+Q.Count);
                //foreach (var node in MST)
                //{
                //    Console.WriteLine(String.Format("Name:{0}, Parent:{1}. CHILDREN {2}", node.Name, node.Parent, node.Children));
                //}
                //sort Q by its nodes' Value property in ascending order
                sortedQ = from node in Q orderby node.Value ascending select node;
                Q = sortedQ.ToList();
                //Console.WriteLine("Printing Q:");
                //foreach (var node in Q)
                //{
                //    Console.WriteLine(String.Format("Name:{0}, Parent:{1}. Value {2}", node.Name, node.Parent, node.Value));
                //}

            }
            //Recursively prints MST
            PrintNode(MST.First(), MST);

            //Find Preorder Traversal of MST  as bestPAth
            preTraversal = PreOrder(MST.First(), MST);
            bestScore = FindPathDistance(preTraversal);

            stopWatch.Stop();
            string results1 = String.Format("Prims Aproximate:Best path ={0}, Best distance ={1}", preTraversal, bestScore);
            Console.WriteLine(results1);
            StringToIntArray(preTraversal, ref paintPath);
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds);
            string results2 = "RunTime " + elapsedTime;
            Console.WriteLine(results2);
            resultsLabel.Content = results1 + ", " + results2;

        }

        public void TS_NEW()
        {
            nearestPoints = new Dictionary<string, double>();
            subPaths = new Dictionary<string, double>();
            double minPathCost;
            char[] minPathChar = new char[2];
            string minPath;
            Stopwatch stopWatch = new Stopwatch();
            char temp;
            char[] revPathChar = new char[2];
            string revPath;
            string bestPath = "";

            if (stepCheckBox.IsChecked == true)
            {
                stopWatch.Start();
            }
            Console.WriteLine("Starting New Algorithm");
            freePoints = "";
            subPaths.Clear();
            nearestPoints.Clear();
            //1)Calculate Distance MAtrix
            maxCost = CalculateDistanceMatrix();   //set minPathCost to the maximum cost in the matrix to initialize
            //2) Create String of FreePoints
            //3) create an orderedList of the shortest path between rows
            for (int i = 0; i < n; i++)
            {
                freePoints = String.Concat(freePoints, (char)(65 + i));
                minPathChar[0] = (char)(65 + i);
                minPathCost = maxCost;
                for (int j = 0; j < n; j++)
                {

                    temp = (char)(65 + j);
                    revPathChar[0] = temp;
                    revPathChar[1] = minPathChar[0];
                    revPath = new string(revPathChar);
                    //Console.WriteLine(String.Format("Checking if nearestPoints contains revPath:({0})", revPath));
                    //check if reverse of path is already in list
                    if (!nearestPoints.ContainsKey(revPath))
                    {
                        if (i != j && distanceMatrix[i, j] < minPathCost)
                        {
                            minPathCost = distanceMatrix[i, j];
                            minPathChar[1] = temp;
                        }
                    }
                }


                minPath = new string(minPathChar);
                Console.WriteLine(String.Format("Adding path:{0}, distance:{1}", minPath, minPathCost));
                nearestPoints.Add(minPath, minPathCost);
            }
            Console.WriteLine("Free Points: " + freePoints);
            //PrintDistanceMatrix();

            //Sort nearestPoints in ascending order
            var sortedPaths = from entry in nearestPoints orderby entry.Value ascending select entry;
            nearestPoints = sortedPaths.ToDictionary(x => x.Key, x => x.Value);
            Console.WriteLine("Nearest Points:");
            PrintPaths(nearestPoints);

            if (stepCheckBox.IsChecked == true)
            {
                int steps = 0;
                do
                {
                    steps++;
                    TSP_NEW_STEP(ref bestPath);
                    Console.WriteLine("Step #" + steps.ToString());
                    Console.WriteLine("Printing subPaths");
                    PrintPaths(subPaths);
                } while (!(subPaths.Count == 1 && subPaths.First().Key.Length == n));

                //translate best path to paintPath
                StringToIntArray(bestPath, ref paintPath);
                bestScore = FindPathDistance(bestPath);
                //print out times
                stopWatch.Stop();
                string results1 = String.Format("New Batch:Best path ={0}, Best distance ={1}", bestPath, bestScore);
                Console.WriteLine(results1);
                StringToIntArray(bestPath, ref paintPath);
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
                string results2 = "RunTime " + elapsedTime;
                Console.WriteLine(results2);
                resultsLabel.Content = results1 + ", " + results2;
            }

        }

        public void TSP_NEW_STEP(ref string bestPath)
        {
            string internalPoints = "";
            List<string> removedPaths = new List<string>();

            //Selects the next subpath to add into subPaths
            int matches = 0;
            int cIndex;
            string nearestPoint;
            string replacePath = "";
            string newPath;
            double minPathCost;
            double temp;
            char otherPoint;
            List<string> matchedStrings = new List<string>();

            if (nearestPoints.Count == 0)
            {
                MessageBox.Show(String.Format("Error: nearestPoints Dictionary is empty!"));
                return;
            }
            Console.WriteLine("***********Starting Yfantis Step *****************");
            nearestPoint = nearestPoints.First().Key;
            //Console.WriteLine("Free Points: " + freePoints);
            foreach (string path in subPaths.Keys)
            {
                //Checks how many times the next nearestPoint entry connects to one of the subpaths
                if (MatchString(path, nearestPoint))
                {
                    matches++;
                    matchedStrings.Add(path);
                    //Console.WriteLine("Adding " + path + " to matchedStrings");
                }
                if (matches >= 2) break;

            }
            //if the first entry has no endpoint matches with current subpaths ---makes a new 2-point subpath
            if (matches == 0)
            {
                // add 2-point subpath  entry to subpaths and delete entry from nearestPoints dictionary
                subPaths.Add(nearestPoint, nearestPoints.First().Value);
                //Console.WriteLine("match =0:Adding " + nearestPoint + " distance : " + nearestPoints.First().Value);
                nearestPoints.Remove(nearestPoint);
                newPath = "";
            }
            else if (matches == 1)
            {

                //else if the top entry does match the endpoints with only one subpath 
                ///  then merge point into new subpath  and delete old subpath
                subPaths.Remove(matchedStrings[0]);
                //Console.WriteLine("Merging " + matchedStrings[0] + " and " + nearestPoint);
                newPath = MergePoint(matchedStrings[0], nearestPoint);
                subPaths.Add(newPath, FindPathDistance(newPath));
                //Console.WriteLine("MergePoint:match =1 adding " + newPath + " distance : " + FindPathDistance(newPath));
                nearestPoints.Remove(nearestPoint);


            }
            else
            ///  else it matches endpoints with two distinct subpaths(merging a subpath with a subpath)  
            {
                subPaths.Remove(matchedStrings[0]);
                subPaths.Remove(matchedStrings[1]);
                newPath = MergePaths(matchedStrings[0], matchedStrings[1], nearestPoint);
                //Console.WriteLine("MergePaths:match =2 adding " + newPath + " distance : " + FindPathDistance(newPath));
                subPaths.Add(newPath, FindPathDistance(newPath));
                nearestPoints.Remove(nearestPoint);
            }

            if (newPath.Length > 2)
            {
                //Find internal points of newPath
                internalPoints = newPath.Substring(1, newPath.Length - 2);
                foreach (char c in internalPoints)
                {
                    //Remove internal points of newPath from FreePoints string
                    if (freePoints.Contains(c))
                    {
                        cIndex = freePoints.IndexOf(c);
                        freePoints = freePoints.Remove(cIndex, 1);
                    }

                    //check nearestPoints for any paths containing internal point of new path
                    foreach (var key in nearestPoints.Keys)
                    {
                        if (key.Contains(c))
                        {
                            //Mark for removal any nearest point with internalPoints as endPoints
                            removedPaths.Add(key);
                            //Console.WriteLine("Removing " + key + " From nearestPoints because it contains an internal point.");
                            //Console.WriteLine("Removed Paths Count = " + removedPaths.Count);
                        }
                    }
                }
                // remove from nearestPoints anything marked for removal
                foreach (var path in removedPaths)
                {
                    //Console.WriteLine("Removing " + path + " from nearestPoints.");
                    nearestPoints.Remove(path);
                }
                removedPaths.Clear();
                //Mark for removal any nearestPoint that closes the loop of newPath
                foreach (string key in nearestPoints.Keys)
                {
                    if (newPath.Contains(key.First()) && newPath.Contains(key.Last()))
                    {
                        removedPaths.Add(key);
                        //Console.WriteLine(key + " closes " + newPath + " so it was removed.");
                        //Console.WriteLine("Removed Paths Count = " + removedPaths.Count);
                    }
                }
                // remove from nearestPoints anything marked for removal
                foreach (var path in removedPaths)
                {
                    Console.WriteLine("Removing " + path + " from nearestPoints.");
                    nearestPoints.Remove(path);
                }
            }
            //Console.WriteLine("New Freepoints : " + freePoints);
            Console.WriteLine("Printing subPaths:");
            PrintPaths(subPaths);

            //TODO:  add new nearest point for any freePoints 
            if (!(subPaths.Count == 1 && subPaths.First().Key.Length == n))
            {
                foreach (char c in freePoints)
                {
                    Console.WriteLine("Checking if we need to find a replace path starting with " + c);
                    if (IsNotInNearestPoints(c))
                    {
                        otherPoint = '1';
                        //find c's other subpath endpoint
                        foreach (string key in subPaths.Keys)
                        {
                            if (key.Contains(c))
                            {
                                if (c.Equals(key.First()))
                                {
                                    otherPoint = key.Last();
                                }
                                else
                                {
                                    otherPoint = key.First();
                                }
                            }
                        }
                        //find new nearest point starts with c and ends any freepoint other than its other endpoint
                        int i = (int)(c) - 65;
                        minPathCost = maxCost;
                        foreach (char fp in freePoints)
                        {
                            if (!fp.Equals(otherPoint) && !fp.Equals(c))
                            {
                                int j = (int)(fp) - 65;
                                temp = distanceMatrix[i, j];
                                if (minPathCost >= temp)
                                {
                                    minPathCost = temp;
                                    replacePath = String.Concat(c, fp);
                                }

                            }
                        }
                        nearestPoints.Add(replacePath, minPathCost);
                        Console.WriteLine("Adding " + replacePath + "(" + minPathCost + ") to NearestPoints");
                    }
                }
                //Re-sort  NearestPoints list
                var sortedPaths = from entry in nearestPoints orderby entry.Value ascending select entry;
                nearestPoints = sortedPaths.ToDictionary(x => x.Key, x => x.Value);
                Console.WriteLine("Nearest Points list:");
                PrintPaths(nearestPoints);
            }
            else
            {
                bestPath = subPaths.First().Key;
                Console.WriteLine("Success: algorithm has found a solution : " + bestPath + ", distance=" + subPaths.First().Value.ToString());
            }
        }

        #endregion

        public String GenerateRandomPath(int size, Random rand)
        {
            List<char> letters = new List<char>(size);
            StringBuilder sb = new StringBuilder();
            int temp;
            char c;
            for (int i = 0; i < size; i++)
            {
                c = (char)(65 + i);
                letters.Add(c);
            }
            while (sb.Length < size)
            {
                temp = rand.Next(letters.Count);
                c = letters[temp];
                letters.Remove(c);
                sb.Append(c);
            }
            //Console.WriteLine("Generating a new string :" + sb.ToString());
            return sb.ToString();
        }

        public void StringToIntArray(string str, ref int[] p)
        {
            p = new int[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                p[i] = (int)str[i] - 65;
            }
        }

        public void PlotCostData(List<double> scores, string seriesName)
        {
            var plotModel = costChart.Model ?? new PlotModel { Title = "Cost Data" };

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
            costChart.Model = plotModel;
        }

        public string SelectPath(string[] pop, double[] rTable, Random r)
        {
            double coin = r.NextDouble();
            for (int i = 0; i < rTable.Length - 1; i++)
            {
                if (coin < rTable[i])
                {
                    return pop[i];
                }
            }
            return pop[pop.Length - 1];

        }

        public bool IsNotInNearestPoints(char c)
        {
            //Return true if the character c is not listed in NearestPoints
            bool result = true;
            foreach (string key in nearestPoints.Keys)
            {
                //Console.WriteLine("IsNotInNEaerestPoint:  checking if " + c + " is in " + key);
                if (key.First().Equals(c))
                {
                    result = false;
                    //Console.WriteLine("IsNotInNEaerestPoint:" + c + " is FOUND in " + key);
                }
            }
            return result;
        }

        public string MergePaths(string str1, string str2, string str3)
        {
            string longPath;
            bool isEndpointClosest = true;
            double closestDistance;
            string checkPath;
            string nearestEndpoint;
            string secondEndpoint;
            string mergedPath;
            string firstSplice = "", secondSplice = "";
            string splicePoints;
            double noSpliceDistance = 0;   //endpoint plus pathcut
            double spliceDistance = 0;    //new distance from endpoints
            double newDistance;
            double endpointDistance;

            Console.WriteLine("MergePaths: Merging " + str1 + " and " + str2 + " via " + str3);
            if (str3.Length != 2)
            {
                Console.WriteLine("MergePaths:  error str3 is not length 2!");
                return "";
            }
            //str3 is connecting string, str1 is first matched substring and str2 is second matched substring
            // if both  str1 and str2 are 2-point paths then just merge them at closest point
            //if ( str1.Length ==2 && str2.Length == 2)
            //{
            mergedPath = SimpleMergePath(str1, str2, str3);
            Console.WriteLine("SimpleMergePath = " + str1 + " + " + str2);
            return mergedPath;
            //}


            //1) store endpoint distance
            /*endpointDistance = FindPathDistance(str3);
            
            //2) Find which path is longest 
            if( FindPathDistance(str2)> FindPathDistance(str1))
            {
                longPath = str2;
            }
            else
            {
                longPath = str1;
            }
            //3) find which point of str3 connects with long part and what other point is
            if (str3[0].Equals(longPath[0]) || str3[0].Equals(longPath[longPath.Length-1]))
            {
                nearestEndpoint = str3[1].ToString();
                secondEndpoint = str3[0].ToString();
            } else if (str3[1].Equals(longPath[0]) || str3[1].Equals(longPath[longPath.Length-1]))
            {
                nearestEndpoint = str3[0].ToString();
                secondEndpoint = str3[1].ToString();

            } else
            {
                MessageBox.Show("Merge Paths: Error str3 does not connect with longPath");
                return "";
               
            }
           
            // check if any point on longpath is closer to nearestEndpoint than longpath's endpoint 
            closestDistance = endpointDistance;
            for ( int  i =1; i < longPath.Length-1; i++)
            {
                checkPath = String.Concat(longPath[i].ToString(), nearestEndpoint);
                newDistance = FindPathDistance(checkPath);
                if( newDistance < closestDistance)
                {
                    closestDistance = newDistance;
                    splicePoints = String.Concat(longPath[i],longPath[i+1]) ;
 
                    noSpliceDistance = endpointDistance + FindPathDistance(splicePoints);
                    spliceDistance = FindPathDistance(String.Concat(longPath[i], nearestEndpoint)) + FindPathDistance(String.Concat(longPath[i + 1], secondEndpoint));
                    if(spliceDistance < noSpliceDistance)
                    {
                        isEndpointClosest = false;
                        firstSplice = longPath.Substring(0, i + 1);
                        secondSplice = longPath.Substring(i + 1);
                    }

                }
            }
            if (isEndpointClosest)    //then merge two strings and be finished
            {
                mergedPath = SimpleMergePath(str1, str2, str3);
                Console.WriteLine("SimpleMergePath = " + str1 + " + " + str2);
            }
            else  // check if merging string can produce a shorter total path 
            {
                mergedPath = String.Concat(firstSplice,str3, secondSplice);
                Console.WriteLine("Splice MergePath = " + firstSplice + " + " + str3 + " + " + secondSplice);
            } 
            
            return mergedPath;*/
        }

        public string SimpleMergePath(string str1, string str2, string str3)
        {
            char[] revArray;
            string mergedPath;
            if (!str3[0].Equals(str1[0]) && !str3[0].Equals(str1[str1.Length - 1]))  //str3's first char does NOT connect to str1
            {
                //Console.WriteLine("str3 was " + str3);
                revArray = str3.ToCharArray();
                Array.Reverse(revArray);
                str3 = new string(revArray);
                // Console.WriteLine("Reversed str3 is now " + str3);

            }
            if (str3[0].Equals(str1[0]) && str3[1].Equals(str2[0]))      //front-to-front
            {
                revArray = str1.ToCharArray();
                Array.Reverse(revArray);
                str1 = new string(revArray);
                mergedPath = String.Concat(str1, str2);

            }
            else if (str3[0].Equals(str1[0]) && str3[1].Equals(str2[str2.Length - 1]))  // front-to-back
            {
                mergedPath = String.Concat(str2, str1);

            }
            else if (str3[0].Equals(str1[str1.Length - 1]) && str3[1].Equals(str2[0]))   //back-to-front
            {
                mergedPath = String.Concat(str1, str2);

            }
            else if (str3[0].Equals(str1[str1.Length - 1]) && str3[1].Equals(str2[str2.Length - 1]))  //back-to-back
            {
                revArray = str2.ToCharArray();
                Array.Reverse(revArray);
                str2 = new string(revArray);
                mergedPath = String.Concat(str1, str2);
            }
            else
            {
                Console.WriteLine("Merge Paths:Error  str3 does not match endpoins of str1 and str2.");
                return "";
            }
            //Console.WriteLine("Merge Paths; best path is  " + mergedPath);
            return mergedPath;
        }
        public string MergePoint(string str1, string str2)
        {
            string checkString;
            string mergePoint;
            string bestString = "";
            int closestPoint;
            bool closestToFront;
            double closestDistance;
            double rootDistance;    //distance from endpoint + distance of old string path
            double newDistance;
            double endpointDistance;
            bool mergeNeeded;
            if (str2.Length != 2)
            {
                MessageBox.Show("Error: Second string is not a 2-point string");
                return "";
            }

            // if str1 is only two point then merging is simple

            if (str2[0].Equals(str1[0]))
            {
                mergePoint = str2[1].ToString();
                closestToFront = true;
                if (str1.Length == 2)
                {
                    //Console.WriteLine("Merging " + mergePoint + " with " + str1);
                    return String.Concat(mergePoint, str1);
                }
            }
            else if (str2[0].Equals(str1[str1.Length - 1]))
            {
                mergePoint = str2[1].ToString();
                closestToFront = false;
                if (str1.Length == 2)
                {

                    //Console.WriteLine("Merging " + str1 + " with " + mergePoint);
                    return String.Concat(str1, mergePoint);
                }
            }
            else if (str2[str2.Length - 1].Equals(str1[0]))
            {
                mergePoint = str2[0].ToString();
                closestToFront = true;
                if (str1.Length == 2)
                {
                    //Console.WriteLine("Merging " + mergePoint + " with " + str1);
                    return String.Concat(mergePoint, str1);
                }
            }
            else if (str2[str2.Length - 1].Equals(str1[str1.Length - 1]))
            {
                mergePoint = str2[0].ToString();
                closestToFront = false;
                if (str1.Length == 2)
                {
                    //Console.WriteLine("Merging " + str1 + " with " + mergePoint);
                    return String.Concat(str1, mergePoint);
                }
            }
            else
            {
                MessageBox.Show("Error:  MergePoint received 2 strings that do not match!");
                return "";
            }
            //IF str1 is greater than length 2

            //Find the distance of endpoint-mergepoint
            endpointDistance = FindPathDistance(str2);
            closestDistance = endpointDistance;

            StringBuilder sb = new StringBuilder(str1);
            mergeNeeded = false;
            //check entire string to see if any non-end points are closer than endpointDistance
            if (closestToFront) { closestPoint = 0; }
            else { closestPoint = str1.Length - 1; }
            for (int i = 1; i < str1.Length - 1; i++)
            {
                checkString = String.Concat(mergePoint, str1[i]);
                if (closestDistance > FindPathDistance(checkString))
                {
                    mergeNeeded = true;
                    closestDistance = FindPathDistance(checkString);
                    closestPoint = i;
                }
            }
            if (mergeNeeded)
            {

                //find out which neighboring point is second closest to mergePoint
                if (FindPathDistance(String.Concat(mergePoint, str1[closestPoint - 1])) > FindPathDistance(String.Concat(mergePoint, str1[closestPoint + 1])))
                {
                    rootDistance = endpointDistance + FindPathDistance(String.Concat(str1[closestPoint], str1[closestPoint + 1]));
                    newDistance = FindPathDistance(String.Concat(mergePoint, str1[closestPoint])) + FindPathDistance(String.Concat(mergePoint, str1[closestPoint + 1]));
                    if (rootDistance > newDistance)
                    {
                        //merge point near closestPoint
                        bestString = String.Concat(str1.Substring(0, closestPoint + 1), mergePoint, str1.Substring(closestPoint + 1, str1.Length - (closestPoint + 1)));
                        //Console.WriteLine("Merge needed:  Merging " + mergePoint + " and " + str1 + " into " + bestString);

                    }
                    else
                    {
                        if (closestToFront)
                        {
                            bestString = String.Concat(mergePoint, str1);
                        }
                        else
                        {
                            bestString = String.Concat(str1, mergePoint);
                        }
                    }
                }
                else
                {
                    rootDistance = endpointDistance + FindPathDistance(String.Concat(str1[closestPoint], str1[closestPoint - 1]));
                    newDistance = FindPathDistance(String.Concat(mergePoint, str1[closestPoint])) + FindPathDistance(String.Concat(mergePoint, str1[closestPoint - 1]));
                    if (rootDistance > newDistance)
                    {
                        //merge point near closestPoint
                        bestString = String.Concat(str1.Substring(0, closestPoint), mergePoint, str1.Substring(closestPoint, str1.Length - closestPoint));
                        //Console.WriteLine("Merge needed:  Merging " + mergePoint + " and " + str1 +" into " + bestString);
                    }
                    else
                    {
                        if (closestToFront)
                        {
                            bestString = String.Concat(mergePoint, str1);
                        }
                        else
                        {
                            bestString = String.Concat(str1, mergePoint);
                        }
                    }
                }

            }
            else///No merge needed
            {
                if (closestToFront)
                {
                    bestString = String.Concat(mergePoint, str1);
                    //Console.WriteLine("No Merge needed : Merging " + mergePoint + " with " + str1 + " into "  +bestString);
                }
                else
                {
                    bestString = String.Concat(str1, mergePoint);
                    //Console.WriteLine("No Merge needed : Merging " + str1+ " with " + mergePoint + " into " + bestString);
                }
            }
            return bestString;
        }

        public bool MatchString(string str1, string str2)
        {
            //Returns a merged string if the endpoints of the strings match returns an empty string if they do not
            int len1 = str1.Length;
            int len2 = str2.Length;
            char[] subarray;
            string rev;
            bool isMatch = false;
            if (str1[0] == str2[0])
            {
                isMatch = true;
            }
            else if (str1[0] == str2[len2 - 1])
            {
                isMatch = true;
            }
            else if (str1[len1 - 1] == str2[0])
            {
                isMatch = true;
            }
            else if (str1[len1 - 1] == str2[len2 - 1])
            {
                isMatch = true;
            }
            return isMatch;
        }

        private int CalculateDistanceMatrix()
        {
            int max = 0;
            ///Calculate the distances between all points in the graph
            for (int i = 0; i < n; i++)
            {
                for (int j = n - 1; j > i; j--)
                {
                    //Console.WriteLine(String.Format("({0},{1})", i, j));
                    if (i != j)
                    {

                        distanceMatrix[i, j] = FindPointDistance(pointsGiven[i], pointsGiven[j]);
                        distanceMatrix[j, i] = distanceMatrix[i, j];
                        if (distanceMatrix[i, j] > max)
                        {
                            max = (int)distanceMatrix[i, j];
                        }
                    }
                    else
                    {
                        distanceMatrix[i, j] = 0;
                    }
                }
            }
            return max;

        }

        private void PrintPaths(Dictionary<string, double> paths)
        {
            //Print key and subpath values
            Console.WriteLine(string.Format("TotalPaths ={0}\nKey\tValue", paths.Count));
            foreach (KeyValuePair<string, double> kvp in paths)
            {
                Console.WriteLine(String.Format("{0}\t{1}", kvp.Key, kvp.Value));
            }
        }

        private double FindPointDistance(Point a, Point b)
        {
            return Math.Pow((a.X - b.X), 2) + Math.Pow((a.Y - b.Y), 2);
        }

        public double FindPathDistance(string path)
        {
            double distance;
            int first, second;
            distance = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                first = (int)path[i] - 65;
                second = (int)path[i + 1] - 65;
                distance += distanceMatrix[first, second];
            }

            return distance;
        }

        public void PrintNode(Node node, List<Node> T)
        {
            PrintNode(node, 0, T);
        }

        private void PrintNode(Node node, int indentation, List<Node> T)
        {
            Node childNode;
            // This prefixes the value with the necessary amount of indentation
            for (int i = 0; i < indentation; i++)
            {
                Console.Write("\t");
            }
            Console.Write("-" + node.Name + "\n");

            // Recursively call the child nodes.
            foreach (char c in node.Children)
            {
                childNode = T.Find(x => x.Name.Equals(c));
                PrintNode(childNode, indentation + 1, T); // Increment the indentation counter.
            }
        }

        public string PreOrder(Node r, List<Node> nodes)
        {

            //Recursive function to return preorder traversal
            StringBuilder sb = new StringBuilder();

            //if root.children.Equals("") return root
            if (r.Children.Equals(""))
            {
                sb.Append(r.Name);
            }
            else
            {
                sb.Append(r.Name);
                //else search for PReorder of each child and merge answers into  each child
                foreach (char c in r.Children)
                {
                    var child = nodes.Find(x => x.Name.Equals(c));
                    sb.Append(PreOrder(child, nodes));
                }

            }
            return sb.ToString();
        }

        private void DataReader(string spath)
        {
            try
            {
                using var sr = new StreamReader(spath);
                string line;
                int counter = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    counter++;
                    string[] points = line.Split(' ');
                    if (!int.TryParse(points[0], out int x))
                    {
                        MessageBox.Show($"Error: Line {counter} x coordinate is not a valid integer");
                        pointsGiven.Clear();
                        return;
                    }
                    if (!int.TryParse(points[1], out int y))
                    {
                        MessageBox.Show($"Error: Line {counter} y coordinate is not a valid integer");
                        pointsGiven.Clear();
                        return;
                    }
                    pointsGiven.Add(new Point(x, y));
                }
                n = counter;
                Console.WriteLine($"File {spath} has been read and contains {n} points");
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private void DrawTree(List<Node> T)
        {
            // Draw the tree T on the canvas
            foreach (Node node in T)
            {
                int i = node.Name - 65;
                if (!string.IsNullOrEmpty(node.Children))
                {
                    foreach (char c in node.Children)
                    {
                        int j = c - 65;
                        Line line = new Line
                        {
                            X1 = pointsGiven[i].X + pointSize / 2,
                            Y1 = pointsGiven[i].Y + pointSize / 2,
                            X2 = pointsGiven[j].X + pointSize / 2,
                            Y2 = pointsGiven[j].Y + pointSize / 2,
                            Stroke = Brushes.Red,
                            StrokeThickness = 4
                        };
                        drawingCanvas.Children.Add(line);
                    }
                }
            }
        }

        private void DrawPoints()
        {
            drawingCanvas.Children.Clear();

            if (pointsGiven.Count == 0) return;

            // Calculate scaling factor
            double maxX = pointsGiven.Max(point => point.X);
            double maxY = pointsGiven.Max(point => point.Y);
            double canvasWidth = drawingCanvas.ActualWidth;
            double canvasHeight = drawingCanvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0)
            {
                // Canvas has not been rendered yet
                return;
            }

            double scaleX = canvasWidth / maxX;
            double scaleY = canvasHeight / maxY;
            double scalingFactor = Math.Min(scaleX, scaleY);

            for (int i = 0; i < n; i++)
            {
                // Scale the coordinates
                double scaledX = pointsGiven[i].X * scalingFactor;
                double scaledY = pointsGiven[i].Y * scalingFactor;

                // Create ellipse for point
                double scaledPointSize = Math.Max(pointSize * scalingFactor, 5);
                Ellipse ellipse = new Ellipse
                {
                    Width = scaledPointSize,
                    Height = scaledPointSize,
                    Fill = Brushes.White,
                    Stroke = Brushes.WhiteSmoke,
                    StrokeThickness = Math.Max(2 * scalingFactor, 1)
                };
                Canvas.SetLeft(ellipse, scaledX - (scaledPointSize / 2));
                Canvas.SetTop(ellipse, scaledY - (scaledPointSize / 2));
                drawingCanvas.Children.Add(ellipse);

                // Create text for label
                TextBlock text = new TextBlock
                {
                    Text = ((char)(65 + i)).ToString(),
                    Foreground = Brushes.Red,
                    FontSize = Math.Max(12 * scalingFactor, 8)
                };
                Canvas.SetLeft(text, scaledX + 3 * scalingFactor);
                Canvas.SetTop(text, scaledY + 3 * scalingFactor);
                drawingCanvas.Children.Add(text);
            }
        }

        private void DrawPath()
        {
            if (paintPath.Length == 0 || pointsGiven.Count == 0) return;

            // Calculate scaling factor
            double maxX = pointsGiven.Max(point => point.X);
            double maxY = pointsGiven.Max(point => point.Y);
            double canvasWidth = drawingCanvas.ActualWidth;
            double canvasHeight = drawingCanvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0)
            {
                // Canvas has not been rendered yet
                return;
            }

            double scaleX = canvasWidth / maxX;
            double scaleY = canvasHeight / maxY;
            double scalingFactor = Math.Min(scaleX, scaleY);

            // Draw lines between points
            for (int i = 0; i < paintPath.Length - 1; i++)
            {
                // Scale the coordinates
                double x1 = pointsGiven[paintPath[i]].X * scalingFactor;
                double y1 = pointsGiven[paintPath[i]].Y * scalingFactor;
                double x2 = pointsGiven[paintPath[i + 1]].X * scalingFactor;
                double y2 = pointsGiven[paintPath[i + 1]].Y * scalingFactor;

                Line line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Yellow,
                    StrokeThickness = Math.Max(4 * scalingFactor, 1)
                };
                drawingCanvas.Children.Add(line);
            }
        }

        private string? GetSaveFilePath(int pointsNumber)
        {
            var rootDir = System.IO.Path.Combine(Utils.GetSolutionDirectoryPath(), "Data");

            var dialog = new SaveFileDialog
            {
                FileName = $"points{pointsNumber}.txt",
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultDirectory = rootDir
            };

            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }

        private void WriteDataToFile(string filePath, int pointsNumber)
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(fs);
            var rand = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < pointsNumber; i++)
            {
                sw.WriteLine("{0} {1}", rand.Next(500), rand.Next(500));
            }
        }


        public void PrintNearestPoints()
        {
            Console.WriteLine("Printing nearestPoints dictionary:");
            foreach (var entry in nearestPoints)
            {
                Console.WriteLine(String.Format("{0} : {1}", entry.Key, entry.Value));
            }
        }

        public void generateTList(List<double> Xtemps, double start, double end, double delta)
        {
            double tempT = start;
            Xtemps.Clear();
            //  generate a list of x cordinates representing the Temnperature for charting 
            while (tempT > end)
            {
                Xtemps.Add(tempT);
                tempT -= delta;
            }
        }

        private void PrintDistanceMatrix()
        {
            char header;
            for (int i = 0; i < n; i++)

            {
                header = (char)(65 + i);
                StringBuilder sb = new StringBuilder(header.ToString());
                sb.Append(": ");
                for (int j = 0; j < n; j++)
                {
                    sb.Append(distanceMatrix[i, j].ToString() + " ");
                }
                Console.WriteLine(sb.ToString());
            }
        }
    }
}