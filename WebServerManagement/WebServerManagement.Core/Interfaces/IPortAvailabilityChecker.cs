namespace WebServerManagement.Core.Interfaces
{
    /// <summary>Checks whether a TCP port is already bound on the local host.</summary>
    public interface IPortAvailabilityChecker
    {
        bool IsPortInUse(int port);
    }
}
