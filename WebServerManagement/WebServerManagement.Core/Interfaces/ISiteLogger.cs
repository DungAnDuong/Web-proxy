using System;

namespace WebServerManagement.Core.Interfaces
{
    public enum SiteLogCategory
    {
        Start,
        Stop,
        Restart,
        Crash,
        StdOut,
        StdErr,
        ProxyError,
        Info
    }

    /// <summary>Per-website log writer -- one physical log file per site per day.</summary>
    public interface ISiteLogger
    {
        void Log(string websiteName, SiteLogCategory category, string message);
    }
}
