namespace CloudFlowExplorer
{
    /// <summary>
    /// Modal dialog that lets the user pick a single Dataverse solution from a list.
    /// </summary>
    public class SolutionPickerForm : Form
    {
        private readonly TextBox _txtSearch;
        private readonly DataGridView _listSolutions;
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

            _listSolutions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            FluentStyle.StyleGrid(_listSolutions);
            _listSolutions.Columns.Add("DisplayName", "Display Name");
            _listSolutions.Columns.Add("UniqueName", "Unique Name");
            _listSolutions.Columns.Add("Version", "Version");
            _listSolutions.Columns.Add("Managed", "Managed");
            _listSolutions.Columns[0].FillWeight = 40;
            _listSolutions.Columns[1].FillWeight = 30;
            _listSolutions.Columns[2].FillWeight = 18;
            _listSolutions.Columns[3].FillWeight = 12;
            _listSolutions.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AcceptSelection(); };
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

            _listSolutions.Rows.Clear();

            var matches = string.IsNullOrEmpty(keyword)
                ? _allSolutions
                : _allSolutions.Where(s =>
                    (s.FriendlyName ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (s.UniqueName ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var solution in matches)
            {
                var index = _listSolutions.Rows.Add(solution.FriendlyName, solution.UniqueName, solution.Version, solution.IsManaged ? "Yes" : "No");
                _listSolutions.Rows[index].Tag = solution;
            }

            if (_listSolutions.Rows.Count > 0)
                _listSolutions.Rows[0].Selected = true;
        }
        private void AcceptSelection()
        {
            if (_listSolutions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a solution.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var solution = (SolutionRecord)_listSolutions.SelectedRows[0].Tag!;
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
