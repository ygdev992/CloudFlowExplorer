namespace CloudFlowExplorer
{
    /// <summary>
    /// Shared Fluent-style palette and styling helpers for the plugin UI.
    /// </summary>
    internal static class FluentStyle
    {
        public static readonly Color Navy = Color.FromArgb(0, 32, 80);
        public static readonly Color Accent = Color.FromArgb(0, 120, 212);
        public static readonly Color AccentDark = Color.FromArgb(16, 110, 190);
        public static readonly Color AccentPressed = Color.FromArgb(0, 90, 158);
        public static readonly Color AccentSelected = Color.FromArgb(222, 236, 249);
        public static readonly Color PageBackground = Color.FromArgb(250, 249, 248);
        public static readonly Color Surface = Color.White;
        public static readonly Color Hover = Color.FromArgb(243, 242, 241);
        public static readonly Color Pressed = Color.FromArgb(237, 235, 233);
        public static readonly Color Border = Color.FromArgb(237, 235, 233);
        public static readonly Color BorderStrong = Color.FromArgb(138, 136, 134);
        public static readonly Color TextPrimary = Color.FromArgb(50, 49, 48);
        public static readonly Color TextSecondary = Color.FromArgb(96, 94, 92);
        public static readonly Color TextDisabled = Color.FromArgb(161, 159, 157);
        public static readonly Color Success = Color.FromArgb(16, 124, 16);
        public static readonly Color Danger = Color.FromArgb(164, 38, 44);

        public static readonly Font Regular = new("Segoe UI", 9f);
        public static readonly Font Semibold = new("Segoe UI Semibold", 9f);
        public static readonly Font Title = new("Segoe UI Semibold", 11f);
        public static readonly Font IconFont = new("Segoe MDL2 Assets", 10f);

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Font = primary ? Semibold : Regular;
            button.FlatAppearance.BorderSize = 1;

            if (primary)
            {
                button.BackColor = Accent;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Accent;
                button.FlatAppearance.MouseOverBackColor = AccentDark;
                button.FlatAppearance.MouseDownBackColor = AccentPressed;
            }
            else
            {
                button.BackColor = Surface;
                button.ForeColor = TextPrimary;
                button.FlatAppearance.BorderColor = BorderStrong;
                button.FlatAppearance.MouseOverBackColor = Hover;
                button.FlatAppearance.MouseDownBackColor = Pressed;
            }

            // Flat buttons do not dim themselves when disabled, so do it explicitly.
            button.EnabledChanged += (s, e) =>
            {
                if (button.Enabled) return;
                button.BackColor = Hover;
                button.ForeColor = TextDisabled;
                button.FlatAppearance.BorderColor = Border;
            };
            button.EnabledChanged += (s, e) =>
            {
                if (!button.Enabled) return;
                button.BackColor = primary ? Accent : Surface;
                button.ForeColor = primary ? Color.White : TextPrimary;
                button.FlatAppearance.BorderColor = primary ? Accent : BorderStrong;
            };
        }

        public static void StyleIconButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Font = Regular;
            button.BackColor = Surface;
            button.ForeColor = TextPrimary;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Hover;
            button.FlatAppearance.MouseDownBackColor = Pressed;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 32;

            var header = grid.ColumnHeadersDefaultCellStyle;
            header.BackColor = Surface;
            header.ForeColor = TextSecondary;
            header.Font = Semibold;
            header.Padding = new Padding(8, 0, 0, 0);
            header.SelectionBackColor = Surface;
            header.SelectionForeColor = TextSecondary;

            var cell = grid.DefaultCellStyle;
            cell.BackColor = Surface;
            cell.ForeColor = TextPrimary;
            cell.Font = Regular;
            cell.Padding = new Padding(8, 0, 0, 0);
            cell.SelectionBackColor = AccentSelected;
            cell.SelectionForeColor = TextPrimary;

            grid.AlternatingRowsDefaultCellStyle.BackColor = Surface;
        }

        /// <summary>
        /// Draws a 1px bottom border on a panel (used for the header/command bands).
        /// </summary>
        public static void AddBottomBorder(Control control)
        {
            control.Paint += (s, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, control.Height - 1, control.Width, control.Height - 1);
            };
        }

        /// <summary>
        /// Wraps a borderless editor in a rounded-free, 1px bordered surface that highlights on focus.
        /// </summary>
        public static Panel CreateBorderedHost()
        {
            var host = new BorderedPanel { BackColor = Surface };
            return host;
        }

        private sealed class BorderedPanel : Panel
        {
            private bool _focused;

            public BorderedPanel()
            {
                SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
                DoubleBuffered = true;
            }

            protected override void OnControlAdded(ControlEventArgs e)
            {
                base.OnControlAdded(e);
                HookFocus(e.Control);
            }

            private void HookFocus(Control? control)
            {
                if (control == null) return;
                control.Enter += (s, a) => SetFocused(true);
                control.Leave += (s, a) => SetFocused(false);
            }

            private void SetFocused(bool focused)
            {
                _focused = focused;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using var pen = new Pen(_focused ? Accent : BorderStrong);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                if (_focused)
                {
                    using var accentPen = new Pen(Accent, 2);
                    e.Graphics.DrawLine(accentPen, 0, Height - 1, Width, Height - 1);
                }
            }
        }
    }
}
