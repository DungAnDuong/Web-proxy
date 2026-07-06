using System;
using System.IO;
using System.Windows.Forms;
using WebServerManagement.UI.Theming;

namespace WebServerManagement.UI.Forms
{
    /// <summary>Polling tail view of a single log file (a website's daily log, or the central app log).</summary>
    public class LogViewerForm : Form
    {
        private readonly string _logFilePath;
        private readonly TextBox _textBox;
        private readonly Timer _pollTimer;
        private long _lastReadLength;

        public LogViewerForm(string title, string logFilePath, bool darkMode)
        {
            _logFilePath = logFilePath;

            Text = title;
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            _textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            Controls.Add(_textBox);

            _pollTimer = new Timer { Interval = 1000 };
            _pollTimer.Tick += (s, e) => RefreshContent();

            Load += (s, e) =>
            {
                if (darkMode) DarkTheme.Apply(this);
                RefreshContent(fullReload: true);
                _pollTimer.Start();
            };
            FormClosed += (s, e) => _pollTimer.Stop();
        }

        private void RefreshContent(bool fullReload = false)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    _textBox.Text = "(no log entries yet)";
                    return;
                }

                using (var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fullReload || stream.Length < _lastReadLength)
                    {
                        _lastReadLength = 0;
                        _textBox.Clear();
                    }

                    stream.Seek(_lastReadLength, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream))
                    {
                        var appended = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(appended))
                        {
                            _textBox.AppendText(appended);
                        }
                    }
                    _lastReadLength = stream.Length;
                }
            }
            catch (IOException)
            {
                // File is being written to by another process at this exact instant -- retry on the next tick.
            }
        }
    }
}
