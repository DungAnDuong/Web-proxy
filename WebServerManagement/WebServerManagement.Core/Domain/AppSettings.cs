namespace WebServerManagement.Core.Domain
{
    /// <summary>
    /// Global application settings, persisted as a single document via <see cref="Interfaces.ISettingsRepository"/>.
    /// </summary>
    public class AppSettings
    {
        public string CaddyExecutablePath { get; set; } = string.Empty;

        public string CaddyConfigFolder { get; set; } = string.Empty;

        public string CaddyAdminApiUrl { get; set; } = "http://localhost:2019";

        public bool RunAtWindowsStartup { get; set; }

        public bool AutoStartWebsitesOnLaunch { get; set; } = true;

        public bool AutoStartReverseProxyOnLaunch { get; set; } = true;

        public int LogRetentionDays { get; set; } = 30;

        public bool DarkMode { get; set; }
    }
}
