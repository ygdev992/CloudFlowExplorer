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
        private const string BugReportEmail = "yg.catapano@outlook.it";
        private const string SearchPlaceholder = "Enter a keyword to search in the flow definition...";

        private const string ModeGeneral = "general";
        private const string ModeExact = "exact";
        private const string ActionReportBug = "reportBug";
        private const string ActionHowItWorks = "howItWorks";
        private System.Windows.Forms.Label _lblModeChip = null!;
        private System.Windows.Forms.Label _lblModeDesc = null!;

        private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        private static readonly Color MatchColor = Color.FromArgb(255, 213, 128);
        private static readonly Color CurrentMatchColor = Color.FromArgb(255, 140, 0);

        private static readonly Color JsonBackColor = Color.FromArgb(11, 26, 51);
        private static readonly Color JsonTextColor = Color.FromArgb(212, 212, 212);
        private static readonly Color JsonKeyColor = Color.FromArgb(156, 220, 254);
        private static readonly Color JsonStringColor = Color.FromArgb(206, 145, 120);
        private static readonly Color JsonNumberColor = Color.FromArgb(181, 206, 168);
        private static readonly Color JsonKeywordColor = Color.FromArgb(86, 156, 214);
        private const int SyntaxColoringMaxLength = 400_000;
        private static readonly Regex JsonTokenRegex = new Regex(
            "\"(?:\\\\.|[^\"\\\\])*\"(\\s*:)?|-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?|\\b(?:true|false|null)\\b",
            RegexOptions.Compiled);
        private string _jsonText = "";

        // Controls
        private readonly CommandButton _btnLoad;
        private readonly CommandButton _btnLoadFromSolution;
        private readonly CommandButton _btnExportExcel;
        private readonly TextBox _txtSearch;
        private readonly Button _btnSearch;
        private readonly NavRail _rail;
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
        private bool _exactMatch;
        private bool _splitterInitialised;

        public CloudFlowExplorerControl()
        {
            Dock = DockStyle.Fill;
            Font = FluentStyle.Regular;
            BackColor = FluentStyle.PageBackground;

            // --- Header band: logo + title (left), connection status (right) ---
            var panelHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = FluentStyle.Navy, Padding = new Padding(12, 0, 12, 0) };

            var logoHost = new Panel { Dock = DockStyle.Left, Width = 42, BackColor = FluentStyle.Navy };
            logoHost.Controls.Add(new PictureBox
            {
                Image = LoadLogo(),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = FluentStyle.Navy,
                Size = new Size(30, 30),
                Location = new Point(0, 5)
            });

            var lblAppTitle = new System.Windows.Forms.Label
            {
                Text = "Cloud Flow Explorer",
                AutoSize = false,
                Width = 220,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = FluentStyle.Title,
                ForeColor = Color.White
            };

            var panelConnectionRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = FluentStyle.Navy
            };

            _lblConnectionDot = new System.Windows.Forms.Label
            {
                Text = "\u25CF",
                AutoSize = true,
                ForeColor = Color.Silver,
                Margin = new Padding(0, 11, 4, 0)
            };
            _lblConnectionPrefix = new System.Windows.Forms.Label
            {
                Text = "Not connected.",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 210, 225),
                Margin = new Padding(0, 12, 4, 0)
            };
            _lblConnectionValue = new System.Windows.Forms.Label
            {
                Text = "",
                AutoSize = true,
                Font = FluentStyle.Semibold,
                ForeColor = Color.White,
                Margin = new Padding(0, 12, 0, 0)
            };
            panelConnectionRight.Controls.AddRange(new Control[] { _lblConnectionDot, _lblConnectionPrefix, _lblConnectionValue });

            panelHeader.Controls.Add(panelConnectionRight);
            panelHeader.Controls.Add(lblAppTitle);
            panelHeader.Controls.Add(logoHost);

            // --- Command bar ---
            var panelCommandBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 41),
                WrapContents = true,
                BackColor = FluentStyle.Surface,
                Margin = Padding.Empty,
                Padding = new Padding(6, 0, 6, 0)
            };
            FluentStyle.AddBottomBorder(panelCommandBar);

            _btnLoad = new CommandButton { Text = "Load Cloud Flows", Glyph = "\uE753", Enabled = false };
            _btnLoad.Click += OnLoadClick;

            _btnLoadFromSolution = new CommandButton { Text = "Load Flows from Solution", Glyph = "\uE8B7", Enabled = false };
            _btnLoadFromSolution.Click += OnLoadFromSolutionClick;

            _btnExportExcel = new CommandButton { Text = "Export to Excel", Glyph = "\uE74E", GlyphColor = Color.FromArgb(33, 115, 70), Enabled = false };
            _btnExportExcel.Click += OnExportExcelClick;

            panelCommandBar.Controls.AddRange(new Control[] { _btnLoad, _btnLoadFromSolution, _btnExportExcel });

            // --- Left rail: search mode + bug report ---
            _rail = new NavRail();
            _rail.AddHeading("SEARCH MODE");
            _rail.AddChoice(ModeGeneral, "\uE721", "Contains");
            _rail.AddChoice(ModeExact, "\uE71C", "Whole word");
            _rail.AddBottomAction(ActionHowItWorks, "\uE897", "How it works");
            _rail.AddBottomAction(ActionReportBug, "\uE7BA", "Report a bug");
            _rail.ItemSelected += (s, id) =>
            {
                _exactMatch = id == ModeExact;
                UpdateModeBanner();
                if (_allFlows.Count > 0 && !string.IsNullOrEmpty(GetSearchKeyword())) OnSearchClick(s, EventArgs.Empty);
            };
            _rail.ActionInvoked += (s, id) =>
            {
                if (id == ActionReportBug) OnReportBugClick(s, EventArgs.Empty);
                else if (id == ActionHowItWorks) ShowHowItWorks();
            };

            // --- Mode banner: always tells which search mode the next search will use ---
            var panelMode = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = FluentStyle.PageBackground, Padding = new Padding(12, 10, 12, 0) };
            _lblModeChip = new System.Windows.Forms.Label
            {
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 96,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = FluentStyle.Accent,
                ForeColor = Color.White,
                Font = FluentStyle.Semibold
            };
            _lblModeDesc = new System.Windows.Forms.Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = FluentStyle.TextSecondary,
                Padding = new Padding(10, 0, 0, 0)
            };
            var lnkHow = new LinkLabel
            {
                Text = "How it works",
                AutoSize = false,
                Dock = DockStyle.Right,
                Width = 90,
                TextAlign = ContentAlignment.MiddleRight,
                LinkColor = FluentStyle.Accent,
                ActiveLinkColor = FluentStyle.Accent
            };
            lnkHow.LinkClicked += (s, e) => ShowHowItWorks();
            panelMode.Controls.Add(_lblModeDesc);
            panelMode.Controls.Add(lnkHow);
            panelMode.Controls.Add(_lblModeChip);

            // --- Centre: search row, flow grid, status bar ---
            var panelCenter = new Panel { Dock = DockStyle.Fill, BackColor = FluentStyle.PageBackground };

            var rowSearch = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = FluentStyle.PageBackground, Padding = new Padding(12, 10, 12, 10) };

            _btnSearch = new Button { Text = "Search", Width = 90, Dock = DockStyle.Right };
            _btnSearch.Click += OnSearchClick;
            FluentStyle.StyleButton(_btnSearch, primary: true);

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
                ForeColor = FluentStyle.TextDisabled,
                BackColor = FluentStyle.Surface,
                Text = SearchPlaceholder
            };
            _txtSearch.Enter += (s, e) => { if (_txtSearch.Text == SearchPlaceholder) { _txtSearch.Text = ""; _txtSearch.ForeColor = FluentStyle.TextPrimary; } };
            _txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = SearchPlaceholder; _txtSearch.ForeColor = FluentStyle.TextDisabled; } };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { OnSearchClick(s, e); e.SuppressKeyPress = true; } };

            // Wrapper with top padding vertically centres the single-line text box in the 30px host.
            var textWrap = new Panel { Dock = DockStyle.Fill, BackColor = FluentStyle.Surface, Padding = new Padding(0, 6, 8, 0) };
            textWrap.Controls.Add(_txtSearch);

            searchHost.Controls.Add(textWrap);
            searchHost.Controls.Add(iconSearch);

            rowSearch.Controls.Add(searchHost);
            rowSearch.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            rowSearch.Controls.Add(_btnSearch);

            var panelStatus = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = FluentStyle.Surface, Padding = new Padding(12, 0, 12, 0) };
            panelStatus.Paint += (s, e) =>
            {
                using var pen = new Pen(FluentStyle.Border);
                e.Graphics.DrawLine(pen, 0, 0, panelStatus.Width, 0);
            };
            _lblFlowCount = new System.Windows.Forms.Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = FluentStyle.TextSecondary
            };
            panelStatus.Controls.Add(_lblFlowCount);

            _gridFlows = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            FluentStyle.StyleGrid(_gridFlows);
            _gridFlows.SelectionChanged += OnFlowSelectionChanged;
            _gridFlows.CellContentClick += OnGridCellContentClick;
            _gridFlows.CellFormatting += OnGridCellFormatting;
            _gridFlows.DataSource = _displayedFlows;

            panelCenter.Controls.Add(_gridFlows);
            panelCenter.Controls.Add(panelStatus);
            panelCenter.Controls.Add(rowSearch);
            panelCenter.Controls.Add(panelMode);

            _rail.Select(ModeGeneral);

            // --- Right: JSON viewer with an occurrence navigator above it ---
            var panelRight = new Panel { Dock = DockStyle.Fill, BackColor = FluentStyle.Surface };

            var panelJsonHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = FluentStyle.Surface, Padding = new Padding(12, 0, 8, 0) };
            FluentStyle.AddBottomBorder(panelJsonHeader);

            var lblJsonTitle = new System.Windows.Forms.Label
            {
                Text = "Flow definition",
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 160,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = FluentStyle.Semibold,
                ForeColor = FluentStyle.TextPrimary
            };

            var panelOccurrenceNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = FluentStyle.Surface
            };

            _btnPrevMatch = new Button { Text = "\u25C0", Width = 28, Height = 24, Enabled = false, Margin = new Padding(0, 8, 4, 0) };
            _btnPrevMatch.Click += (s, e) => MoveMatch(-1);
            FluentStyle.StyleIconButton(_btnPrevMatch);

            _btnNextMatch = new Button { Text = "\u25B6", Width = 28, Height = 24, Enabled = false, Margin = new Padding(0, 8, 4, 0) };
            _btnNextMatch.Click += (s, e) => MoveMatch(1);
            FluentStyle.StyleIconButton(_btnNextMatch);

            _lblOccurrenceCount = new System.Windows.Forms.Label
            {
                Text = "No search term",
                AutoSize = true,
                ForeColor = FluentStyle.TextSecondary,
                Margin = new Padding(0, 12, 10, 0)
            };

            panelOccurrenceNav.Controls.AddRange(new Control[] { _lblOccurrenceCount, _btnPrevMatch, _btnNextMatch });
            panelJsonHeader.Controls.Add(panelOccurrenceNav);
            panelJsonHeader.Controls.Add(lblJsonTitle);

            _txtJsonDetail = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false,
                Font = new Font(IsFontInstalled("Cascadia Code") ? "Cascadia Code" : "Consolas", 10f, FontStyle.Regular),
                BackColor = JsonBackColor,
                ForeColor = JsonTextColor
            };

            var btnCopyJson = new Button { Text = "\uE8C8", Width = 28, Height = 24, Margin = new Padding(0, 8, 4, 0) };
            FluentStyle.StyleIconButton(btnCopyJson);
            btnCopyJson.Font = FluentStyle.IconFont;
            btnCopyJson.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_jsonText)) Clipboard.SetText(_jsonText);
            };
            new ToolTip().SetToolTip(btnCopyJson, "Copy JSON");
            panelOccurrenceNav.Controls.Add(btnCopyJson);

            panelRight.Controls.Add(_txtJsonDetail);
            panelRight.Controls.Add(panelJsonHeader);

            // --- Centre | right split ---
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 5,
                BackColor = FluentStyle.Border
            };
            _splitMain.Panel1.Controls.Add(panelCenter);
            _splitMain.Panel2.Controls.Add(panelRight);
            _splitMain.SizeChanged += (s, e) =>
            {
                // Min sizes can only be applied once the control is wide enough to honour them.
                if (_splitterInitialised) return;
                int distance = (int)(_splitMain.Width * 0.58);
                if (distance < 320 || _splitMain.Width - distance < 260) return;
                _splitMain.Panel1MinSize = 320;
                _splitMain.Panel2MinSize = 260;
                _splitMain.SplitterDistance = distance;
                _splitterInitialised = true;
            };

            // --- Assemble (dock order: fill first, then rail, command bar, header) ---
            Controls.Add(_splitMain);
            Controls.Add(_rail);
            Controls.Add(panelCommandBar);
            Controls.Add(panelHeader);
        }

        private static Image? LoadLogo()
        {
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CloudFlowExplorer.logo.png");
            if (stream == null) return null;
            using var original = Image.FromStream(stream);
            return new Bitmap(original);
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

            _lblConnectionDot.ForeColor = connected ? Color.FromArgb(108, 203, 95) : Color.Silver;
            _lblConnectionPrefix.Text = connected ? "Connected to:" : "Not connected.";
            _lblConnectionPrefix.ForeColor = Color.FromArgb(200, 210, 225);
            _lblConnectionValue.Text = connected
                ? ConnectionDetail?.OrganizationFriendlyName ?? ""
                : "Use the connection button in the toolbar above to connect.";
            _lblConnectionValue.Font = connected ? FluentStyle.Semibold : FluentStyle.Regular;
            _lblConnectionValue.ForeColor = connected ? Color.White : Color.FromArgb(200, 210, 225);
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

        private void UpdateModeBanner()
        {
            if (_exactMatch)
            {
                _lblModeChip.Text = "WHOLE WORD";
                _lblModeDesc.Text = "Matches the complete word only: \"account\" finds \"account\" but not \"accountid\".";
            }
            else
            {
                _lblModeChip.Text = "CONTAINS";
                _lblModeDesc.Text = "Finds the text anywhere: \"acc\" also matches \"account\" and \"accountid\".";
            }
        }

        private void ShowHowItWorks()
        {
            using var dlg = new HowItWorksForm();
            dlg.ShowDialog(this);
        }

        private void OnSearchClick(object? sender, EventArgs e)
        {
            var keyword = GetSearchKeyword();
            if (_allFlows.Count == 0) return;

            bool exact = _exactMatch;
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
                        _jsonText = FormatJson(flow.JsonDefinition);
                    }
                    catch
                    {
                        _jsonText = flow.JsonDefinition;
                    }
                }
                else
                {
                    _jsonText = "(No JSON definition available)";
                }

                RefreshJsonHighlighting();
            }
        }

        private static bool IsFontInstalled(string name)
        {
            using var families = new System.Drawing.Text.InstalledFontCollection();
            return families.Families.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static string RtfEscape(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length + 16);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '{': sb.Append("\\{"); break;
                    case '}': sb.Append("\\}"); break;
                    case '\n': sb.Append("\\par\n"); break;
                    case '\r': break;
                    default:
                        if (c < 128) sb.Append(c);
                        else sb.Append("\\u").Append((short)c).Append('?');
                        break;
                }
            }
            return sb.ToString();
        }

        private static string RtfColor(Color c) => $"\\red{c.R}\\green{c.G}\\blue{c.B};";

        // Rebuilds the viewer content (also clearing any previous match highlight) as RTF
        // with JSON syntax colours; very large documents fall back to plain text.
        private void RenderJson()
        {
            var text = _jsonText.Replace("\r\n", "\n");
            if (text.Length == 0 || text.Length > SyntaxColoringMaxLength)
            {
                _txtJsonDetail.Text = text;
                return;
            }

            var font = _txtJsonDetail.Font;
            var sb = new System.Text.StringBuilder(text.Length * 2);
            sb.Append("{\\rtf1\\ansi\\deff0{\\fonttbl{\\f0\\fmodern ").Append(font.Name).Append(";}}");
            sb.Append("{\\colortbl;")
              .Append(RtfColor(JsonTextColor)).Append(RtfColor(JsonKeyColor)).Append(RtfColor(JsonStringColor))
              .Append(RtfColor(JsonNumberColor)).Append(RtfColor(JsonKeywordColor)).Append('}');
            sb.Append("\\f0\\fs").Append((int)Math.Round(font.SizeInPoints * 2)).Append("\\cf1 ");

            int last = 0;
            foreach (Match m in JsonTokenRegex.Matches(text))
            {
                sb.Append(RtfEscape(text.Substring(last, m.Index - last)));
                int color;
                if (m.Value[0] == '"') color = m.Groups[1].Success ? 2 : 3;
                else if (char.IsLetter(m.Value[0])) color = 5;
                else color = 4;
                sb.Append("\\cf").Append(color).Append(' ').Append(RtfEscape(m.Value)).Append("\\cf1 ");
                last = m.Index + m.Length;
            }
            sb.Append(RtfEscape(text.Substring(last))).Append('}');

            _txtJsonDetail.Rtf = sb.ToString();
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
            RenderJson();
            var text = _txtJsonDetail.Text;

            var keyword = GetSearchKeyword();
            _jsonMatchPositions = FindMatchPositions(text ?? "", keyword, _exactMatch);
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
            var columnName = _gridFlows.Columns[e.ColumnIndex].Name;

            if (columnName == "StatusDisplay")
            {
                e.CellStyle.ForeColor = (e.Value as string) switch
                {
                    "Activated" => Color.FromArgb(16, 124, 16),
                    "Suspended" => Color.FromArgb(196, 43, 28),
                    "Draft" => Color.DimGray,
                    _ => _gridFlows.DefaultCellStyle.ForeColor
                };
                e.CellStyle.Font = FluentStyle.Semibold;
                return;
            }

            if (columnName == "OpenFlow" &&
                _gridFlows.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewLinkCell linkCell)
            {
                // Selection uses a light tint, so the link keeps its accent colour on selected rows.
                linkCell.LinkColor = AccentColor;
                linkCell.ActiveLinkColor = Color.FromArgb(0, 71, 171);
                linkCell.VisitedLinkColor = AccentColor;
            }
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
            // ConnectionDetail.EnvironmentId is only a UI "environment highlighting" label, not the
            // real Power Platform environment GUID. The actual environment id is exposed by the
            // underlying CrmServiceClient (and, through it, the Dataverse OrganizationDetail).
            var environmentId = detail?.ServiceClient?.EnvironmentId
                ?? detail?.GetCrmServiceClient()?.EnvironmentId;
            if (detail == null || string.IsNullOrEmpty(environmentId))
            {
                MessageBox.Show("Unable to determine the Power Platform environment for the current connection.",
                    "Cannot open flow", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Opens the flow's details page in Power Automate. The "/flows/{id}/edit" shorthand
            // (without a solution context) does not resolve to the flow and silently redirects to
            // the home page; the "solutions/~preferred" segment is required for a direct deep link.
            var url = $"https://make.powerautomate.com/environments/{environmentId}/solutions/~preferred/flows/{flow.FlowId}/details";

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

        private void OnReportBugClick(object sender, EventArgs e)
        {
            var pluginVersion = typeof(CloudFlowExplorerControl).Assembly.GetName().Version?.ToString();
            var xtbVersion = Application.ProductVersion;
            var osVersion = Environment.OSVersion.VersionString;

            const string subject = "Cloud Flow Explorer - Bug report";
            var body = "Please describe the problem and the steps to reproduce it. "
                + "Do not include secrets or confidential flow content.\n\n\n"
                + "--- Environment ---\n"
                + $"Plugin version: {pluginVersion}\n"
                + $"XrmToolBox version: {xtbVersion}\n"
                + $"OS: {osVersion}\n";

            using var dialog = new Form
            {
                Text = "Report a bug",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(380, 232),
                Font = Font
            };
            var lblIntro = new System.Windows.Forms.Label
            {
                Text = "How would you like to report the bug?",
                AutoSize = true,
                Location = new Point(16, 16)
            };
            var btnEmail = new Button
            {
                Text = "Report via email",
                Size = new Size(348, 34),
                Location = new Point(16, 46),
                DialogResult = DialogResult.Yes
            };
            var btnGitHub = new Button
            {
                Text = "Report on GitHub (requires a GitHub account)",
                Size = new Size(348, 34),
                Location = new Point(16, 88),
                DialogResult = DialogResult.No
            };
            var lblManual = new System.Windows.Forms.Label
            {
                Text = $"No mail app opens correctly? Send your report to:\n{BugReportEmail}",
                AutoSize = true,
                Location = new Point(16, 136)
            };
            var btnCopy = new Button
            {
                Text = "Copy email address and details",
                Size = new Size(348, 34),
                Location = new Point(16, 182)
            };
            btnCopy.Click += (_, _) =>
            {
                try
                {
                    Clipboard.SetText($"To: {BugReportEmail}\nSubject: {subject}\n\n{body}");
                    btnCopy.Text = "Copied to clipboard";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(dialog, $"Could not copy to the clipboard: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            dialog.Controls.AddRange(new Control[] { lblIntro, btnEmail, btnGitHub, lblManual, btnCopy });
            dialog.CancelButton = btnGitHub;

            var result = dialog.ShowDialog(this);
            if (result != DialogResult.Yes && result != DialogResult.No)
            {
                return;
            }

            string url;
            if (result == DialogResult.Yes)
            {
                url = $"mailto:{BugReportEmail}?subject={Uri.EscapeDataString(subject)}"
                    + $"&body={Uri.EscapeDataString(body)}";
            }
            else
            {
                url = "https://github.com/ygdev992/CloudFlowExplorer/issues/new?template=bug_report.yml"
                    + "&plugin-version=" + Uri.EscapeDataString(pluginVersion ?? "")
                    + "&xtb-version=" + Uri.EscapeDataString(xtbVersion ?? "")
                    + "&os=" + Uri.EscapeDataString(osVersion);
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the email client or browser: {ex.Message}\n\nYou can report the bug by email at {BugReportEmail} "
                    + "or at https://github.com/ygdev992/CloudFlowExplorer/issues",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
