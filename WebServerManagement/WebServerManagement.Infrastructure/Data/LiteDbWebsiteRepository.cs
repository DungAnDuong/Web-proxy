using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Data
{
    /// <summary>LiteDB-backed <see cref="IWebsiteRepository"/> -- the source of truth for website configuration.</summary>
    public class LiteDbWebsiteRepository : IWebsiteRepository
    {
        private const string CollectionName = "websites";
        private readonly ILiteCollection<WebsiteConfig> _collection;

        public LiteDbWebsiteRepository(LiteDbContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _collection = context.Database.GetCollection<WebsiteConfig>(CollectionName);
            _collection.EnsureIndex(w => w.Id, true);
        }

        public IReadOnlyList<WebsiteConfig> GetAll() => _collection.FindAll().ToList();

        public WebsiteConfig GetById(Guid id) => _collection.FindById(id);

        public void Add(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));
            _collection.Insert(website);
        }

        public void Update(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));
            website.UpdatedAt = DateTime.Now;
            _collection.Update(website);
        }

        public void Delete(Guid id) => _collection.Delete(id);
    }
}
