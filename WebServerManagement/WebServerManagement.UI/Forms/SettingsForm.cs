using System;
using System.Drawing;
using System.Windows.Forms;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.UI.Theming;

namespace WebServerManagement.UI.Forms
{
    public class SettingsForm : Form
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IWindowsStartupManager _startupManager;

        private TextBox _txtCaddyPath;
        private TextBox _txtCaddyConfigFolder;
        private CheckBox _chkRunAtStartup;
        private CheckBox _chkAutoStartWebsites;
        private CheckBox _chkAutoStartProxy;
        private CheckBox _chkDarkMode;
        private NumericUpDown _numLogRetention;

        public AppSettings ResultSettings { get; private set; }

        public SettingsForm(ISettingsRepository settingsRepository, IWindowsStartupManager startupManager)
        {
            _settingsRepository = settingsRepository;
            _startupManager = startupManager;

            BuildLayout();
            LoadValues();

            if (_settingsRepository.Get().DarkMode) DarkTheme.Apply(this);
            else LightTheme.Apply(this);
        }

        private void BuildLayout()
        {
            Text = "Settings";
            Width = 560;
            Height = 400;
            Icon = ApplicationIconProvider.Icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(12), AutoSize = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            int row = 0;

            layout.Controls.Add(new Label { Text = "Caddy Executable Path", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _txtCaddyPath = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_txtCaddyPath, 1, row);
            var btnBrowseCaddy = NewBrowseButton();
            btnBrowseCaddy.Click += (s, e) => BrowseForCaddyExe();
            layout.Controls.Add(btnBrowseCaddy, 2, row);
            row++;

            layout.Controls.Add(new Label { Text = "Caddy Config Folder", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _txtCaddyConfigFolder = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_txtCaddyConfigFolder, 1, row);
            var btnBrowseFolder = NewBrowseButton();
            btnBrowseFolder.Click += (s, e) => BrowseForConfigFolder();
            layout.Controls.Add(btnBrowseFolder, 2, row);
            row++;

            layout.Controls.Add(new Label { Text = "Log Retention (days)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            _numLogRetention = new NumericUpDown { Minimum = 1, Maximum = 3650, Width = 100 };
            layout.Controls.Add(_numLogRetention, 1, row);
            row++;

            _chkRunAtStartup = new CheckBox { Text = "Run at Windows Startup", AutoSize = true };
            layout.Controls.Add(_chkRunAtStartup, 1, row);
            row++;

            _chkAutoStartWebsites = new CheckBox { Text = "Auto Start Websites on Launch", AutoSize = true };
            layout.Controls.Add(_chkAutoStartWebsites, 1, row);
            row++;

            _chkAutoStartProxy = new CheckBox { Text = "Auto Start Reverse Proxy on Launch", AutoSize = true };
            layout.Controls.Add(_chkAutoStartProxy, 1, row);
            row++;

            _chkDarkMode = new CheckBox { Text = "Dark Mode", AutoSize = true };
            layout.Controls.Add(_chkDarkMode, 1, row);
            row++;

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 46 };
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Image = IconFactory.Get(AppIcon.Cancel),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight
            };
            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Width = 100,
                Image = IconFactory.Get(AppIcon.Save),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight
            };
            btnSave.Click += (s, e) => SaveAndClose();
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);

            Controls.Add(layout);
            Controls.Add(buttonPanel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadValues()
        {
            var settings = _settingsRepository.Get();
            _txtCaddyPath.Text = settings.CaddyExecutablePath;
            _txtCaddyConfigFolder.Text = settings.CaddyConfigFolder;
            _numLogRetention.Value = Math.Max(1, Math.Min(3650, settings.LogRetentionDays));
            _chkRunAtStartup.Checked = settings.RunAtWindowsStartup;
            _chkAutoStartWebsites.Checked = settings.AutoStartWebsitesOnLaunch;
            _chkAutoStartProxy.Checked = settings.AutoStartReverseProxyOnLaunch;
            _chkDarkMode.Checked = settings.DarkMode;
        }

        private static Button NewBrowseButton()
        {
            return new Button
            {
                Text = "Browse...",
                Dock = DockStyle.Fill,
                Image = IconFactory.Get(AppIcon.Folder),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(4, 0, 0, 0)
            };
        }

        private void BrowseForCaddyExe()
        {
            using (var dialog = new OpenFileDialog { Filter = "caddy.exe|caddy.exe|Executable files (*.exe)|*.exe|All files (*.*)|*.*" })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _txtCaddyPath.Text = dialog.FileName;
                }
            }
        }

        private void BrowseForConfigFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _txtCaddyConfigFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveAndClose()
        {
            var settings = new AppSettings
            {
                CaddyExecutablePath = _txtCaddyPath.Text.Trim(),
                CaddyConfigFolder = _txtCaddyConfigFolder.Text.Trim(),
                LogRetentionDays = (int)_numLogRetention.Value,
                RunAtWindowsStartup = _chkRunAtStartup.Checked,
                AutoStartWebsitesOnLaunch = _chkAutoStartWebsites.Checked,
                AutoStartReverseProxyOnLaunch = _chkAutoStartProxy.Checked,
                DarkMode = _chkDarkMode.Checked
            };

            _settingsRepository.Save(settings);

            if (settings.RunAtWindowsStartup && !_startupManager.IsRegistered())
            {
                _startupManager.Register();
            }
            else if (!settings.RunAtWindowsStartup && _startupManager.IsRegistered())
            {
                _startupManager.Unregister();
            }

            ResultSettings = settings;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
