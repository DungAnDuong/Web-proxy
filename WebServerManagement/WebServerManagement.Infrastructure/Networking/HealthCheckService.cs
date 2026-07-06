using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Networking
{
    /// <summary>
    /// Polls each tracked website's health check endpoint on its own configured interval and
    /// raises <see cref="HealthCheckFailed"/> when a probe fails, so the process manager can
    /// decide whether to restart the site.
    /// </summary>
    public class HealthCheckService : IHealthCheckService, IDisposable
    {
        private class TrackedSite
        {
            public WebsiteConfig Config;
            public DateTime NextCheckDueAt;
        }

        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private readonly ConcurrentDictionary<Guid, TrackedSite> _trackedSites = new ConcurrentDictionary<Guid, TrackedSite>();
        private readonly IAppLogger _appLogger;
        private Timer _timer;

        public event EventHandler<HealthCheckFailedEventArgs> HealthCheckFailed;

        public HealthCheckService(IAppLogger appLogger)
        {
            _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        public void Track(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));
            if (string.IsNullOrWhiteSpace(website.HealthCheckPath)) return;

            _trackedSites[website.Id] = new TrackedSite
            {
                Config = website,
                NextCheckDueAt = DateTime.Now.AddSeconds(website.HealthCheckIntervalSeconds)
            };
        }

        public void Untrack(Guid websiteId)
        {
            _trackedSites.TryRemove(websiteId, out _);
        }

        public void Start()
        {
            if (_timer != null) return;
            _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void OnTick(object state)
        {
            var now = DateTime.Now;
            foreach (var tracked in _trackedSites.Values)
            {
                if (tracked.NextCheckDueAt > now) continue;

                tracked.NextCheckDueAt = now.AddSeconds(Math.Max(1, tracked.Config.HealthCheckIntervalSeconds));
                _ = ProbeAsync(tracked.Config);
            }
        }

        private async Task ProbeAsync(WebsiteConfig website)
        {
            var url = $"http://localhost:{website.InternalPort}{website.HealthCheckPath}";
            try
            {
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    RaiseFailed(website.Id, $"Health check returned HTTP {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                RaiseFailed(website.Id, $"Health check request failed: {ex.Message}");
            }
        }

        private void RaiseFailed(Guid websiteId, string reason)
        {
            _appLogger.Warn($"Health check failed for website {websiteId}: {reason}");
            HealthCheckFailed?.Invoke(this, new HealthCheckFailedEventArgs(websiteId, reason));
        }

        public void Dispose()
        {
            Stop();
            _httpClient.Dispose();
        }
    }
}
