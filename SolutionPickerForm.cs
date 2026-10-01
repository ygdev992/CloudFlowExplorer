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
            Font = new Font("Segoe UI", 9f);

            _allSolutions = solutions.ToList();

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(8, 6, 8, 4) };
            var lblSearch = new System.Windows.Forms.Label { Text = "Search:", AutoSize = true, Location = new Point(0, 8) };
            _txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(55, 4),
                Width = panelSearch.Width - 65
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter(_txtSearch.Text);
            panelSearch.Controls.AddRange(new Control[] { lblSearch, _txtSearch });
            panelSearch.Resize += (s, e) => { _txtSearch.Width = panelSearch.Width - 65; };

            _listSolutions = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false
            };
            _listSolutions.Columns.Add("Display Name", 230);
            _listSolutions.Columns.Add("Unique Name", 150);
            _listSolutions.Columns.Add("Version", 90);
            _listSolutions.Columns.Add("Managed", 70);
            _listSolutions.DoubleClick += (s, e) => { if (_listSolutions.SelectedItems.Count > 0) AcceptSelection(); };

            ApplyFilter("");

            var panelButtons = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            _btnOk = new Button
            {
                Text = "Load Flows",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Width = 110,
                Height = 28,
                Location = new Point(panelButtons.Width - 230, 8)
            };
            _btnOk.Click += (s, e) => AcceptSelection();

            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Width = 100,
                Height = 28,
                Location = new Point(panelButtons.Width - 110, 8)
            };

            panelButtons.Controls.Add(_btnOk);
            panelButtons.Controls.Add(_btnCancel);
            panelButtons.Resize += (s, e) =>
            {
                _btnOk.Location = new Point(panelButtons.Width - 230, 8);
                _btnCancel.Location = new Point(panelButtons.Width - 110, 8);
            };

            Controls.Add(_listSolutions);
            Controls.Add(panelButtons);
            Controls.Add(panelSearch);

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
