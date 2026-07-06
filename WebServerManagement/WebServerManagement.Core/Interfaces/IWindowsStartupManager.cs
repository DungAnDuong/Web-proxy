namespace WebServerManagement.Core.Interfaces
{
    /// <summary>Registers/unregisters the application to launch when the current user logs in to Windows.</summary>
    public interface IWindowsStartupManager
    {
        bool IsRegistered();

        void Register();

        void Unregister();
    }
}
