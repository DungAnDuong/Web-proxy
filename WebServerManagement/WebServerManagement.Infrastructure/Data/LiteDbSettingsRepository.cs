using System;
using System.Linq;
using LiteDB;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Data
{
    /// <summary>
    /// LiteDB-backed <see cref="ISettingsRepository"/>. Settings are a single document identified
    /// by a fixed id, since there is exactly one <see cref="AppSettings"/> per installation.
    /// </summary>
    public class LiteDbSettingsRepository : ISettingsRepository
    {
        private const string CollectionName = "settings";
        private const int SingletonId = 1;

        private readonly ILiteCollection<SettingsDocument> _collection;

        public LiteDbSettingsRepository(LiteDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _collection = context.Database.GetCollection<SettingsDocument>(CollectionName);
        }

        public AppSettings Get()
        {
            var document = _collection.FindById(SingletonId);
            return document?.Settings ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _collection.Upsert(new SettingsDocument { Id = SingletonId, Settings = settings });
        }

        private class SettingsDocument
        {
            public int Id { get; set; }
            public AppSettings Settings { get; set; }
        }
    }
}
