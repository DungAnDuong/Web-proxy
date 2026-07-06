using System.Drawing;
using System.Windows.Forms;

namespace WebServerManagement.UI.Theming
{
    /// <summary>
    /// Recursive dark-mode styling applied at startup when <c>AppSettings.DarkMode</c> is set.
    /// WinForms has no built-in dark theme, so colors are applied explicitly to each control type
    /// -- a <see cref="ToolStripRenderer"/> alone is not enough to cover DataGridView, which needs
    /// its own cell/header style overrides.
    /// </summary>
    public static class DarkTheme
    {
        public static readonly Color Background = Color.FromArgb(30, 30, 30);
        public static readonly Color PanelBackground = Color.FromArgb(37, 37, 38);
        public static readonly Color ControlBackground = Color.FromArgb(45, 45, 48);
        public static readonly Color Border = Color.FromArgb(63, 63, 70);
        public static readonly Color Text = Color.FromArgb(241, 241, 241);
        public static readonly Color MutedText = Color.FromArgb(153, 153, 153);
        public static readonly Color Accent = Color.FromArgb(0, 122, 204);
        public static readonly Color GridAlternate = Color.FromArgb(40, 40, 42);
        public static readonly Color SelectionBackground = Color.FromArgb(9, 71, 113);

        public static readonly ToolStripRenderer ToolStripRenderer = new ToolStripProfessionalRenderer(new DarkColorTable());

        public static void Apply(Control root)
        {
            root.BackColor = Background;
            root.ForeColor = Text;
            ApplyRecursive(root);
        }

        private static void ApplyRecursive(Control control)
        {
            switch (control)
            {
                case DataGridView grid:
                    StyleDataGridView(grid);
                    break;
                case ToolStrip toolStrip:
                    // Covers ToolStrip itself plus its subclasses (StatusStrip, MenuStrip, ...).
                    toolStrip.Renderer = ToolStripRenderer;
                    toolStrip.BackColor = PanelBackground;
                    toolStrip.ForeColor = Text;
                    break;
                case TextBox textBox:
                    textBox.BackColor = ControlBackground;
                    textBox.ForeColor = Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = ControlBackground;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;
                case Button button:
                    button.BackColor = ControlBackground;
                    button.ForeColor = Text;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Border;
                    break;
                case Panel panel:
                    panel.BackColor = Background;
                    panel.ForeColor = Text;
                    break;
                case GroupBox groupBox:
                    groupBox.ForeColor = Text;
                    break;
                case CheckBox checkBox:
                    checkBox.ForeColor = Text;
                    break;
                case Label label:
                    label.ForeColor = Text;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyRecursive(child);
            }
        }

        private static void StyleDataGridView(DataGridView grid)
        {
            grid.BackgroundColor = Background;
            grid.GridColor = Border;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = PanelBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PanelBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;

            grid.DefaultCellStyle.BackColor = Background;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = SelectionBackground;
            grid.DefaultCellStyle.SelectionForeColor = Text;

            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionBackground;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Text;

            grid.RowHeadersDefaultCellStyle.BackColor = PanelBackground;
            grid.RowHeadersDefaultCellStyle.ForeColor = Text;
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => PanelBackground;
            public override Color ToolStripGradientMiddle => PanelBackground;
            public override Color ToolStripGradientEnd => PanelBackground;
            public override Color MenuStripGradientBegin => PanelBackground;
            public override Color MenuStripGradientEnd => PanelBackground;
            public override Color ImageMarginGradientBegin => ControlBackground;
            public override Color ImageMarginGradientMiddle => ControlBackground;
            public override Color ImageMarginGradientEnd => ControlBackground;
            public override Color MenuItemSelected => Accent;
            public override Color MenuItemSelectedGradientBegin => Accent;
            public override Color MenuItemSelectedGradientEnd => Accent;
            public override Color MenuItemBorder => Accent;
            public override Color ButtonSelectedHighlight => Accent;
            public override Color ButtonSelectedBorder => Accent;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Border;
            public override Color StatusStripGradientBegin => PanelBackground;
            public override Color StatusStripGradientEnd => PanelBackground;
        }
    }
}
