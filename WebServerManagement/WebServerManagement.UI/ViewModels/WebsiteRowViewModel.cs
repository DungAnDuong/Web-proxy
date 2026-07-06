using System;
using System.ComponentModel;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;

namespace WebServerManagement.UI.ViewModels
{
    /// <summary>
    /// Flattens a <see cref="WebsiteConfig"/> plus its live <see cref="WebsiteRuntimeState"/> into
    /// a single grid-bindable row. Runtime fields raise <see cref="INotifyPropertyChanged"/> so the
    /// DataGridView repaints automatically as the main form's refresh timer updates them.
    /// </summary>
    public class WebsiteRowViewModel : INotifyPropertyChanged
    {
        private WebsiteStatus _status;
        private int _pid;
        private double _cpuPercent;
        private double _ramMb;
        private DateTime? _lastStartTime;
        private DateTime? _lastStopTime;
        private bool _enabled;

        public event PropertyChangedEventHandler PropertyChanged;

        public WebsiteConfig Config { get; private set; }

        public Guid Id => Config.Id;

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public string Name => Config.Name;

        public string Domain => Config.Domain;

        public int InternalPort => Config.InternalPort;

        public string SourceFolder => Config.SourceFolder;

        public string Command => Config.Command;

        public bool AutoStart => Config.AutoStart;

        public bool Ssl => Config.EnableSsl;

        public string Status
        {
            get => _status.ToString();
        }

        public WebsiteStatus StatusValue
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusValue));
            }
        }

        public int Pid
        {
            get => _pid;
            set => SetField(ref _pid, value);
        }

        public double CpuPercent
        {
            get => _cpuPercent;
            set => SetField(ref _cpuPercent, value);
        }

        public double RamMb
        {
            get => _ramMb;
            set => SetField(ref _ramMb, value);
        }

        public DateTime? LastStartTime
        {
            get => _lastStartTime;
            set => SetField(ref _lastStartTime, value);
        }

        public DateTime? LastStopTime
        {
            get => _lastStopTime;
            set => SetField(ref _lastStopTime, value);
        }

        public WebsiteRowViewModel(WebsiteConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _enabled = config.Enabled;
        }

        /// <summary>Re-points this row at an updated config (after editing) without losing runtime state.</summary>
        public void ReplaceConfig(WebsiteConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _enabled = config.Enabled;
            OnPropertyChanged(nameof(Enabled));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Domain));
            OnPropertyChanged(nameof(InternalPort));
            OnPropertyChanged(nameof(SourceFolder));
            OnPropertyChanged(nameof(Command));
            OnPropertyChanged(nameof(AutoStart));
            OnPropertyChanged(nameof(Ssl));
        }

        public void ApplyRuntimeState(WebsiteRuntimeState state)
        {
            if (state == null) return;
            StatusValue = state.Status;
            Pid = state.Pid;
            CpuPercent = state.CpuPercent;
            RamMb = state.RamMb;
            LastStartTime = state.StartTime;
            LastStopTime = state.StopTime;
        }

        private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
