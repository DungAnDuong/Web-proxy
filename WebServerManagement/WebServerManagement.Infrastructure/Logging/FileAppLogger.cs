using System;
using System.IO;
using System.Text;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Logging
{
    /// <summary>
    /// Central application log writer. Writes one file per day at Logs\app-yyyy-MM-dd.log,
    /// serializing concurrent writes with a lock (log volume is low enough that a simple lock
    /// does not become a bottleneck).
    /// </summary>
    public class FileAppLogger : IAppLogger
    {
        private readonly string _logsRootFolder;
        private readonly object _writeLock = new object();

        public FileAppLogger(string logsRootFolder)
        {
            _logsRootFolder = logsRootFolder ?? throw new ArgumentNullException(nameof(logsRootFolder));
            Directory.CreateDirectory(_logsRootFolder);
        }

        public void Info(string message) => Write("INFO", message);

        public void Warn(string message) => Write("WARN", message);

        public void Error(string message, Exception ex = null)
        {
            var full = ex == null ? message : $"{message} :: {ex}";
            Write("ERROR", full);
        }

        private void Write(string level, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            var path = Path.Combine(_logsRootFolder, $"app-{DateTime.Now:yyyy-MM-dd}.log");

            lock (_writeLock)
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
