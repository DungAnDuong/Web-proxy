namespace WebServerManagement.Core.Enums
{
    /// <summary>
    /// Lifecycle status of a managed website process.
    /// </summary>
    public enum WebsiteStatus
    {
        Stopped,
        Starting,
        Running,
        Pausing,
        Paused,
        Stopping,
        Crashed,
        Error
    }
}
