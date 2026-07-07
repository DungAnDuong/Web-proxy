using System;
using System.Drawing;
using System.Windows.Forms;

namespace WebServerManagement.UI.Theming
{
    /// <summary>
    /// Extracts the icon embedded in the exe itself (set via &lt;ApplicationIcon&gt; in the UI
    /// csproj) so every window and the tray icon reuse the exact same image Windows already shows
    /// for the desktop shortcut and taskbar entry, instead of the generic <see cref="SystemIcons.Application"/>.
    /// </summary>
    public static class ApplicationIconProvider
    {
        private static readonly Lazy<Icon> Instance =
            new Lazy<Icon>(() => Icon.ExtractAssociatedIcon(Application.ExecutablePath));

        public static Icon Icon => Instance.Value;
    }
}
