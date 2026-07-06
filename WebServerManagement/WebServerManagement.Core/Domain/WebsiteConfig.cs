using System;
using System.Collections.Generic;
using WebServerManagement.Core.Enums;

namespace WebServerManagement.Core.Domain
{
    /// <summary>
    /// Persisted configuration for a single managed website. This is the object edited by
    /// Add/Edit Website and stored via <see cref="Interfaces.IWebsiteRepository"/>.
    /// </summary>
    public class WebsiteConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public int InternalPort { get; set; }

        public string SourceFolder { get; set; } = string.Empty;

        public RuntimeType RuntimeType { get; set; } = RuntimeType.Node;

        public string NodeExecutablePath { get; set; } = string.Empty;

        public string WorkingDirectory { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public string Arguments { get; set; } = string.Empty;

        public EnvironmentMode Environment { get; set; } = EnvironmentMode.Production;

        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public bool AutoStart { get; set; }

        public bool Enabled { get; set; } = true;

        public bool EnableSsl { get; set; }

        public string CertPath { get; set; } = string.Empty;

        public string KeyPath { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>Maximum number of automatic restarts allowed within <see cref="RestartWindowSeconds"/> before the site is marked Error.</summary>
        public int MaxRestartCount { get; set; } = 5;

        /// <summary>Rolling time window, in seconds, that <see cref="MaxRestartCount"/> is evaluated against.</summary>
        public int RestartWindowSeconds { get; set; } = 300;

        /// <summary>Relative path (e.g. "/health") polled by the health check service. Empty disables health checking.</summary>
        public string HealthCheckPath { get; set; } = string.Empty;

        public int HealthCheckIntervalSeconds { get; set; } = 30;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
