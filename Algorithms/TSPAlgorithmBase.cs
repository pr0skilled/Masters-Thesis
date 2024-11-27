using System.Windows;

namespace Thesis.Algorithms;

public abstract class TSPAlgorithmBase : ITSPAlgorithm
{
    protected int numberOfPoints;
    protected double[,] distanceMatrix;
    protected double bestScore;

    public List<Point> PointsGiven { get; private set; }
    public List<int> PaintPath { get; set; }

    public TSPAlgorithmBase(List<Point> pointsGiven)
    {
        this.PointsGiven = pointsGiven;
        this.PaintPath = [];
        this.distanceMatrix = new double[pointsGiven.Count, pointsGiven.Count];
    }

    public abstract (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve();

    protected int CalculateDistanceMatrix()
    {
        int max = 0;
        ///Calculate the distances between all points in the graph
        for (int i = 0; i < this.PointsGiven.Count; i++)
        {
            for (int j = this.PointsGiven.Count - 1; j > i; j--)
            {
                if (i != j)
                {

                    this.distanceMatrix[i, j] = this.FindPointDistance(this.PointsGiven[i], this.PointsGiven[j]);
                    this.distanceMatrix[j, i] = this.distanceMatrix[i, j];
                    if (this.distanceMatrix[i, j] > max)
                    {
                        max = (int)this.distanceMatrix[i, j];
                    }
                }
                else
                {
                    this.distanceMatrix[i, j] = 0;
                }
            }
        }

        return max;
    }

    protected double FindPointDistance(Point a, Point b)
    {
        return Math.Sqrt(Math.Pow((a.X - b.X), 2) + Math.Pow((a.Y - b.Y), 2));
    }

    protected double FindPathDistance(string path)
    {
        double distance = 0;
        int first, second;
        int length = path.Length;

        for (int i = 0; i < length; i++)
        {
            first = path[i % length] - 65;
            second = path[(i + 1) % length] - 65;
            distance += this.distanceMatrix[first, second];
        }

        return distance;
    }
}
