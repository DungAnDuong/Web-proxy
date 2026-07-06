using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Logging
{
    /// <summary>
    /// Per-website log writer: Logs\{WebsiteName}\{yyyy-MM-dd}.log. One lock object per website
    /// name (not a single global lock) so high-traffic console output from one site never blocks
    /// writes for another.
    /// </summary>
    public class FileSiteLogger : ISiteLogger
    {
        private readonly string _logsRootFolder;
        private readonly ConcurrentDictionary<string, object> _locksByWebsite = new ConcurrentDictionary<string, object>();

        public FileSiteLogger(string logsRootFolder)
        {
            _logsRootFolder = logsRootFolder ?? throw new ArgumentNullException(nameof(logsRootFolder));
        }

        public void Log(string websiteName, SiteLogCategory category, string message)
        {
            if (string.IsNullOrWhiteSpace(websiteName)) throw new ArgumentException("Website name is required.", nameof(websiteName));

            var safeName = MakeSafeFolderName(websiteName);
            var folder = Path.Combine(_logsRootFolder, safeName);
            var lockObj = _locksByWebsite.GetOrAdd(safeName, _ => new object());

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";
            var path = Path.Combine(folder, $"{DateTime.Now:yyyy-MM-dd}.log");

            lock (lockObj)
            {
                Directory.CreateDirectory(folder);
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private static string MakeSafeFolderName(string name)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }
            return name;
        }
    }
}
