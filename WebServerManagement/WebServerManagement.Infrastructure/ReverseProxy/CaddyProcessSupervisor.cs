using System;
using System.Diagnostics;
using System.IO;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.ReverseProxy
{
    /// <summary>
    /// Starts/stops the Caddy binary as a supervised child process of this application. The user
    /// must supply caddy.exe (download from https://caddyserver.com/download) and point
    /// AppSettings.CaddyExecutablePath at it -- we do not fabricate or fetch the binary.
    /// </summary>
    public class CaddyProcessSupervisor : IDisposable
    {
        private readonly IAppLogger _appLogger;
        private Process _caddyProcess;
        private readonly object _lock = new object();

        public CaddyProcessSupervisor(IAppLogger appLogger)
        {
            _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _caddyProcess != null && !SafeHasExited(_caddyProcess);
                }
            }
        }

        public void Start(string caddyExecutablePath, string caddyfilePath)
        {
            if (string.IsNullOrWhiteSpace(caddyExecutablePath) || !File.Exists(caddyExecutablePath))
            {
                throw new FileNotFoundException(
                    "Caddy executable not found. Configure the path to caddy.exe under Settings.", caddyExecutablePath);
            }

            lock (_lock)
            {
                if (_caddyProcess != null && !SafeHasExited(_caddyProcess)) return;

                var startInfo = new ProcessStartInfo
                {
                    FileName = caddyExecutablePath,
                    Arguments = $"run --config \"{caddyfilePath}\" --adapter caddyfile",
                    WorkingDirectory = Path.GetDirectoryName(caddyExecutablePath) ?? string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (s, e) => LogCaddyLine(e.Data);
                process.ErrorDataReceived += (s, e) => LogCaddyLine(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _caddyProcess = process;
                _appLogger.Info($"Caddy started (PID {process.Id}).");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_caddyProcess == null || SafeHasExited(_caddyProcess)) return;

                try
                {
                    _caddyProcess.Kill();
                    _caddyProcess.WaitForExit(5000);
                    _appLogger.Info("Caddy stopped.");
                }
                catch (Exception ex)
                {
                    _appLogger.Error("Failed to stop Caddy.", ex);
                }
                finally
                {
                    _caddyProcess.Dispose();
                    _caddyProcess = null;
                }
            }
        }

        private void LogCaddyLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _appLogger.Info($"[caddy] {line}");
        }

        private static bool SafeHasExited(Process process)
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

        public void Dispose()
        {
            Stop();
        }
    }
}
