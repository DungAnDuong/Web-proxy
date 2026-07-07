using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Core.Validation;
using WebServerManagement.Infrastructure.Data;
using WebServerManagement.UI.Theming;
using WebServerManagement.UI.ViewModels;

namespace WebServerManagement.UI.Forms
{
    /// <summary>
    /// Main window: the website grid, toolbar/menu/tray actions, and the 1-second refresh timer
    /// that pulls fresh PID/CPU/RAM/status from the process manager without ever doing I/O on the
    /// UI thread directly (process control calls are dispatched to the thread pool).
    /// </summary>
    public class MainForm : Form
    {
        private readonly IWebsiteRepository _websiteRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IProcessManager _processManager;
        private readonly IReverseProxyManager _reverseProxyManager;
        private readonly IHealthCheckService _healthCheckService;
        private readonly WebsiteValidator _validator;
        private readonly IAppLogger _appLogger;
        private readonly JsonImportExportService _importExportService;
        private readonly IWindowsStartupManager _startupManager;
        private readonly string _logsRootFolder;

        private DataGridView _grid;
        private BindingList<WebsiteRowViewModel> _rows;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusProxyLabel;
        private ToolStripStatusLabel _statusCountLabel;
        private NotifyIcon _notifyIcon;
        private Timer _refreshTimer;
        private bool _isExiting;

        public MainForm(
            IWebsiteRepository websiteRepository,
            ISettingsRepository settingsRepository,
            IProcessManager processManager,
            IReverseProxyManager reverseProxyManager,
            IHealthCheckService healthCheckService,
            WebsiteValidator validator,
            IAppLogger appLogger,
            JsonImportExportService importExportService,
            IWindowsStartupManager startupManager,
            string logsRootFolder)
        {
            _websiteRepository = websiteRepository;
            _settingsRepository = settingsRepository;
            _processManager = processManager;
            _reverseProxyManager = reverseProxyManager;
            _healthCheckService = healthCheckService;
            _validator = validator;
            _appLogger = appLogger;
            _importExportService = importExportService;
            _startupManager = startupManager;
            _logsRootFolder = logsRootFolder;

            BuildLayout();

            _processManager.StatusChanged += ProcessManager_StatusChanged;
            _healthCheckService.HealthCheckFailed += HealthCheckService_HealthCheckFailed;

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
            }
        }

        private void BuildLayout()
        {
            Text = "Web Server Manager";
            Width = 1400;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = ApplicationIconProvider.Icon;

            var menuStrip = BuildMenuStrip();
            var toolStrip = BuildToolStrip();
            _grid = BuildGrid();
            _statusStrip = BuildStatusStrip();

            Controls.Add(_grid);
            Controls.Add(toolStrip);
            Controls.Add(_statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            _grid.ContextMenuStrip = BuildContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = ApplicationIconProvider.Icon,
                Text = "Web Server Manager",
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };
            _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();

            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) => RefreshRuntimeColumns();
        }

        private MenuStrip BuildMenuStrip()
        {
            var menu = new MenuStrip { ImageScalingSize = new Size(18, 18) };

            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("Import...", IconFactory.Get(AppIcon.Import), (s, e) => ImportConfig());
            fileMenu.DropDownItems.Add("Export...", IconFactory.Get(AppIcon.Export), (s, e) => ExportConfig());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", IconFactory.Get(AppIcon.Exit), (s, e) => { _isExiting = true; Close(); });

            var proxyMenu = new ToolStripMenuItem("&Reverse Proxy");
            proxyMenu.DropDownItems.Add("Reload Reverse Proxy", IconFactory.Get(AppIcon.Reload), (s, e) => ReloadReverseProxy());

            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("Settings...", IconFactory.Get(AppIcon.Settings), (s, e) => OpenSettings());

            menu.Items.Add(fileMenu);
            menu.Items.Add(proxyMenu);
            menu.Items.Add(toolsMenu);
            return menu;
        }

        private ToolStrip BuildToolStrip()
        {
            var toolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                ImageScalingSize = new Size(20, 20),
                Padding = new Padding(4, 2, 4, 2)
            };
            toolStrip.Items.Add(NewButton("Add Website", AppIcon.Add, (s, e) => AddWebsite()));
            toolStrip.Items.Add(NewButton("Edit", AppIcon.Edit, (s, e) => EditSelectedWebsite()));
            toolStrip.Items.Add(NewButton("Delete", AppIcon.Delete, (s, e) => DeleteSelectedWebsite()));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(NewButton("Start", AppIcon.Start, (s, e) => StartSelectedWebsite()));
            toolStrip.Items.Add(NewButton("Stop", AppIcon.Stop, (s, e) => StopSelectedWebsite()));
            toolStrip.Items.Add(NewButton("Pause", AppIcon.Pause, (s, e) => PauseSelectedWebsite()));
            toolStrip.Items.Add(NewButton("Restart", AppIcon.Restart, (s, e) => RestartSelectedWebsite()));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(NewButton("Open Folder", AppIcon.Folder, (s, e) => OpenSelectedFolder()));
            toolStrip.Items.Add(NewButton("Open Browser", AppIcon.Browser, (s, e) => OpenSelectedInBrowser()));
            toolStrip.Items.Add(NewButton("Open Log", AppIcon.Log, (s, e) => OpenSelectedLog()));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(NewButton("Reload Reverse Proxy", AppIcon.Reload, (s, e) => ReloadReverseProxy()));
            toolStrip.Items.Add(NewButton("Save Config", AppIcon.Save, (s, e) => SaveAllConfig()));
            toolStrip.Items.Add(NewButton("Import", AppIcon.Import, (s, e) => ImportConfig()));
            toolStrip.Items.Add(NewButton("Export", AppIcon.Export, (s, e) => ExportConfig()));
            return toolStrip;
        }

        private static ToolStripButton NewButton(string text, AppIcon icon, EventHandler onClick)
        {
            var button = new ToolStripButton(text, IconFactory.Get(icon))
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter
            };
            button.Click += onClick;
            return button;
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip { ImageScalingSize = new Size(18, 18) };
            menu.Items.Add("Start", IconFactory.Get(AppIcon.Start), (s, e) => StartSelectedWebsite());
            menu.Items.Add("Stop", IconFactory.Get(AppIcon.Stop), (s, e) => StopSelectedWebsite());
            menu.Items.Add("Pause", IconFactory.Get(AppIcon.Pause), (s, e) => PauseSelectedWebsite());
            menu.Items.Add("Restart", IconFactory.Get(AppIcon.Restart), (s, e) => RestartSelectedWebsite());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Edit", IconFactory.Get(AppIcon.Edit), (s, e) => EditSelectedWebsite());
            menu.Items.Add("Delete", IconFactory.Get(AppIcon.Delete), (s, e) => DeleteSelectedWebsite());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open Folder", IconFactory.Get(AppIcon.Folder), (s, e) => OpenSelectedFolder());
            menu.Items.Add("Open Browser", IconFactory.Get(AppIcon.Browser), (s, e) => OpenSelectedInBrowser());
            menu.Items.Add("Open Log", IconFactory.Get(AppIcon.Log), (s, e) => OpenSelectedLog());
            return menu;
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip { ImageScalingSize = new Size(18, 18) };
            menu.Items.Add("Open", IconFactory.Get(AppIcon.Restore), (s, e) => RestoreFromTray());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", IconFactory.Get(AppIcon.Exit), (s, e) => { _isExiting = true; Close(); });
            return menu;
        }

        private DataGridView BuildGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                ReadOnly = false
            };

            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Enabled), HeaderText = "Enable", Width = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Name), HeaderText = "Website Name", Width = 130, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Domain), HeaderText = "Domain", Width = 160, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.InternalPort), HeaderText = "Internal Port", Width = 90, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.SourceFolder), HeaderText = "Source Folder", Width = 180, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Command), HeaderText = "Start Command", Width = 140, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Status), HeaderText = "Status", Width = 80, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Pid), HeaderText = "PID", Width = 60, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.CpuPercent), HeaderText = "CPU %", Width = 65, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.RamMb), HeaderText = "RAM (MB)", Width = 75, ReadOnly = true });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.AutoStart), HeaderText = "Auto Start", Width = 70, ReadOnly = true });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.Ssl), HeaderText = "SSL", Width = 45, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.LastStartTime), HeaderText = "Last Start Time", Width = 130, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WebsiteRowViewModel.LastStopTime), HeaderText = "Last Stop Time", Width = 130, ReadOnly = true });
            grid.Columns.Add(new DataGridViewButtonColumn { HeaderText = "Log", Text = "View", UseColumnTextForButtonValue = true, Width = 60, Name = "colLog" });

            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellClick += Grid_CellClick;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelectedWebsite(); };

            return grid;
        }

        private StatusStrip BuildStatusStrip()
        {
            var strip = new StatusStrip();
            _statusProxyLabel = new ToolStripStatusLabel("Reverse Proxy: Unknown");
            _statusCountLabel = new ToolStripStatusLabel { Spring = true, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            strip.Items.Add(_statusProxyLabel);
            strip.Items.Add(_statusCountLabel);
            return strip;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var settings = _settingsRepository.Get();
            if (settings.DarkMode) DarkTheme.Apply(this);
            else LightTheme.Apply(this);

            LoadWebsitesFromRepository();

            if (settings.AutoStartReverseProxyOnLaunch)
            {
                if (string.IsNullOrWhiteSpace(settings.CaddyExecutablePath))
                {
                    _appLogger.Warn("Reverse proxy auto-start skipped: Caddy executable path is not configured yet (set it under Tools > Settings).");
                }
                else
                {
                    ReloadReverseProxy();
                }
            }

            if (settings.AutoStartWebsitesOnLaunch)
            {
                foreach (var row in _rows.Where(r => r.Config.AutoStart && r.Config.Enabled))
                {
                    StartWebsite(row.Config);
                }
            }

            _healthCheckService.Start();
            _refreshTimer.Start();
            UpdateStatusBar();
        }

        private void LoadWebsitesFromRepository()
        {
            var configs = _websiteRepository.GetAll();
            _rows = new BindingList<WebsiteRowViewModel>(configs.Select(c => new WebsiteRowViewModel(c)).ToList());

            foreach (var row in _rows)
            {
                row.ApplyRuntimeState(_processManager.GetState(row.Id));
                if (!string.IsNullOrWhiteSpace(row.Config.HealthCheckPath))
                {
                    _healthCheckService.Track(row.Config);
                }
            }

            var bindingSource = new BindingSource { DataSource = _rows };
            _grid.DataSource = bindingSource;
        }

        private void RefreshRuntimeColumns()
        {
            if (_rows == null) return;
            foreach (var row in _rows)
            {
                row.ApplyRuntimeState(_processManager.GetState(row.Id));
            }
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            _statusProxyLabel.Text = $"Reverse Proxy: {(_reverseProxyManager.IsRunning ? "Running" : "Stopped")}";
            if (_rows != null)
            {
                var running = _rows.Count(r => r.StatusValue == WebsiteStatus.Running);
                _statusCountLabel.Text = $"{_rows.Count} website(s), {running} running";
            }
        }

        private WebsiteRowViewModel GetSelectedRow()
        {
            if (_grid.CurrentRow?.DataBoundItem is WebsiteRowViewModel row) return row;
            return null;
        }

        private void AddWebsite()
        {
            using (var form = new AddEditWebsiteForm(_validator, darkMode: _settingsRepository.Get().DarkMode))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                _websiteRepository.Add(form.Result);
                _rows.Add(new WebsiteRowViewModel(form.Result));
                ReloadReverseProxy();
                UpdateStatusBar();
            }
        }

        private void EditSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row == null) return;

            using (var form = new AddEditWebsiteForm(_validator, row.Config, _settingsRepository.Get().DarkMode))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                _websiteRepository.Update(form.Result);
                row.ReplaceConfig(form.Result);
                ReloadReverseProxy();
            }
        }

        private void DeleteSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row == null) return;

            var confirm = MessageBox.Show(this, $"Delete website '{row.Name}'? This stops its process and removes its configuration.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            _processManager.Forget(row.Id);
            _healthCheckService.Untrack(row.Id);
            _websiteRepository.Delete(row.Id);
            _rows.Remove(row);
            ReloadReverseProxy();
            UpdateStatusBar();
        }

        private void StartSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row != null) StartWebsite(row.Config);
        }

        private void StartWebsite(WebsiteConfig config)
        {
            System.Threading.Tasks.Task.Run(() => _processManager.Start(config));
        }

        private void StopSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row == null) return;
            var id = row.Id;
            System.Threading.Tasks.Task.Run(() => _processManager.Stop(id));
        }

        private void PauseSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row == null) return;
            var id = row.Id;
            var state = _processManager.GetState(id);
            if (state.Status == WebsiteStatus.Paused)
            {
                System.Threading.Tasks.Task.Run(() => _processManager.Resume(id));
            }
            else
            {
                System.Threading.Tasks.Task.Run(() => _processManager.Pause(id));
            }
        }

        private void RestartSelectedWebsite()
        {
            var row = GetSelectedRow();
            if (row != null)
            {
                var config = row.Config;
                System.Threading.Tasks.Task.Run(() => _processManager.Restart(config));
            }
        }

        private void OpenSelectedFolder()
        {
            var row = GetSelectedRow();
            if (row == null || string.IsNullOrWhiteSpace(row.SourceFolder)) return;
            if (Directory.Exists(row.SourceFolder))
            {
                Process.Start("explorer.exe", $"\"{row.SourceFolder}\"");
            }
        }

        private void OpenSelectedInBrowser()
        {
            var row = GetSelectedRow();
            if (row == null) return;
            var url = $"https://{row.Domain}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void OpenSelectedLog()
        {
            var row = GetSelectedRow();
            if (row == null) return;
            OpenLogFor(row);
        }

        private void OpenLogFor(WebsiteRowViewModel row)
        {
            var safeName = row.Name;
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            var logPath = Path.Combine(_logsRootFolder, safeName, $"{DateTime.Now:yyyy-MM-dd}.log");
            var settings = _settingsRepository.Get();
            var viewer = new LogViewerForm($"Log - {row.Name}", logPath, settings.DarkMode);
            viewer.Show(this);
        }

        private void ReloadReverseProxy()
        {
            // ApplyConfiguration writes the Caddyfile, validates it, and starts/reloads Caddy as
            // needed on its own -- it never throws even when Caddy isn't configured yet (a normal
            // first-run state), so this must not be allowed to interrupt MainForm_Load either.
            try
            {
                var enabledConfigs = _rows?.Select(r => r.Config) ?? _websiteRepository.GetAll();
                var result = _reverseProxyManager.ApplyConfiguration(enabledConfigs);

                if (!result.Success)
                {
                    _appLogger.Warn($"Reverse proxy configuration was not applied: {result.Message}");
                    MessageBox.Show(this, result.Message, "Reverse Proxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _appLogger.Error("Unexpected error while reloading the reverse proxy.", ex);
                MessageBox.Show(this, ex.Message, "Reverse Proxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateStatusBar();
            }
        }

        private void SaveAllConfig()
        {
            if (_rows == null) return;
            foreach (var row in _rows)
            {
                _websiteRepository.Update(row.Config);
            }
            MessageBox.Show(this, "Configuration saved.", "Save Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportConfig()
        {
            using (var dialog = new OpenFileDialog { Filter = "JSON config (*.json)|*.json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                var count = _importExportService.ImportFromFile(dialog.FileName);
                LoadWebsitesFromRepository();
                ReloadReverseProxy();
                MessageBox.Show(this, $"Imported {count} website(s).", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportConfig()
        {
            using (var dialog = new SaveFileDialog { Filter = "JSON config (*.json)|*.json", FileName = "config.json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _importExportService.ExportToFile(dialog.FileName);
                MessageBox.Show(this, "Export complete.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm(_settingsRepository, _startupManager))
            {
                form.ShowDialog(this);
            }
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _rows == null || e.RowIndex >= _rows.Count) return;
            var row = _rows[e.RowIndex];
            var column = _grid.Columns[e.ColumnIndex];

            if (column.DataPropertyName == nameof(WebsiteRowViewModel.Enabled))
            {
                row.Config.Enabled = row.Enabled;
                _websiteRepository.Update(row.Config);
                ReloadReverseProxy();
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name == "colLog")
            {
                OpenLogFor(_rows[e.RowIndex]);
            }
        }

        private void ProcessManager_StatusChanged(object sender, WebsiteStatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ProcessManager_StatusChanged(sender, e)));
                return;
            }

            var row = _rows?.FirstOrDefault(r => r.Id == e.WebsiteId);
            row?.ApplyRuntimeState(e.State);
            UpdateStatusBar();
        }

        private void HealthCheckService_HealthCheckFailed(object sender, HealthCheckFailedEventArgs e)
        {
            _appLogger.Warn($"Website {e.WebsiteId} failed its health check: {e.Reason}");
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                _notifyIcon.ShowBalloonTip(1500, "Web Server Manager", "Still running in the background.", ToolTipIcon.Info);
                return;
            }

            _refreshTimer.Stop();
            _healthCheckService.Stop();
            _notifyIcon.Visible = false;
        }
    }
}
