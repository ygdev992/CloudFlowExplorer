using ClosedXML.Excel;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using XrmToolBox.Extensibility;

namespace CloudFlowExplorer
{
    public partial class CloudFlowExplorerControl : PluginControlBase
    {
        private const string SearchPlaceholder = "Enter a keyword to search in the flow definition...";

        private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        private static readonly Color MatchColor = Color.FromArgb(255, 213, 128);
        private static readonly Color CurrentMatchColor = Color.FromArgb(255, 140, 0);

        // Controls
        private readonly Button _btnLoad;
        private readonly Button _btnLoadFromSolution;
        private readonly Button _btnExportExcel;
        private readonly TextBox _txtSearch;
        private readonly Button _btnSearch;
        private readonly CheckBox _chkExactMatch;
        private readonly DataGridView _gridFlows;
        private readonly RichTextBox _txtJsonDetail;
        private readonly SplitContainer _splitMain;
        private readonly System.Windows.Forms.Label _lblFlowCount;
        private readonly System.Windows.Forms.Label _lblConnectionDot;
        private readonly System.Windows.Forms.Label _lblConnectionPrefix;
        private readonly System.Windows.Forms.Label _lblConnectionValue;
        private readonly System.Windows.Forms.Label _lblOccurrenceCount;
        private readonly Button _btnPrevMatch;
        private readonly Button _btnNextMatch;

        // Data
        private List<FlowRecord> _allFlows = new();
        private BindingList<FlowRecord> _displayedFlows = new();
        private List<int> _jsonMatchPositions = new();
        private int _currentMatchIndex = -1;

        public CloudFlowExplorerControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);

            // --- Top panel: actions (left), connection status (right), search row below ---
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(10, 8, 10, 6) };

            // Row 1: action buttons on the left, connection status on the right.
            var rowActions = new Panel { Dock = DockStyle.Top, Height = 34 };

            var panelActionsLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnLoad = new Button { Text = "Load Cloud Flows", Width = 150, Height = 28, Enabled = false, Margin = new Padding(0, 0, 6, 0) };
            _btnLoad.Click += OnLoadClick;

            _btnLoadFromSolution = new Button { Text = "Load Flows from Solution", Width = 170, Height = 28, Enabled = false, Margin = new Padding(0, 0, 10, 0) };
            _btnLoadFromSolution.Click += OnLoadFromSolutionClick;

            _btnExportExcel = new Button { Text = "Export to Excel", Width = 130, Height = 28, Enabled = false };
            _btnExportExcel.Click += OnExportExcelClick;

            panelActionsLeft.Controls.AddRange(new Control[]
            {
                CreateIconLabel("\uE753", AccentColor), _btnLoad,
                CreateIconLabel("\uE8B7", AccentColor), _btnLoadFromSolution,
                CreateIconLabel("\uE74E", Color.FromArgb(33, 115, 70)), _btnExportExcel
            });

            var panelConnectionRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _lblConnectionDot = new System.Windows.Forms.Label
            {
                Text = "\u25CF",
                AutoSize = true,
                ForeColor = Color.Silver,
                Margin = new Padding(0, 6, 4, 0)
            };
            _lblConnectionPrefix = new System.Windows.Forms.Label
            {
                Text = "Not connected.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(0, 7, 4, 0)
            };
            _lblConnectionValue = new System.Windows.Forms.Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.Black,
                Margin = new Padding(0, 7, 0, 0)
            };
            panelConnectionRight.Controls.AddRange(new Control[] { _lblConnectionDot, _lblConnectionPrefix, _lblConnectionValue });

            rowActions.Controls.Add(panelConnectionRight);
            rowActions.Controls.Add(panelActionsLeft);

            // Row 2: search bar.
            var rowSearch = new Panel { Dock = DockStyle.Top, Height = 34 };

            var panelSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1
            };
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var iconSearch = CreateIconLabel("\uE721", AccentColor);
            var lblSearch = new System.Windows.Forms.Label { Text = "Search keyword:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 8, 0) };

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                Text = SearchPlaceholder,
                Margin = new Padding(0, 4, 10, 0)
            };
            _txtSearch.Enter += (s, e) => { if (_txtSearch.Text == SearchPlaceholder) { _txtSearch.Text = ""; _txtSearch.ForeColor = SystemColors.WindowText; } };
            _txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = SearchPlaceholder; _txtSearch.ForeColor = Color.Gray; } };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { OnSearchClick(s, e); e.SuppressKeyPress = true; } };

            _chkExactMatch = new CheckBox
            {
                Text = "Whole word",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 10, 0)
            };
            var toolTip = new ToolTip();
            toolTip.SetToolTip(_chkExactMatch, "Match the exact word only (e.g. \"EM\" will not match \"EMEA\").");
            _chkExactMatch.CheckedChanged += (s, e) => { if (_allFlows.Count > 0) OnSearchClick(s, EventArgs.Empty); };

            _btnSearch = new Button { Text = "Search", Width = 70, Height = 28, Margin = new Padding(0, 2, 10, 0) };
            _btnSearch.Click += OnSearchClick;

            _lblFlowCount = new System.Windows.Forms.Label { Text = "", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 0, 0) };

            // Icon + label are grouped in a nested panel so they occupy a single
            // TableLayoutPanel column/cell together.
            var searchLabelGroup = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            searchLabelGroup.Controls.AddRange(new Control[] { iconSearch, lblSearch });

            panelSearch.Controls.Add(searchLabelGroup, 0, 0);
            panelSearch.Controls.Add(_txtSearch, 2, 0);
            panelSearch.Controls.Add(_chkExactMatch, 3, 0);
            panelSearch.Controls.Add(_btnSearch, 4, 0);
            panelSearch.Controls.Add(_lblFlowCount, 5, 0);

            rowSearch.Controls.Add(panelSearch);

            panelTop.Controls.Add(rowSearch);
            panelTop.Controls.Add(rowActions);

            // --- Split container: grid + JSON viewer ---
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                Panel1MinSize = 150,
                Panel2MinSize = 100
            };

            _gridFlows = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(230, 230, 230)
            };
            _gridFlows.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 243, 243);
            _gridFlows.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            _gridFlows.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            _gridFlows.ColumnHeadersHeight = 32;
            _gridFlows.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 249, 249);
            _gridFlows.SelectionChanged += OnFlowSelectionChanged;
            _gridFlows.CellContentClick += OnGridCellContentClick;
            _gridFlows.CellFormatting += OnGridCellFormatting;
            _gridFlows.DataSource = _displayedFlows;

            // --- JSON viewer with an occurrence navigator above it ---
            var panelJsonHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(245, 245, 245) };

            var panelOccurrenceNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _btnPrevMatch = new Button { Text = "\u25C0", Width = 26, Height = 24, Enabled = false, Margin = new Padding(0, 3, 4, 0) };
            _btnPrevMatch.Click += (s, e) => MoveMatch(-1);

            _btnNextMatch = new Button { Text = "\u25B6", Width = 26, Height = 24, Enabled = false, Margin = new Padding(0, 3, 10, 0) };
            _btnNextMatch.Click += (s, e) => MoveMatch(1);

            _lblOccurrenceCount = new System.Windows.Forms.Label
            {
                Text = "No search term",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 8, 10, 0)
            };

            panelOccurrenceNav.Controls.AddRange(new Control[] { _lblOccurrenceCount, _btnPrevMatch, _btnNextMatch });
            panelJsonHeader.Controls.Add(panelOccurrenceNav);

            _txtJsonDetail = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false,
                Font = new Font("Cascadia Code", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(212, 212, 212)
            };

            _splitMain.Panel1.Controls.Add(_gridFlows);
            _splitMain.Panel2.Controls.Add(_txtJsonDetail);
            _splitMain.Panel2.Controls.Add(panelJsonHeader);

            // --- Assemble ---
            Controls.Add(_splitMain);
            Controls.Add(panelTop);
        }

        private static System.Windows.Forms.Label CreateIconLabel(string glyph, Color color, int size = 14)
        {
            return new System.Windows.Forms.Label
            {
                Text = glyph,
                Font = new Font("Segoe MDL2 Assets", size, FontStyle.Regular),
                AutoSize = false,
                Width = size + 12,
                Height = size + 12,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = color,
                Margin = new Padding(0, 2, 2, 0)
            };
        }

        /// <summary>
        /// Called by XrmToolBox whenever the active connection changes.
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            bool connected = Service != null;
            _btnLoad.Enabled = connected;
            _btnLoadFromSolution.Enabled = connected;
            _btnExportExcel.Enabled = connected && _displayedFlows.Count > 0;

            _lblConnectionDot.ForeColor = connected ? Color.FromArgb(16, 124, 16) : Color.Silver;
            _lblConnectionPrefix.Text = connected ? "Connected to:" : "Not connected.";
            _lblConnectionPrefix.ForeColor = connected ? Color.Black : Color.Gray;
            _lblConnectionValue.Text = connected
                ? ConnectionDetail?.OrganizationFriendlyName ?? ""
                : "Use the connection button in the toolbar above to connect.";
            _lblConnectionValue.Font = new Font(Font, connected ? FontStyle.Bold : FontStyle.Regular);
            _lblConnectionValue.ForeColor = connected ? Color.Black : Color.Gray;
        }

        private void OnLoadClick(object? sender, EventArgs e)
        {
            // Guard against re-entrancy: a double-click (or the user clicking again
            // before the first load finishes) must not start a second overlapping
            // background operation.
            if (!_btnLoad.Enabled) return;

            if (Service == null)
            {
                RaiseRequestConnectionEvent(new RequestConnectionEventArgs());
                return;
            }

            ExecuteMethod(LoadFlows);
        }

        private void LoadFlows()
        {
            _btnLoad.Enabled = false;
            var service = Service;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Cloud Flows...",
                Work = (worker, args) =>
                {
                    args.Result = LoadCloudFlows(service);
                },
                PostWorkCallBack = args =>
                {
                    _btnLoad.Enabled = Service != null;

                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _allFlows = (List<FlowRecord>)args.Result;
                    _displayedFlows = new BindingList<FlowRecord>(_allFlows);
                    _gridFlows.DataSource = _displayedFlows;
                    ConfigureGridColumns();
                    _lblFlowCount.Text = $"{_allFlows.Count} flows";
                    _btnExportExcel.Enabled = Service != null && _allFlows.Count > 0;
                }
            });
        }

        private void OnLoadFromSolutionClick(object? sender, EventArgs e)
        {
            // Guard against re-entrancy, same reasoning as OnLoadClick.
            if (!_btnLoadFromSolution.Enabled) return;

            if (Service == null)
            {
                RaiseRequestConnectionEvent(new RequestConnectionEventArgs());
                return;
            }

            ExecuteMethod(LoadSolutionsThenPrompt);
        }

        private void LoadSolutionsThenPrompt()
        {
            _btnLoad.Enabled = false;
            _btnLoadFromSolution.Enabled = false;
            var service = Service;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    args.Result = LoadSolutions(service);
                },
                PostWorkCallBack = args =>
                {
                    _btnLoad.Enabled = Service != null;
                    _btnLoadFromSolution.Enabled = Service != null;

                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var solutions = (List<SolutionRecord>)args.Result;
                    if (solutions.Count == 0)
                    {
                        MessageBox.Show("No solutions were found in this environment.", "No solutions",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    using var picker = new SolutionPickerForm(solutions);
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadFlowsForSolution(picker.SelectedSolutionId, picker.SelectedSolutionName);
                    }
                }
            });
        }

        private void LoadFlowsForSolution(Guid solutionId, string solutionName)
        {
            _btnLoad.Enabled = false;
            _btnLoadFromSolution.Enabled = false;
            var service = Service;

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Loading Cloud Flows from solution '{solutionName}'...",
                Work = (worker, args) =>
                {
                    args.Result = LoadCloudFlows(service, solutionId);
                },
                PostWorkCallBack = args =>
                {
                    _btnLoad.Enabled = Service != null;
                    _btnLoadFromSolution.Enabled = Service != null;

                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _allFlows = (List<FlowRecord>)args.Result;
                    _displayedFlows = new BindingList<FlowRecord>(_allFlows);
                    _gridFlows.DataSource = _displayedFlows;
                    ConfigureGridColumns();
                    _lblFlowCount.Text = $"{_allFlows.Count} flows (solution: {solutionName})";
                    _btnExportExcel.Enabled = Service != null && _allFlows.Count > 0;
                }
            });
        }

        private string GetSearchKeyword()
        {
            var text = _txtSearch.Text.Trim();
            return text == SearchPlaceholder ? "" : text;
        }

        private void OnSearchClick(object? sender, EventArgs e)
        {
            var keyword = GetSearchKeyword();
            if (_allFlows.Count == 0) return;

            bool exact = _chkExactMatch.Checked;
            List<FlowRecord> filtered;
            if (string.IsNullOrEmpty(keyword))
            {
                filtered = _allFlows;
            }
            else
            {
                filtered = _allFlows
                    .Where(f => HasMatch(f.JsonDefinition, keyword, exact)
                             || HasMatch(f.Name, keyword, exact)
                             || HasMatch(f.Description, keyword, exact))
                    .ToList();
            }

            _displayedFlows = new BindingList<FlowRecord>(filtered);
            _gridFlows.DataSource = _displayedFlows;
            ConfigureGridColumns();
            _lblFlowCount.Text = $"{filtered.Count}/{_allFlows.Count} flows";

            // The keyword (or match mode) may have changed, so refresh the occurrence
            // navigator for whichever flow is currently selected.
            RefreshJsonHighlighting();
        }

        private static bool HasMatch(string? text, string keyword, bool exactWord)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return false;

            if (exactWord)
                return Regex.IsMatch(text!, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);

            return text!.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<int> FindMatchPositions(string text, string keyword, bool exactWord)
        {
            var positions = new List<int>();
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return positions;

            if (exactWord)
            {
                var pattern = $@"\b{Regex.Escape(keyword)}\b";
                foreach (Match m in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
                    positions.Add(m.Index);
            }
            else
            {
                var idx = 0;
                while ((idx = text.IndexOf(keyword, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    positions.Add(idx);
                    idx += keyword.Length;
                }
            }

            return positions;
        }

        private void OnFlowSelectionChanged(object? sender, EventArgs e)
        {
            if (_gridFlows.CurrentRow?.DataBoundItem is FlowRecord flow)
            {
                if (!string.IsNullOrEmpty(flow.JsonDefinition))
                {
                    // The actual formatting call is isolated in its own method so that if the
                    // host process has a conflicting Newtonsoft.Json assembly loaded (seen in the
                    // wild as a MissingMethodException that bypasses try/catch when it happens
                    // inline, because the JIT fails to prepare the containing method), the
                    // failure surfaces as a normal catchable exception here instead of crashing
                    // the whole application.
                    try
                    {
                        _txtJsonDetail.Text = FormatJson(flow.JsonDefinition);
                    }
                    catch
                    {
                        _txtJsonDetail.Text = flow.JsonDefinition;
                    }
                }
                else
                {
                    _txtJsonDetail.Text = "(No JSON definition available)";
                }

                RefreshJsonHighlighting();
            }
        }

        // Kept in its own method (never inlined into the caller) so that a JIT-time failure to
        // resolve a Newtonsoft.Json method (e.g. a conflicting assembly version already loaded by
        // the XrmToolBox host or another plugin) throws a normal, catchable exception at the call
        // site instead of corrupting the caller's own try/catch handling.
        private static string FormatJson(string json)
        {
            var parsed = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsed, Formatting.Indented);
        }

        /// <summary>
        /// Recomputes every occurrence of the current keyword in the JSON viewer, highlights
        /// them all, and resets the occurrence navigator (count + current position) to the
        /// first match.
        /// </summary>
        private void RefreshJsonHighlighting()
        {
            var text = _txtJsonDetail.Text;

            // Reset any highlighting left over from a previous selection.
            _txtJsonDetail.SelectAll();
            _txtJsonDetail.SelectionBackColor = _txtJsonDetail.BackColor;
            _txtJsonDetail.SelectionColor = _txtJsonDetail.ForeColor;
            _txtJsonDetail.DeselectAll();

            var keyword = GetSearchKeyword();
            _jsonMatchPositions = FindMatchPositions(text ?? "", keyword, _chkExactMatch.Checked);
            _currentMatchIndex = _jsonMatchPositions.Count > 0 ? 0 : -1;

            for (int i = 0; i < _jsonMatchPositions.Count; i++)
            {
                ApplyMatchStyle(i, isCurrent: i == _currentMatchIndex);
            }

            if (_currentMatchIndex >= 0)
            {
                _txtJsonDetail.Select(_jsonMatchPositions[_currentMatchIndex], keyword.Length);
                _txtJsonDetail.ScrollToCaret();
            }

            _txtJsonDetail.DeselectAll();
            UpdateOccurrenceUi();
        }

        private void ApplyMatchStyle(int matchIndex, bool isCurrent)
        {
            var keyword = GetSearchKeyword();
            if (matchIndex < 0 || matchIndex >= _jsonMatchPositions.Count || string.IsNullOrEmpty(keyword)) return;

            _txtJsonDetail.Select(_jsonMatchPositions[matchIndex], keyword.Length);
            _txtJsonDetail.SelectionBackColor = isCurrent ? CurrentMatchColor : MatchColor;
            _txtJsonDetail.SelectionColor = Color.Black;
        }

        /// <summary>Moves the occurrence navigator to the previous (-1) or next (+1) match.</summary>
        private void MoveMatch(int delta)
        {
            if (_jsonMatchPositions.Count == 0) return;

            ApplyMatchStyle(_currentMatchIndex, isCurrent: false);
            _currentMatchIndex = (_currentMatchIndex + delta + _jsonMatchPositions.Count) % _jsonMatchPositions.Count;
            ApplyMatchStyle(_currentMatchIndex, isCurrent: true);

            var keyword = GetSearchKeyword();
            _txtJsonDetail.Select(_jsonMatchPositions[_currentMatchIndex], keyword.Length);
            _txtJsonDetail.ScrollToCaret();
            _txtJsonDetail.DeselectAll();

            UpdateOccurrenceUi();
        }

        private void UpdateOccurrenceUi()
        {
            var total = _jsonMatchPositions.Count;
            var hasKeyword = !string.IsNullOrEmpty(GetSearchKeyword());

            _btnPrevMatch.Enabled = total > 1;
            _btnNextMatch.Enabled = total > 1;
            _lblOccurrenceCount.Text = !hasKeyword
                ? "No search term"
                : total == 0
                    ? "0 occurrences"
                    : $"Occurrence {_currentMatchIndex + 1} of {total}";
        }

        private void ConfigureGridColumns()
        {
            if (_gridFlows.Columns.Count == 0) return;

            // Hide raw JSON column and ID in the grid
            if (_gridFlows.Columns["JsonDefinition"] is DataGridViewColumn colJson)
                colJson.Visible = false;
            if (_gridFlows.Columns["FlowId"] is DataGridViewColumn colId)
                colId.Visible = false;

            if (_gridFlows.Columns["Name"] is DataGridViewColumn colName)
            { colName.HeaderText = "Flow Name"; colName.FillWeight = 30; }
            if (_gridFlows.Columns["Description"] is DataGridViewColumn colDesc)
            { colDesc.HeaderText = "Description"; colDesc.FillWeight = 25; }
            if (_gridFlows.Columns["StatusDisplay"] is DataGridViewColumn colStatus)
            { colStatus.HeaderText = "Status"; colStatus.FillWeight = 8; }
            if (_gridFlows.Columns["Owner"] is DataGridViewColumn colOwner)
            { colOwner.HeaderText = "Owner"; colOwner.FillWeight = 15; }
            if (_gridFlows.Columns["CreatedOn"] is DataGridViewColumn colCreated)
            { colCreated.HeaderText = "Created On"; colCreated.FillWeight = 11; colCreated.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm"; }
            if (_gridFlows.Columns["ModifiedOn"] is DataGridViewColumn colModified)
            { colModified.HeaderText = "Modified On"; colModified.FillWeight = 11; colModified.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm"; }

            // Data-bound columns are regenerated every time DataSource is reassigned, so this
            // unbound link column needs to be re-added on every call rather than just once.
            if (_gridFlows.Columns["OpenFlow"] == null)
            {
                var colOpen = new DataGridViewLinkColumn
                {
                    Name = "OpenFlow",
                    HeaderText = "",
                    Text = "Open in Power Automate",
                    UseColumnTextForLinkValue = true,
                    FillWeight = 14,
                    LinkColor = AccentColor,
                    ActiveLinkColor = Color.FromArgb(0, 71, 171),
                    VisitedLinkColor = AccentColor
                };
                _gridFlows.Columns.Add(colOpen);
            }
        }

        private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_gridFlows.Columns[e.ColumnIndex].Name != "StatusDisplay") return;

            e.CellStyle.ForeColor = (e.Value as string) switch
            {
                "Activated" => Color.FromArgb(16, 124, 16),
                "Suspended" => Color.FromArgb(196, 43, 28),
                "Draft" => Color.DimGray,
                _ => _gridFlows.DefaultCellStyle.ForeColor
            };
            e.CellStyle.Font = new Font(_gridFlows.Font, FontStyle.Bold);
        }

        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_gridFlows.Columns[e.ColumnIndex].Name != "OpenFlow") return;

            if (_gridFlows.Rows[e.RowIndex].DataBoundItem is FlowRecord flow)
            {
                OpenFlowInBrowser(flow);
            }
        }

        private void OnExportExcelClick(object? sender, EventArgs e)
        {
            if (_displayedFlows.Count == 0)
            {
                MessageBox.Show("There is nothing to export yet. Load some Cloud Flows first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"CloudFlows_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ExportFlowsToExcel(_displayedFlows, sfd.FileName);
                MessageBox.Show("Export completed successfully.", "Export to Excel",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export to Excel: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ExportFlowsToExcel(IEnumerable<FlowRecord> flows, string path)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Cloud Flows");

            string[] headers = { "Flow Name", "Description", "Status", "Owner", "Created On", "Modified On" };
            for (int i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);

            int row = 2;
            foreach (var f in flows)
            {
                sheet.Cell(row, 1).Value = f.Name;
                sheet.Cell(row, 2).Value = f.Description;
                sheet.Cell(row, 3).Value = f.StatusDisplay;
                sheet.Cell(row, 4).Value = f.Owner;
                if (f.CreatedOn.HasValue) sheet.Cell(row, 5).Value = f.CreatedOn.Value;
                if (f.ModifiedOn.HasValue) sheet.Cell(row, 6).Value = f.ModifiedOn.Value;
                row++;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(path);
        }

        private void OpenFlowInBrowser(FlowRecord flow)
        {
            var detail = ConnectionDetail;
            var environmentId = detail?.EnvironmentId;
            if (detail == null || string.IsNullOrEmpty(environmentId))
            {
                MessageBox.Show("Unable to determine the Power Platform environment for the current connection.",
                    "Cannot open flow", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Opens the flow directly in the Power Automate designer (editable), instead of the
            // read-only Dynamics "workflow" table record previously reached through main.aspx.
            var url = $"https://make.powerautomate.com/environments/{environmentId}/flows/{flow.FlowId}/edit";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the flow in the browser: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Dataverse query ---
        private static List<FlowRecord> LoadCloudFlows(IOrganizationService service, Guid? solutionId = null)
        {
            var flows = new List<FlowRecord>();
            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet(
                    "workflowid", "name", "description", "category",
                    "statecode", "statuscode", "clientdata",
                    "createdon", "modifiedon", "ownerid"
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("category", ConditionOperator.Equal, 5) // Modern Flow
                    }
                },
                Orders = { new OrderExpression("name", OrderType.Ascending) },
                PageInfo = new PagingInfo { Count = 5000, PageNumber = 1, ReturnTotalRecordCount = true },
                // Joining to solutioncomponent can otherwise yield duplicate rows if a
                // flow is somehow listed more than once as a component of the solution.
                Distinct = solutionId.HasValue
            };

            if (solutionId.HasValue)
            {
                var link = query.AddLink("solutioncomponent", "workflowid", "objectid");
                link.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId.Value);
                link.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 29); // Workflow (incl. Cloud Flows)
            }

            EntityCollection results;
            do
            {
                results = service.RetrieveMultiple(query);
                foreach (var entity in results.Entities)
                {
                    flows.Add(new FlowRecord
                    {
                        FlowId = entity.Id,
                        Name = entity.GetAttributeValue<string>("name") ?? "",
                        Description = entity.GetAttributeValue<string>("description") ?? "",
                        StatusDisplay = entity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 0 ? "Draft" :
                                        entity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1 ? "Activated" :
                                        entity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 2 ? "Suspended" : "Unknown",
                        Owner = entity.GetAttributeValue<EntityReference>("ownerid")?.Name ?? "",
                        CreatedOn = entity.GetAttributeValue<DateTime?>("createdon"),
                        ModifiedOn = entity.GetAttributeValue<DateTime?>("modifiedon"),
                        JsonDefinition = entity.GetAttributeValue<string>("clientdata") ?? ""
                    });
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
            }
            while (results.MoreRecords);

            return flows;
        }

        private static List<SolutionRecord> LoadSolutions(IOrganizationService service)
        {
            var solutions = new List<SolutionRecord>();
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename", "version", "ismanaged"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Hide hidden/internal system solutions (e.g. "Active Solution") that are
                        // not meant to be managed directly by end users.
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true)
                    }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            foreach (var entity in results.Entities)
            {
                solutions.Add(new SolutionRecord
                {
                    SolutionId = entity.Id,
                    FriendlyName = entity.GetAttributeValue<string>("friendlyname") ?? "",
                    UniqueName = entity.GetAttributeValue<string>("uniquename") ?? "",
                    Version = entity.GetAttributeValue<string>("version") ?? "",
                    IsManaged = entity.GetAttributeValue<bool>("ismanaged")
                });
            }

            return solutions;
        }
    }

    // --- DTO ---
    public class FlowRecord
    {
        [Browsable(false)]
        public Guid FlowId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string Owner { get; set; } = "";
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        [Browsable(false)]
        public string JsonDefinition { get; set; } = "";
    }
}
