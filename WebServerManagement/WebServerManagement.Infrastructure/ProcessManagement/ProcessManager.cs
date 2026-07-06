using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Core.Services;

namespace WebServerManagement.Infrastructure.ProcessManagement
{
    /// <summary>
    /// Concrete <see cref="IProcessManager"/>: owns every managed website's <see cref="Process"/>,
    /// wires up async console capture, applies the auto-restart policy on unexpected exits, and
    /// keeps a lightweight CPU/RAM/thread-count sample fresh for the UI to poll.
    /// </summary>
    public class ProcessManager : IProcessManager, IDisposable
    {
        private class ManagedProcess
        {
            public WebsiteConfig Config;
            public Process Process;
            public readonly WebsiteRuntimeState State = new WebsiteRuntimeState();
            public readonly List<DateTime> CrashTimestamps = new List<DateTime>();
            public bool StopRequested;
            public DateTime LastSampleTime;
            public TimeSpan LastTotalProcessorTime;
            public readonly object Lock = new object();
        }

        private readonly ConcurrentDictionary<Guid, ManagedProcess> _processes = new ConcurrentDictionary<Guid, ManagedProcess>();
        private readonly RuntimeAdapterFactory _adapterFactory;
        private readonly ISiteLogger _siteLogger;
        private readonly IAppLogger _appLogger;
        private readonly Timer _metricsTimer;

        public event EventHandler<WebsiteOutputEventArgs> OutputReceived;
        public event EventHandler<WebsiteStatusChangedEventArgs> StatusChanged;

        public ProcessManager(RuntimeAdapterFactory adapterFactory, ISiteLogger siteLogger, IAppLogger appLogger)
        {
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
            _siteLogger = siteLogger ?? throw new ArgumentNullException(nameof(siteLogger));
            _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
            _metricsTimer = new Timer(OnMetricsTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public WebsiteRuntimeState GetState(Guid websiteId)
        {
            if (_processes.TryGetValue(websiteId, out var managed)) return managed.State;
            return new WebsiteRuntimeState { WebsiteId = websiteId, Status = WebsiteStatus.Stopped };
        }

        public bool IsRunning(Guid websiteId)
        {
            return _processes.TryGetValue(websiteId, out var managed) && managed.Process != null && !IsExited(managed.Process);
        }

        public void Start(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));

            var managed = _processes.GetOrAdd(website.Id, id => new ManagedProcess { Config = website, State = { WebsiteId = id } });

            lock (managed.Lock)
            {
                managed.Config = website;

                if (managed.Process != null && !IsExited(managed.Process))
                {
                    return; // already running
                }

                managed.StopRequested = false;
                managed.State.Status = WebsiteStatus.Starting;
                RaiseStatusChanged(website.Id, managed.State);

                ProcessStartInfo startInfo;
                try
                {
                    var adapter = _adapterFactory.Resolve(website.RuntimeType);
                    startInfo = adapter.BuildStartInfo(website);
                }
                catch (Exception ex)
                {
                    FailStart(managed, website, ex);
                    return;
                }

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (s, e) => OnOutput(website, e.Data, false);
                process.ErrorDataReceived += (s, e) => OnOutput(website, e.Data, true);
                process.Exited += (s, e) => OnExited(website.Id);

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                catch (Exception ex)
                {
                    FailStart(managed, website, ex);
                    return;
                }

                managed.Process = process;
                managed.LastSampleTime = DateTime.Now;
                managed.LastTotalProcessorTime = TimeSpan.Zero;

                managed.State.Pid = process.Id;
                managed.State.Status = WebsiteStatus.Running;
                managed.State.StartTime = DateTime.Now;
                managed.State.StopTime = null;
                managed.State.LastExitCode = null;
                managed.State.LastError = string.Empty;

                _siteLogger.Log(website.Name, SiteLogCategory.Start, $"Started (PID {process.Id}).");
                RaiseStatusChanged(website.Id, managed.State);
            }
        }

        public void Stop(Guid websiteId)
        {
            if (!TryBeginStop(websiteId, out var managed, out var process)) return;

            ProcessTreeHelper.CloseTree(process.Id);
            if (!SafeWaitForExit(process, 5000))
            {
                ProcessTreeHelper.KillTree(process.Id);
            }
        }

        public void Kill(Guid websiteId)
        {
            if (!TryBeginStop(websiteId, out var managed, out var process)) return;

            ProcessTreeHelper.KillTree(process.Id);
        }

        public void Restart(WebsiteConfig website)
        {
            Stop(website.Id);
            Thread.Sleep(300);
            Start(website);
        }

        public void Pause(Guid websiteId)
        {
            if (!_processes.TryGetValue(websiteId, out var managed)) return;
            if (managed.Process == null || IsExited(managed.Process)) return;

            lock (managed.Lock)
            {
                managed.State.Status = WebsiteStatus.Pausing;
                RaiseStatusChanged(websiteId, managed.State);
            }

            ProcessTreeHelper.SuspendTree(managed.Process.Id);
            _siteLogger.Log(managed.Config.Name, SiteLogCategory.Info, "Paused.");

            lock (managed.Lock)
            {
                managed.State.Status = WebsiteStatus.Paused;
                RaiseStatusChanged(websiteId, managed.State);
            }
        }

        public void Resume(Guid websiteId)
        {
            if (!_processes.TryGetValue(websiteId, out var managed)) return;
            if (managed.Process == null || IsExited(managed.Process)) return;

            ProcessTreeHelper.ResumeTree(managed.Process.Id);
            _siteLogger.Log(managed.Config.Name, SiteLogCategory.Info, "Resumed.");

            lock (managed.Lock)
            {
                managed.State.Status = WebsiteStatus.Running;
                RaiseStatusChanged(websiteId, managed.State);
            }
        }

        public void Forget(Guid websiteId)
        {
            if (!_processes.TryRemove(websiteId, out var managed)) return;

            if (managed.Process != null && !IsExited(managed.Process))
            {
                managed.StopRequested = true;
                ProcessTreeHelper.KillTree(managed.Process.Id);
            }
        }

        private bool TryBeginStop(Guid websiteId, out ManagedProcess managed, out Process process)
        {
            process = null;
            if (!_processes.TryGetValue(websiteId, out managed))
            {
                return false;
            }

            lock (managed.Lock)
            {
                if (managed.Process == null || IsExited(managed.Process))
                {
                    managed.State.Status = WebsiteStatus.Stopped;
                    RaiseStatusChanged(websiteId, managed.State);
                    return false;
                }

                managed.StopRequested = true;
                managed.State.Status = WebsiteStatus.Stopping;
                process = managed.Process;
                RaiseStatusChanged(websiteId, managed.State);
                return true;
            }
        }

        private void FailStart(ManagedProcess managed, WebsiteConfig website, Exception ex)
        {
            managed.State.Status = WebsiteStatus.Error;
            managed.State.LastError = ex.Message;
            _appLogger.Error($"Failed to start website '{website.Name}'.", ex);
            _siteLogger.Log(website.Name, SiteLogCategory.Crash, $"Failed to start: {ex.Message}");
            RaiseStatusChanged(website.Id, managed.State);
        }

        private void OnOutput(WebsiteConfig website, string line, bool isError)
        {
            if (line == null) return;
            _siteLogger.Log(website.Name, isError ? SiteLogCategory.StdErr : SiteLogCategory.StdOut, line);
            OutputReceived?.Invoke(this, new WebsiteOutputEventArgs(website.Id, line, isError));
        }

        private void OnExited(Guid websiteId)
        {
            if (!_processes.TryGetValue(websiteId, out var managed)) return;

            WebsiteConfig configForRestart = null;

            lock (managed.Lock)
            {
                var exitCode = SafeGetExitCode(managed.Process);
                managed.State.LastExitCode = exitCode;
                managed.State.StopTime = DateTime.Now;

                if (managed.StopRequested)
                {
                    managed.State.Status = WebsiteStatus.Stopped;
                    managed.State.Pid = 0;
                    _siteLogger.Log(managed.Config.Name, SiteLogCategory.Stop, $"Stopped (exit code {exitCode}).");
                    RaiseStatusChanged(websiteId, managed.State);
                    return;
                }

                managed.State.Status = WebsiteStatus.Crashed;
                managed.State.Pid = 0;
                managed.CrashTimestamps.Add(DateTime.Now);
                managed.State.RestartCount++;
                _siteLogger.Log(managed.Config.Name, SiteLogCategory.Crash, $"Crashed (exit code {exitCode}).");
                RaiseStatusChanged(websiteId, managed.State);

                var decision = RestartPolicyEvaluator.Evaluate(
                    managed.CrashTimestamps, DateTime.Now, managed.Config.MaxRestartCount, managed.Config.RestartWindowSeconds);

                if (decision == RestartDecision.Restart)
                {
                    configForRestart = managed.Config;
                }
                else
                {
                    managed.State.Status = WebsiteStatus.Error;
                    managed.State.LastError = $"Exceeded {managed.Config.MaxRestartCount} restarts within {managed.Config.RestartWindowSeconds}s.";
                    _appLogger.Error($"Website '{managed.Config.Name}' exceeded its restart policy; giving up.");
                    _siteLogger.Log(managed.Config.Name, SiteLogCategory.Crash, "Exceeded restart policy; giving up automatic restarts.");
                    RaiseStatusChanged(websiteId, managed.State);
                }
            }

            if (configForRestart != null)
            {
                Task.Delay(1000).ContinueWith(_ => Start(configForRestart));
            }
        }

        private void OnMetricsTick(object state)
        {
            foreach (var managed in _processes.Values)
            {
                var process = managed.Process;
                if (process == null) continue;
                if (managed.State.Status != WebsiteStatus.Running) continue;

                try
                {
                    if (IsExited(process)) continue;

                    var sample = ProcessMetricsSampler.Sample(process, managed.LastSampleTime, managed.LastTotalProcessorTime);
                    managed.LastSampleTime = sample.SampledAt;
                    managed.LastTotalProcessorTime = sample.TotalProcessorTime;

                    managed.State.CpuPercent = sample.CpuPercent;
                    managed.State.RamMb = sample.RamMb;
                    managed.State.ThreadCount = sample.ThreadCount;
                }
                catch (InvalidOperationException)
                {
                    // Process exited between the status check and sampling -- ignore this tick.
                }
            }
        }

        private static bool IsExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static bool SafeWaitForExit(Process process, int millisecondsTimeout)
        {
            try
            {
                return process.WaitForExit(millisecondsTimeout);
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static int? SafeGetExitCode(Process process)
        {
            try
            {
                return process?.ExitCode;
            }
            catch
            {
                return null;
            }
        }

        private void RaiseStatusChanged(Guid websiteId, WebsiteRuntimeState state)
        {
            StatusChanged?.Invoke(this, new WebsiteStatusChangedEventArgs(websiteId, state));
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
        }
    }
}
