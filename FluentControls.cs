using System.Drawing.Drawing2D;

namespace CloudFlowExplorer
{
    /// <summary>
    /// Flat, borderless command-bar button: accent glyph followed by a label, grey on hover/press.
    /// </summary>
    internal sealed class CommandButton : Control
    {
        private const int HorizontalPadding = 10;
        private const int GlyphGap = 6;

        private bool _hover;
        private bool _down;
        private string _glyph = string.Empty;
        private Color _glyphColor = FluentStyle.Accent;

        public CommandButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
            Height = 40;
            Margin = Padding.Empty;
            Cursor = Cursors.Hand;
            Font = FluentStyle.Regular;
        }

        public string Glyph
        {
            get => _glyph;
            set { _glyph = value ?? string.Empty; RecalculateWidth(); Invalidate(); }
        }

        public Color GlyphColor
        {
            get => _glyphColor;
            set { _glyphColor = value; Invalidate(); }
        }

        public override string Text
        {
            get => base.Text;
            set { base.Text = value; RecalculateWidth(); Invalidate(); }
        }

        private void RecalculateWidth()
        {
            int glyphWidth = _glyph.Length == 0 ? 0
                : TextRenderer.MeasureText(_glyph, FluentStyle.IconFont, Size.Empty, TextFormatFlags.NoPadding).Width + GlyphGap;
            int textWidth = TextRenderer.MeasureText(Text ?? string.Empty, FluentStyle.Regular, Size.Empty, TextFormatFlags.NoPadding).Width;
            Width = HorizontalPadding * 2 + glyphWidth + textWidth;
        }

        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; base.OnMouseLeave(e); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; base.OnMouseDown(e); Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; base.OnMouseUp(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color back = !Enabled ? FluentStyle.Surface : _down ? FluentStyle.Pressed : _hover ? FluentStyle.Hover : FluentStyle.Surface;
            e.Graphics.Clear(back);

            int x = HorizontalPadding;
            if (_glyph.Length > 0)
            {
                var size = TextRenderer.MeasureText(_glyph, FluentStyle.IconFont, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, _glyph, FluentStyle.IconFont, new Rectangle(x, 0, size.Width, Height),
                    Enabled ? _glyphColor : FluentStyle.TextDisabled, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += size.Width + GlyphGap;
            }

            TextRenderer.DrawText(e.Graphics, Text, FluentStyle.Regular, new Rectangle(x, 0, Width - x, Height),
                Enabled ? FluentStyle.TextPrimary : FluentStyle.TextDisabled, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>
    /// Collapsible left navigation rail with selectable items (radio-like) and action items.
    /// </summary>
    internal sealed class NavRail : Control
    {
        public const int ExpandedWidth = 210;
        public const int CollapsedWidth = 48;

        private const int ToggleHeight = 40;
        private const int ItemHeight = 36;
        private const int HeadingHeight = 30;

        private enum RowKind { Heading, Choice, Action }

        private sealed class Row
        {
            public string Id = string.Empty;
            public string Glyph = string.Empty;
            public string Title = string.Empty;
            public RowKind Kind;
            public bool Bottom;
        }

        private static readonly Font HeadingFont = new("Segoe UI", 8f, FontStyle.Bold);

        private readonly List<Row> _rows = new();
        private readonly ToolTip _toolTip = new();
        private string? _selectedId;
        private Row? _hoverRow;
        private bool _toggleHover;
        private bool _expanded = true;

        public NavRail()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
            Dock = DockStyle.Left;
            Width = ExpandedWidth;
            Cursor = Cursors.Hand;
            Font = FluentStyle.Regular;
        }

        public event EventHandler<string>? ItemSelected;
        public event EventHandler<string>? ActionInvoked;

        public string? SelectedId => _selectedId;

        public bool Expanded
        {
            get => _expanded;
            set { _expanded = value; Width = value ? ExpandedWidth : CollapsedWidth; Invalidate(); }
        }

        public void AddHeading(string title) => _rows.Add(new Row { Kind = RowKind.Heading, Title = title });

        public void AddChoice(string id, string glyph, string title) =>
            _rows.Add(new Row { Kind = RowKind.Choice, Id = id, Glyph = glyph, Title = title });

        public void AddBottomAction(string id, string glyph, string title) =>
            _rows.Add(new Row { Kind = RowKind.Action, Id = id, Glyph = glyph, Title = title, Bottom = true });

        public void Select(string id)
        {
            if (_selectedId == id) return;
            _selectedId = id;
            Invalidate();
            ItemSelected?.Invoke(this, id);
        }

        private int HeightOf(Row row) => row.Kind == RowKind.Heading ? (_expanded ? HeadingHeight : 17) : ItemHeight;

        private IEnumerable<(Row Row, Rectangle Bounds)> Layout()
        {
            int y = ToggleHeight;
            foreach (var row in _rows.Where(r => !r.Bottom))
            {
                int h = HeightOf(row);
                yield return (row, new Rectangle(0, y, Width - 1, h));
                y += h;
            }

            int bottomTotal = _rows.Where(r => r.Bottom).Sum(HeightOf);
            y = Height - bottomTotal;
            foreach (var row in _rows.Where(r => r.Bottom))
            {
                int h = HeightOf(row);
                yield return (row, new Rectangle(0, y, Width - 1, h));
                y += h;
            }
        }

        private Row? RowAt(Point p)
        {
            foreach (var (row, bounds) in Layout())
            {
                if (bounds.Contains(p)) return row;
            }
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool toggle = e.Y < ToggleHeight;
            var row = toggle ? null : RowAt(e.Location);
            if (row?.Kind == RowKind.Heading) row = null;

            if (row != _hoverRow || toggle != _toggleHover)
            {
                _hoverRow = row;
                _toggleHover = toggle;
                _toolTip.SetToolTip(this, !_expanded && row != null ? row.Title : string.Empty);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverRow = null;
            _toggleHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Y < ToggleHeight)
            {
                Expanded = !Expanded;
            }
            else
            {
                var row = RowAt(e.Location);
                if (row?.Kind == RowKind.Choice) Select(row.Id);
                else if (row?.Kind == RowKind.Action) ActionInvoked?.Invoke(this, row.Id);
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(FluentStyle.Surface);

            foreach (var (row, bounds) in Layout())
            {
                if (row.Kind == RowKind.Heading) DrawHeading(g, row, bounds);
                else DrawItem(g, row, bounds);
            }

            DrawToggle(g);
            using var pen = new Pen(FluentStyle.Border);
            g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }

        private void DrawToggle(Graphics g)
        {
            var rect = new Rectangle(0, 0, Width - 1, ToggleHeight);
            using (var brush = new SolidBrush(_toggleHover ? FluentStyle.Hover : FluentStyle.Surface))
            using (var pen = new Pen(FluentStyle.Border))
            {
                g.FillRectangle(brush, rect);
                g.DrawLine(pen, 0, ToggleHeight - 1, Width - 1, ToggleHeight - 1);
            }
            TextRenderer.DrawText(g, "\uE700", FluentStyle.IconFont, new Rectangle(0, 0, CollapsedWidth, ToggleHeight - 1),
                FluentStyle.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void DrawHeading(Graphics g, Row row, Rectangle bounds)
        {
            if (!_expanded)
            {
                using var pen = new Pen(FluentStyle.Border);
                g.DrawLine(pen, 10, bounds.Top + bounds.Height / 2, Width - 11, bounds.Top + bounds.Height / 2);
                return;
            }

            TextRenderer.DrawText(g, row.Title, HeadingFont, new Rectangle(14, bounds.Top + 8, Width - 28, bounds.Height - 8),
                FluentStyle.TextSecondary, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void DrawItem(Graphics g, Row row, Rectangle bounds)
        {
            bool selected = row.Kind == RowKind.Choice && row.Id == _selectedId;
            bool hover = row == _hoverRow;

            if (selected || hover)
            {
                using var brush = new SolidBrush(selected ? FluentStyle.AccentSelected : FluentStyle.Hover);
                g.FillRectangle(brush, bounds);
            }

            if (selected)
            {
                using var brush = new SolidBrush(FluentStyle.Accent);
                g.FillRectangle(brush, bounds.Left, bounds.Top + 6, 3, bounds.Height - 12);
            }

            Color glyphColor = row.Kind == RowKind.Action ? FluentStyle.Danger : FluentStyle.Accent;
            TextRenderer.DrawText(g, row.Glyph, FluentStyle.IconFont, new Rectangle(0, bounds.Top, CollapsedWidth, bounds.Height),
                glyphColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (_expanded)
            {
                TextRenderer.DrawText(g, row.Title, selected ? FluentStyle.Semibold : FluentStyle.Regular,
                    new Rectangle(CollapsedWidth, bounds.Top, Width - CollapsedWidth - 8, bounds.Height),
                    FluentStyle.TextPrimary, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
