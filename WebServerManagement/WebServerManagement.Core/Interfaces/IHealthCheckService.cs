using System;
using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Interfaces
{
    public class HealthCheckFailedEventArgs : EventArgs
    {
        public Guid WebsiteId { get; }
        public string Reason { get; }

        public HealthCheckFailedEventArgs(Guid websiteId, string reason)
        {
            WebsiteId = websiteId;
            Reason = reason;
        }
    }

    /// <summary>
    /// Periodically probes running websites that declare a <see cref="WebsiteConfig.HealthCheckPath"/>
    /// and raises <see cref="HealthCheckFailed"/> when a site stops responding so the process manager
    /// can decide whether to restart it.
    /// </summary>
    public interface IHealthCheckService
    {
        event EventHandler<HealthCheckFailedEventArgs> HealthCheckFailed;

        void Track(WebsiteConfig website);

        void Untrack(Guid websiteId);

        void Start();

        void Stop();
    }
}
