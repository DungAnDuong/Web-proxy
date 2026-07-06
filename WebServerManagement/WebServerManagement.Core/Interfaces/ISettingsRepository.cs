using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Interfaces
{
    /// <summary>Persistence port for the single global <see cref="AppSettings"/> document.</summary>
    public interface ISettingsRepository
    {
        AppSettings Get();

        void Save(AppSettings settings);
    }
}
