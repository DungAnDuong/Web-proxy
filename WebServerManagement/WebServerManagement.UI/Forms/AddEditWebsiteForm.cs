using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Validation;
using WebServerManagement.UI.Theming;

namespace WebServerManagement.UI.Forms
{
    /// <summary>Add/Edit dialog for a single website. Pass an existing config to edit it in place, or null to create a new one.</summary>
    public class AddEditWebsiteForm : Form
    {
        private readonly WebsiteValidator _validator;
        private readonly WebsiteConfig _original;

        private TextBox _txtName;
        private TextBox _txtSourceFolder;
        private TextBox _txtDomain;
        private NumericUpDown _numPort;
        private TextBox _txtNodeExecutable;
        private TextBox _txtWorkingDirectory;
        private TextBox _txtCommand;
        private TextBox _txtArguments;
        private ComboBox _cboEnvironment;
        private CheckBox _chkAutoStart;
        private CheckBox _chkEnabled;
        private CheckBox _chkEnableSsl;
        private TextBox _txtCertPath;
        private TextBox _txtKeyPath;
        private Button _btnBrowseCert;
        private Button _btnBrowseKey;
        private TextBox _txtDescription;

        public WebsiteConfig Result { get; private set; }

        public AddEditWebsiteForm(WebsiteValidator validator, WebsiteConfig existing = null, bool darkMode = false)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _original = existing;

            BuildLayout();
            if (existing != null) LoadValues(existing);
            else
            {
                _chkEnabled.Checked = true;
                _cboEnvironment.SelectedItem = EnvironmentMode.Production;
                _numPort.Value = Math.Max(_numPort.Minimum, Math.Min(_numPort.Maximum, (decimal)_validator.SuggestAvailablePort()));
                _txtCommand.Text = "npm";
                _txtArguments.Text = "start";

                var detectedNode = DetectNodeExecutable();
                if (!string.IsNullOrEmpty(detectedNode)) _txtNodeExecutable.Text = detectedNode;
            }

            if (darkMode) DarkTheme.Apply(this);
            else LightTheme.Apply(this);
        }

        private void BuildLayout()
        {
            Text = _original == null ? "Add Website" : "Edit Website";
            Width = 640;
            Height = 640;
            Icon = ApplicationIconProvider.Icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(12), AutoScroll = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            int row = 0;

            AddLabel(layout, "Website Name", row);
            _txtName = AddTextBox(layout, row, span: 2);
            row++;

            AddLabel(layout, "Source Folder", row);
            _txtSourceFolder = AddTextBox(layout, row);
            _txtSourceFolder.TextChanged += (s, e) => SyncWorkingDirectoryWithSource();
            var btnBrowseSource = NewBrowseButton();
            btnBrowseSource.Click += (s, e) => BrowseFolder(_txtSourceFolder);
            layout.Controls.Add(btnBrowseSource, 2, row);
            row++;

            AddLabel(layout, "Domain", row);
            _txtDomain = AddTextBox(layout, row, span: 2);
            row++;

            AddLabel(layout, "Internal Port", row);
            _numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 3000, Width = 100 };
            layout.Controls.Add(_numPort, 1, row);
            row++;

            AddLabel(layout, "Node Executable", row);
            _txtNodeExecutable = AddTextBox(layout, row);
            var btnBrowseNode = NewBrowseButton();
            btnBrowseNode.Click += (s, e) => BrowseFile(_txtNodeExecutable, "node.exe|node.exe|Executable files (*.exe)|*.exe");
            layout.Controls.Add(btnBrowseNode, 2, row);
            row++;

            AddLabel(layout, "Working Directory", row);
            _txtWorkingDirectory = AddTextBox(layout, row);
            var btnBrowseWorkDir = NewBrowseButton();
            btnBrowseWorkDir.Click += (s, e) => BrowseFolder(_txtWorkingDirectory);
            layout.Controls.Add(btnBrowseWorkDir, 2, row);
            row++;

            AddLabel(layout, "Command", row);
            _txtCommand = AddTextBox(layout, row, span: 2);
            row++;

            AddLabel(layout, "Arguments", row);
            _txtArguments = AddTextBox(layout, row, span: 2);
            row++;

            AddLabel(layout, "Environment", row);
            _cboEnvironment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
            _cboEnvironment.Items.AddRange(new object[] { EnvironmentMode.Development, EnvironmentMode.Production });
            layout.Controls.Add(_cboEnvironment, 1, row);
            row++;

            _chkAutoStart = new CheckBox { Text = "Auto Start", AutoSize = true };
            layout.Controls.Add(_chkAutoStart, 1, row);
            row++;

            _chkEnabled = new CheckBox { Text = "Enabled (included in reverse proxy)", AutoSize = true };
            layout.Controls.Add(_chkEnabled, 1, row);
            row++;

            _chkEnableSsl = new CheckBox { Text = "Enable SSL (custom certificate)", AutoSize = true };
            _chkEnableSsl.CheckedChanged += (s, e) => ToggleSslFields();
            layout.Controls.Add(_chkEnableSsl, 1, row);
            row++;

            AddLabel(layout, "Certificate Path", row);
            _txtCertPath = AddTextBox(layout, row);
            _btnBrowseCert = NewBrowseButton();
            _btnBrowseCert.Click += (s, e) => BrowseFile(_txtCertPath, "Certificate files (*.crt;*.pem;*.cer)|*.crt;*.pem;*.cer|All files (*.*)|*.*");
            layout.Controls.Add(_btnBrowseCert, 2, row);
            row++;

            AddLabel(layout, "Key Path", row);
            _txtKeyPath = AddTextBox(layout, row);
            _btnBrowseKey = NewBrowseButton();
            _btnBrowseKey.Click += (s, e) => BrowseFile(_txtKeyPath, "Key files (*.key;*.pem)|*.key;*.pem|All files (*.*)|*.*");
            layout.Controls.Add(_btnBrowseKey, 2, row);
            row++;

            AddLabel(layout, "Description", row);
            _txtDescription = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            layout.Controls.Add(_txtDescription, 1, row);
            layout.SetColumnSpan(_txtDescription, 2);
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
                Width = 100,
                Image = IconFactory.Get(AppIcon.Save),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight
            };
            btnSave.Click += (s, e) => Validate_AndSave();
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);

            Controls.Add(layout);
            Controls.Add(buttonPanel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            ToggleSslFields();
        }

        private void ToggleSslFields()
        {
            var enabled = _chkEnableSsl.Checked;
            _txtCertPath.Enabled = enabled;
            _txtKeyPath.Enabled = enabled;
            _btnBrowseCert.Enabled = enabled;
            _btnBrowseKey.Enabled = enabled;
        }

        private static void AddLabel(TableLayoutPanel layout, string text, int row)
        {
            layout.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) }, 0, row);
        }

        private static TextBox AddTextBox(TableLayoutPanel layout, int row, int span = 1)
        {
            var textBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(textBox, 1, row);
            if (span > 1) layout.SetColumnSpan(textBox, span);
            return textBox;
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

        private void SyncWorkingDirectoryWithSource()
        {
            if (string.IsNullOrWhiteSpace(_txtWorkingDirectory.Text))
            {
                _txtWorkingDirectory.Text = _txtSourceFolder.Text;
            }
        }

        private static string DetectNodeExecutable()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    var candidate = Path.Combine(dir.Trim(), "node.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException)
                {
                    // Malformed PATH segment (stray quotes/invalid chars) -- skip it.
                }
            }
            return null;
        }

        private void BrowseFolder(TextBox target)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseFile(TextBox target, string filter)
        {
            using (var dialog = new OpenFileDialog { Filter = filter })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    target.Text = dialog.FileName;
                }
            }
        }

        private void LoadValues(WebsiteConfig config)
        {
            _txtName.Text = config.Name;
            _txtSourceFolder.Text = config.SourceFolder;
            _txtDomain.Text = config.Domain;
            _numPort.Value = Math.Max(1, Math.Min(65535, config.InternalPort));
            _txtNodeExecutable.Text = config.NodeExecutablePath;
            _txtWorkingDirectory.Text = config.WorkingDirectory;
            _txtCommand.Text = config.Command;
            _txtArguments.Text = config.Arguments;
            _cboEnvironment.SelectedItem = config.Environment;
            _chkAutoStart.Checked = config.AutoStart;
            _chkEnabled.Checked = config.Enabled;
            _chkEnableSsl.Checked = config.EnableSsl;
            _txtCertPath.Text = config.CertPath;
            _txtKeyPath.Text = config.KeyPath;
            _txtDescription.Text = config.Description;
            ToggleSslFields();
        }

        private void Validate_AndSave()
        {
            var candidate = _original == null ? new WebsiteConfig() : Clone(_original);

            candidate.Name = _txtName.Text.Trim();
            candidate.SourceFolder = _txtSourceFolder.Text.Trim();
            candidate.Domain = _txtDomain.Text.Trim();
            candidate.InternalPort = (int)_numPort.Value;
            candidate.NodeExecutablePath = _txtNodeExecutable.Text.Trim();
            candidate.WorkingDirectory = _txtWorkingDirectory.Text.Trim();
            candidate.Command = _txtCommand.Text.Trim();
            candidate.Arguments = _txtArguments.Text.Trim();
            candidate.Environment = (EnvironmentMode)(_cboEnvironment.SelectedItem ?? EnvironmentMode.Production);
            candidate.AutoStart = _chkAutoStart.Checked;
            candidate.Enabled = _chkEnabled.Checked;
            candidate.EnableSsl = _chkEnableSsl.Checked;
            candidate.CertPath = _txtCertPath.Text.Trim();
            candidate.KeyPath = _txtKeyPath.Text.Trim();
            candidate.Description = _txtDescription.Text.Trim();

            var validation = _validator.Validate(candidate);
            if (!validation.IsValid)
            {
                MessageBox.Show(this, validation.ToString(), "Please fix the following", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = candidate;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static WebsiteConfig Clone(WebsiteConfig source)
        {
            return new WebsiteConfig
            {
                Id = source.Id,
                CreatedAt = source.CreatedAt,
                RuntimeType = source.RuntimeType,
                MaxRestartCount = source.MaxRestartCount,
                RestartWindowSeconds = source.RestartWindowSeconds,
                HealthCheckPath = source.HealthCheckPath,
                HealthCheckIntervalSeconds = source.HealthCheckIntervalSeconds,
                EnvironmentVariables = source.EnvironmentVariables
            };
        }
    }
}
