using System;
using System.IO;
using LiteDB;

namespace WebServerManagement.Infrastructure.Data
{
    /// <summary>
    /// Owns the single shared <see cref="LiteDatabase"/> connection for the application. LiteDB
    /// only supports one writer per file at a time, so every repository shares this one instance
    /// rather than opening the file independently.
    /// </summary>
    public class LiteDbContext : IDisposable
    {
        public LiteDatabase Database { get; }

        public LiteDbContext(string databaseFilePath)
        {
            if (string.IsNullOrWhiteSpace(databaseFilePath)) throw new ArgumentNullException(nameof(databaseFilePath));

            Directory.CreateDirectory(Path.GetDirectoryName(databaseFilePath) ?? string.Empty);
            Database = new LiteDatabase($"Filename={databaseFilePath};Connection=shared");
        }

        public void Dispose()
        {
            Database?.Dispose();
        }
    }
}
