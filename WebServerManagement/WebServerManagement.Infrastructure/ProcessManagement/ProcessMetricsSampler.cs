using System;
using System.Diagnostics;

namespace WebServerManagement.Infrastructure.ProcessManagement
{
    public class ProcessMetricsSample
    {
        public double CpuPercent { get; set; }
        public double RamMb { get; set; }
        public int ThreadCount { get; set; }
        public DateTime SampledAt { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
    }

    /// <summary>
    /// Computes CPU%/RAM/thread-count snapshots for a live <see cref="Process"/> using the
    /// standard delta-of-processor-time formula. Pure with respect to its inputs (previous
    /// sample values are passed in rather than held as static state), so it stays testable and
    /// reusable across however many processes the process manager is tracking.
    /// </summary>
    public static class ProcessMetricsSampler
    {
        public static ProcessMetricsSample Sample(Process process, DateTime previousSampleTime, TimeSpan previousTotalProcessorTime)
        {
            process.Refresh();

            var now = DateTime.Now;
            var currentTotalProcessorTime = process.TotalProcessorTime;

            double cpuPercent = 0;
            var elapsedMs = (now - previousSampleTime).TotalMilliseconds;
            if (elapsedMs > 0 && previousTotalProcessorTime > TimeSpan.Zero)
            {
                var cpuUsedMs = (currentTotalProcessorTime - previousTotalProcessorTime).TotalMilliseconds;
                cpuPercent = cpuUsedMs / (elapsedMs * Environment.ProcessorCount) * 100.0;
            }

            return new ProcessMetricsSample
            {
                CpuPercent = Math.Max(0, Math.Round(cpuPercent, 1)),
                RamMb = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
                ThreadCount = process.Threads.Count,
                SampledAt = now,
                TotalProcessorTime = currentTotalProcessorTime
            };
        }
    }
}
