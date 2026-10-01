namespace CloudFlowExplorer
{
    /// <summary>
    /// Modal dialog that lets the user pick a single Dataverse solution from a list.
    /// </summary>
    public class SolutionPickerForm : Form
    {
        private readonly ListView _listSolutions;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

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

            foreach (var solution in solutions)
            {
                var item = new ListViewItem(solution.FriendlyName)
                {
                    Tag = solution
                };
                item.SubItems.Add(solution.UniqueName);
                item.SubItems.Add(solution.Version);
                item.SubItems.Add(solution.IsManaged ? "Yes" : "No");
                _listSolutions.Items.Add(item);
            }

            if (_listSolutions.Items.Count > 0)
                _listSolutions.Items[0].Selected = true;

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

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
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
