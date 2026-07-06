using System.Collections.Generic;
using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Interfaces
{
    public class ReverseProxyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ReverseProxyResult Ok(string message = "") => new ReverseProxyResult { Success = true, Message = message };
        public static ReverseProxyResult Fail(string message) => new ReverseProxyResult { Success = false, Message = message };
    }

    /// <summary>
    /// Generates the reverse-proxy configuration for the currently enabled websites and applies
    /// it to the supervised Caddy process (write config, validate, reload).
    /// </summary>
    public interface IReverseProxyManager
    {
        bool IsRunning { get; }

        void Start();

        void Stop();

        /// <summary>Regenerates the Caddyfile from <paramref name="websites"/>, validates it, and reloads Caddy.</summary>
        ReverseProxyResult ApplyConfiguration(IEnumerable<WebsiteConfig> websites);
    }
}
