using System;

namespace WebServerManagement.Core.Interfaces
{
    /// <summary>Central application log (not tied to any one website).</summary>
    public interface IAppLogger
    {
        void Info(string message);

        void Warn(string message);

        void Error(string message, Exception ex = null);
    }
}
