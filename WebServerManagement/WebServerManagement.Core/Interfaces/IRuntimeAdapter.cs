using System.Diagnostics;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;

namespace WebServerManagement.Core.Interfaces
{
    /// <summary>
    /// Translates a <see cref="WebsiteConfig"/> into a launchable <see cref="ProcessStartInfo"/> for
    /// one specific runtime (Node today). Adding a new runtime means adding a new adapter and
    /// registering it in RuntimeAdapterFactory -- existing adapters are never modified (Open/Closed).
    /// </summary>
    public interface IRuntimeAdapter
    {
        RuntimeType RuntimeType { get; }

        ProcessStartInfo BuildStartInfo(WebsiteConfig website);
    }
}
