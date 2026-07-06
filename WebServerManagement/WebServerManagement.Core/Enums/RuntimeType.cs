namespace WebServerManagement.Core.Enums
{
    /// <summary>
    /// Identifies which <see cref="Interfaces.IRuntimeAdapter"/> builds the process start information
    /// for a website. New runtimes (Python, PHP, ASP.NET Core, Go, ...) are added here and by
    /// registering a new adapter in RuntimeAdapterFactory -- existing adapters are never modified.
    /// </summary>
    public enum RuntimeType
    {
        Node
    }
}
