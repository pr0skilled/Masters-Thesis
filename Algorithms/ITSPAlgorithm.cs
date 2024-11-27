namespace Thesis.Algorithms
{
    public interface ITSPAlgorithm
    {
        (string BestPath, double BestScore, TimeSpan ElapsedTime) Solve();
    }
}
