using System;
using WebServerManagement.Core.Enums;

namespace WebServerManagement.Core.Domain
{
    /// <summary>
    /// Live, in-memory runtime state for a website. Never persisted as configuration -- rebuilt
    /// every time the process manager starts a site, and refreshed continuously by the metrics
    /// sampler and health check service.
    /// </summary>
    public class WebsiteRuntimeState
    {
        public Guid WebsiteId { get; set; }

        public WebsiteStatus Status { get; set; } = WebsiteStatus.Stopped;

        public int Pid { get; set; }

        public double CpuPercent { get; set; }

        public double RamMb { get; set; }

        public int ThreadCount { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? StopTime { get; set; }

        public int RestartCount { get; set; }

        public int? LastExitCode { get; set; }

        public bool? LastHealthCheckOk { get; set; }

        public DateTime? LastHealthCheckAt { get; set; }

        public double? ResponseTimeMs { get; set; }

        public string LastError { get; set; } = string.Empty;
    }
}
