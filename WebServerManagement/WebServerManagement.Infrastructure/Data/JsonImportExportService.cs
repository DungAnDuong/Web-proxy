using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Data
{
    /// <summary>
    /// Backs the Import/Export buttons: LiteDB remains the source of truth at runtime, but the
    /// full website list can be round-tripped through a human-readable config.json for backup,
    /// review, or moving between machines.
    /// </summary>
    public class JsonImportExportService
    {
        private readonly IWebsiteRepository _repository;

        public JsonImportExportService(IWebsiteRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void ExportToFile(string filePath)
        {
            var websites = _repository.GetAll();
            var json = JsonConvert.SerializeObject(websites, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Imports websites from a config.json. Existing entries (matched by Id) are updated;
        /// new ones are inserted. Ids that already exist in the file are preserved so re-importing
        /// the same export does not create duplicates.
        /// </summary>
        public int ImportFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var websites = JsonConvert.DeserializeObject<List<WebsiteConfig>>(json) ?? new List<WebsiteConfig>();
            var existingIds = new HashSet<Guid>(_repository.GetAll().Select(w => w.Id));

            foreach (var website in websites)
            {
                if (existingIds.Contains(website.Id))
                {
                    _repository.Update(website);
                }
                else
                {
                    _repository.Add(website);
                }
            }

            return websites.Count;
        }
    }
}
