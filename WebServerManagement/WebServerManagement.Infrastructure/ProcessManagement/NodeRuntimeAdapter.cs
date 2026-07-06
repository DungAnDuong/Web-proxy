using System;
using System.Diagnostics;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.ProcessManagement
{
    /// <summary>
    /// Builds the <see cref="ProcessStartInfo"/> for Node.js/npm/yarn/pm2-based sites (Next.js,
    /// Express, plain Node scripts, ...). On Windows, package-manager commands like "npm run
    /// start" or "pm2 start ecosystem.config.js" are ".cmd" shims that cannot be launched
    /// directly as an executable, so they are routed through cmd.exe; a direct "node server.js"
    /// (or an explicit path to node.exe) is launched as the executable itself.
    /// </summary>
    public class NodeRuntimeAdapter : IRuntimeAdapter
    {
        public RuntimeType RuntimeType => RuntimeType.Node;

        public ProcessStartInfo BuildStartInfo(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = string.IsNullOrWhiteSpace(website.WorkingDirectory)
                    ? website.SourceFolder
                    : website.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            var command = (website.Command ?? string.Empty).Trim();
            var firstToken = command.Split(new[] { ' ' }, 2)[0].ToLowerInvariant();

            if (RequiresShim(firstToken))
            {
                startInfo.FileName = ResolveComSpec();
                var fullCommand = string.IsNullOrWhiteSpace(website.Arguments) ? command : $"{command} {website.Arguments}";
                startInfo.Arguments = $"/d /s /c \"{fullCommand}\"";
            }
            else if (string.Equals(firstToken, "node", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = string.IsNullOrWhiteSpace(website.NodeExecutablePath)
                    ? "node.exe"
                    : website.NodeExecutablePath;

                var remainder = command.Length > 4 ? command.Substring(4).Trim() : string.Empty;
                startInfo.Arguments = string.IsNullOrWhiteSpace(website.Arguments) ? remainder : $"{remainder} {website.Arguments}";
            }
            else
            {
                // Explicit node executable path was supplied and the command is a script/args list
                // (e.g. Command = "server.js") -- launch node.exe directly against it.
                startInfo.FileName = string.IsNullOrWhiteSpace(website.NodeExecutablePath)
                    ? "node.exe"
                    : website.NodeExecutablePath;
                startInfo.Arguments = string.IsNullOrWhiteSpace(website.Arguments) ? command : $"{command} {website.Arguments}";
            }

            startInfo.EnvironmentVariables["NODE_ENV"] = website.Environment == EnvironmentMode.Production ? "production" : "development";
            startInfo.EnvironmentVariables["PORT"] = website.InternalPort.ToString();

            foreach (var kvp in website.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            return startInfo;
        }

        private static bool RequiresShim(string firstToken)
        {
            return firstToken == "npm" || firstToken == "npx" || firstToken == "yarn" || firstToken == "pnpm" || firstToken == "pm2";
        }

        private static string ResolveComSpec()
        {
            var comspec = System.Environment.GetEnvironmentVariable("ComSpec");
            return string.IsNullOrWhiteSpace(comspec) ? "cmd.exe" : comspec;
        }
    }
}
