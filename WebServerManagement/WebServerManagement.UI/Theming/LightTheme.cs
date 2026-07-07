using System.Drawing;
using System.Windows.Forms;

namespace WebServerManagement.UI.Theming
{
    /// <summary>
    /// Recursive light-mode styling applied at startup when <c>AppSettings.DarkMode</c> is not set.
    /// Mirrors <see cref="DarkTheme"/>'s structure so both themes stay visually consistent -- flat
    /// buttons, a subtle bordered grid, and a professional (non-default-gray) ToolStrip renderer --
    /// instead of leaving "light mode" as whatever the raw WinForms defaults happen to look like.
    /// </summary>
    public static class LightTheme
    {
        public static readonly Color Background = Color.FromArgb(250, 250, 251);
        public static readonly Color PanelBackground = Color.FromArgb(255, 255, 255);
        public static readonly Color ControlBackground = Color.White;
        public static readonly Color Border = Color.FromArgb(216, 219, 224);
        public static readonly Color Text = Color.FromArgb(32, 33, 36);
        public static readonly Color MutedText = Color.FromArgb(108, 115, 128);
        public static readonly Color Accent = Color.FromArgb(0, 103, 192);
        public static readonly Color GridAlternate = Color.FromArgb(246, 247, 249);
        public static readonly Color SelectionBackground = Color.FromArgb(204, 228, 247);

        public static readonly ToolStripRenderer ToolStripRenderer = new ToolStripProfessionalRenderer(new LightColorTable());

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
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(237, 243, 250);
                    button.FlatAppearance.MouseDownBackColor = SelectionBackground;
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

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 243, 246);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 243, 246);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
            grid.ColumnHeadersHeight = grid.ColumnHeadersHeight + 6;

            grid.DefaultCellStyle.BackColor = PanelBackground;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = SelectionBackground;
            grid.DefaultCellStyle.SelectionForeColor = Text;

            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionBackground;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Text;

            grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 243, 246);
            grid.RowHeadersDefaultCellStyle.ForeColor = Text;
        }

        private class LightColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => PanelBackground;
            public override Color ToolStripGradientMiddle => PanelBackground;
            public override Color ToolStripGradientEnd => PanelBackground;
            public override Color MenuStripGradientBegin => PanelBackground;
            public override Color MenuStripGradientEnd => PanelBackground;
            public override Color ImageMarginGradientBegin => Color.FromArgb(248, 249, 250);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(248, 249, 250);
            public override Color ImageMarginGradientEnd => Color.FromArgb(248, 249, 250);
            public override Color MenuItemSelected => Color.FromArgb(229, 241, 251);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(229, 241, 251);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(229, 241, 251);
            public override Color MenuItemBorder => Accent;
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(204, 228, 247);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(204, 228, 247);
            public override Color ButtonSelectedHighlight => Color.FromArgb(229, 241, 251);
            public override Color ButtonSelectedBorder => Accent;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Color.White;
            public override Color StatusStripGradientBegin => PanelBackground;
            public override Color StatusStripGradientEnd => PanelBackground;
            public override Color RaftingContainerGradientBegin => PanelBackground;
            public override Color RaftingContainerGradientEnd => PanelBackground;
        }
    }
}
