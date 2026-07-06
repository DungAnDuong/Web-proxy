using System;
using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Interfaces
{
    public class WebsiteOutputEventArgs : EventArgs
    {
        public Guid WebsiteId { get; }
        public string Line { get; }
        public bool IsError { get; }

        public WebsiteOutputEventArgs(Guid websiteId, string line, bool isError)
        {
            WebsiteId = websiteId;
            Line = line;
            IsError = isError;
        }
    }

    public class WebsiteStatusChangedEventArgs : EventArgs
    {
        public Guid WebsiteId { get; }
        public WebsiteRuntimeState State { get; }

        public WebsiteStatusChangedEventArgs(Guid websiteId, WebsiteRuntimeState state)
        {
            WebsiteId = websiteId;
            State = state;
        }
    }

    /// <summary>
    /// Owns the lifecycle of every managed website process: starting, stopping, killing,
    /// restarting, pausing/resuming, and reacting to unexpected exits according to each
    /// site's restart policy.
    /// </summary>
    public interface IProcessManager
    {
        event EventHandler<WebsiteOutputEventArgs> OutputReceived;

        event EventHandler<WebsiteStatusChangedEventArgs> StatusChanged;

        WebsiteRuntimeState GetState(Guid websiteId);

        bool IsRunning(Guid websiteId);

        void Start(WebsiteConfig website);

        void Stop(Guid websiteId);

        void Kill(Guid websiteId);

        void Restart(WebsiteConfig website);

        void Pause(Guid websiteId);

        void Resume(Guid websiteId);

        /// <summary>Stops tracking a website entirely (used when a website is deleted).</summary>
        void Forget(Guid websiteId);
    }
}
