using System;
using System.Collections.Generic;
using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Interfaces
{
    /// <summary>
    /// Persistence port for <see cref="WebsiteConfig"/> records. Implemented against LiteDB in the
    /// Infrastructure layer -- Core never depends on a concrete storage technology.
    /// </summary>
    public interface IWebsiteRepository
    {
        IReadOnlyList<WebsiteConfig> GetAll();

        WebsiteConfig GetById(Guid id);

        void Add(WebsiteConfig website);

        void Update(WebsiteConfig website);

        void Delete(Guid id);
    }
}
