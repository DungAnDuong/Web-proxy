using System.Linq;
using System.Net.NetworkInformation;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Networking
{
    /// <summary>Checks TCP port usage via the OS's active listener table.</summary>
    public class PortAvailabilityChecker : IPortAvailabilityChecker
    {
        public bool IsPortInUse(int port)
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            var listeners = ipProperties.GetActiveTcpListeners();
            if (listeners.Any(l => l.Port == port)) return true;

            var connections = ipProperties.GetActiveTcpConnections();
            return connections.Any(c => c.LocalEndPoint.Port == port);
        }
    }
}
