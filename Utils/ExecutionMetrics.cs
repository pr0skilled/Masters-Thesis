using System.Diagnostics;

namespace Thesis.Utils
{
    public class ExecutionMetrics
    {
        public TimeSpan ElapsedTime { get; private set; }
        public double MemoryUsageMB { get; private set; }
        public double CPUUsagePercentage { get; private set; }

        public static ExecutionMetrics Measure(Action action)
        {
            var process = Process.GetCurrentProcess();
            var startCpuTime = process.TotalProcessorTime;
            var startMemory = process.WorkingSet64;

            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            var endCpuTime = process.TotalProcessorTime;
            var endMemory = process.WorkingSet64;

            return new ExecutionMetrics
            {
                ElapsedTime = stopwatch.Elapsed,
                MemoryUsageMB = (endMemory - startMemory) / (1024.0 * 1024.0),
                CPUUsagePercentage = stopwatch.Elapsed.TotalMilliseconds > 0
                    ? (endCpuTime - startCpuTime).TotalMilliseconds / stopwatch.Elapsed.TotalMilliseconds * 100
                    : 0
            };
        }
    }
}
