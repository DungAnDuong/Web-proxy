using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Core.Services;

namespace WebServerManagement.Infrastructure.ReverseProxy
{
    /// <summary>
    /// Implements <see cref="IReverseProxyManager"/> on top of Caddy: regenerates the Caddyfile
    /// from the current website list via <see cref="CaddyfileBuilder"/>, validates it, then
    /// either starts Caddy (first run) or asks the already-running instance to reload -- never
    /// applying a configuration that fails validation.
    /// </summary>
    public class CaddyReverseProxyManager : IReverseProxyManager
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly CaddyProcessSupervisor _supervisor;
        private readonly IAppLogger _appLogger;

        public CaddyReverseProxyManager(ISettingsRepository settingsRepository, CaddyProcessSupervisor supervisor, IAppLogger appLogger)
        {
            _settingsRepository = settingsRepository;
            _supervisor = supervisor;
            _appLogger = appLogger;
        }

        public bool IsRunning => _supervisor.IsRunning;

        public void Start()
        {
            var settings = _settingsRepository.Get();
            var caddyfilePath = GetCaddyfilePath(settings);

            if (!File.Exists(caddyfilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(caddyfilePath) ?? string.Empty);
                File.WriteAllText(caddyfilePath, CaddyfileBuilder.Build(Enumerable.Empty<WebsiteConfig>()));
            }

            _supervisor.Start(settings.CaddyExecutablePath, caddyfilePath);
        }

        public void Stop() => _supervisor.Stop();

        public ReverseProxyResult ApplyConfiguration(IEnumerable<WebsiteConfig> websites)
        {
            var settings = _settingsRepository.Get();
            var caddyfilePath = GetCaddyfilePath(settings);

            Directory.CreateDirectory(Path.GetDirectoryName(caddyfilePath) ?? string.Empty);
            File.WriteAllText(caddyfilePath, CaddyfileBuilder.Build(websites));

            var validation = RunCaddyCommand(settings.CaddyExecutablePath, $"validate --config \"{caddyfilePath}\" --adapter caddyfile");
            if (!validation.Success)
            {
                _appLogger.Error($"Caddyfile validation failed: {validation.Output}");
                return ReverseProxyResult.Fail($"Caddyfile validation failed:{Environment.NewLine}{validation.Output}");
            }

            if (!_supervisor.IsRunning)
            {
                _supervisor.Start(settings.CaddyExecutablePath, caddyfilePath);
                return ReverseProxyResult.Ok("Caddy started with the new configuration.");
            }

            var reload = RunCaddyCommand(settings.CaddyExecutablePath, $"reload --config \"{caddyfilePath}\" --adapter caddyfile");
            if (!reload.Success)
            {
                _appLogger.Error($"Caddy reload failed: {reload.Output}");
                return ReverseProxyResult.Fail($"Caddy reload failed:{Environment.NewLine}{reload.Output}");
            }

            return ReverseProxyResult.Ok("Reverse proxy configuration reloaded.");
        }

        private static string GetCaddyfilePath(AppSettings settings)
        {
            var folder = string.IsNullOrWhiteSpace(settings.CaddyConfigFolder)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReverseProxy")
                : settings.CaddyConfigFolder;
            return Path.Combine(folder, "Caddyfile");
        }

        private (bool Success, string Output) RunCaddyCommand(string caddyExecutablePath, string arguments)
        {
            if (string.IsNullOrWhiteSpace(caddyExecutablePath) || !File.Exists(caddyExecutablePath))
            {
                return (false, "Caddy executable not found. Configure the path to caddy.exe under Settings.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = caddyExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var output = new StringBuilder();
                output.Append(process.StandardOutput.ReadToEnd());
                output.Append(process.StandardError.ReadToEnd());
                process.WaitForExit(15000);

                return (process.ExitCode == 0, output.ToString().Trim());
            }
        }
    }
}
