namespace CloudFlowExplorer
{
    /// <summary>
    /// Modal dialog that lets the user pick a single Dataverse solution from a list.
    /// </summary>
    public class SolutionPickerForm : Form
    {
        private readonly TextBox _txtSearch;
        private readonly ListView _listSolutions;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;
        private readonly List<SolutionRecord> _allSolutions;

        public Guid SelectedSolutionId { get; private set; }
        public string SelectedSolutionName { get; private set; } = "";

        public SolutionPickerForm(IEnumerable<SolutionRecord> solutions)
        {
            Text = "Select a Solution";
            StartPosition = FormStartPosition.CenterParent;
            Width = 560;
            Height = 420;
            MinimumSize = new Size(420, 300);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowIcon = false;
            Font = FluentStyle.Regular;

            _allSolutions = solutions.ToList();

            var header = new System.Windows.Forms.Label
            {
                Text = "Select a solution",
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = FluentStyle.Navy,
                ForeColor = Color.White,
                Font = FluentStyle.Title,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = FluentStyle.PageBackground, Padding = new Padding(12, 10, 12, 10) };
            var searchHost = FluentStyle.CreateBorderedHost();
            searchHost.Dock = DockStyle.Fill;
            searchHost.Padding = new Padding(1);
            var iconSearch = new System.Windows.Forms.Label
            {
                Text = "\uE721",
                Font = FluentStyle.IconFont,
                ForeColor = FluentStyle.Accent,
                Dock = DockStyle.Left,
                Width = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = FluentStyle.Surface,
                ForeColor = FluentStyle.TextPrimary
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter(_txtSearch.Text);
            var textWrap = new Panel { Dock = DockStyle.Fill, BackColor = FluentStyle.Surface, Padding = new Padding(0, 6, 8, 0) };
            textWrap.Controls.Add(_txtSearch);
            searchHost.Controls.Add(textWrap);
            searchHost.Controls.Add(iconSearch);
            panelSearch.Controls.Add(searchHost);

            _listSolutions = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                BorderStyle = BorderStyle.None,
                BackColor = FluentStyle.Surface,
                ForeColor = FluentStyle.TextPrimary
            };
            _listSolutions.Columns.Add("Display Name", 230);
            _listSolutions.Columns.Add("Unique Name", 150);
            _listSolutions.Columns.Add("Version", 90);
            _listSolutions.Columns.Add("Managed", 70);
            _listSolutions.DoubleClick += (s, e) => { if (_listSolutions.SelectedItems.Count > 0) AcceptSelection(); };

            ApplyFilter("");

            var panelButtons = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = FluentStyle.Surface, Padding = new Padding(12, 10, 12, 10) };
            panelButtons.Paint += (s, e) =>
            {
                using var pen = new Pen(FluentStyle.Border);
                e.Graphics.DrawLine(pen, 0, 0, panelButtons.Width, 0);
            };

            _btnOk = new Button { Text = "Load Flows", DialogResult = DialogResult.OK, Width = 110, Dock = DockStyle.Right };
            _btnOk.Click += (s, e) => AcceptSelection();
            FluentStyle.StyleButton(_btnOk, primary: true);

            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Dock = DockStyle.Right };
            FluentStyle.StyleButton(_btnCancel, primary: false);

            panelButtons.Controls.Add(_btnCancel);
            panelButtons.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            panelButtons.Controls.Add(_btnOk);

            BackColor = FluentStyle.PageBackground;
            Controls.Add(_listSolutions);
            Controls.Add(panelButtons);
            Controls.Add(panelSearch);
            Controls.Add(header);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void ApplyFilter(string keyword)
        {
            keyword = keyword.Trim();

            _listSolutions.BeginUpdate();
            _listSolutions.Items.Clear();

            var matches = string.IsNullOrEmpty(keyword)
                ? _allSolutions
                : _allSolutions.Where(s =>
                    (s.FriendlyName ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (s.UniqueName ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var solution in matches)
            {
                var item = new ListViewItem(solution.FriendlyName) { Tag = solution };
                item.SubItems.Add(solution.UniqueName);
                item.SubItems.Add(solution.Version);
                item.SubItems.Add(solution.IsManaged ? "Yes" : "No");
                _listSolutions.Items.Add(item);
            }

            if (_listSolutions.Items.Count > 0)
                _listSolutions.Items[0].Selected = true;

            _listSolutions.EndUpdate();
        }

        private void AcceptSelection()
        {
            if (_listSolutions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a solution.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var solution = (SolutionRecord)_listSolutions.SelectedItems[0].Tag!;
            SelectedSolutionId = solution.SolutionId;
            SelectedSolutionName = solution.FriendlyName;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // --- DTO ---
    public class SolutionRecord
    {
        public Guid SolutionId { get; set; }
        public string FriendlyName { get; set; } = "";
        public string UniqueName { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsManaged { get; set; }
    }
}
