using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using XrmToolBox.Extensibility;

namespace CloudFlowExplorer
{
    public partial class CloudFlowExplorerControl : PluginControlBase
    {
        private const string SearchPlaceholder = "Enter a keyword to search in the flow definition...";

        // Controls
        private readonly Button _btnLoad;
        private readonly TextBox _txtSearch;
        private readonly Button _btnSearch;
        private readonly DataGridView _gridFlows;
        private readonly TextBox _txtJsonDetail;
        private readonly SplitContainer _splitMain;
        private readonly System.Windows.Forms.Label _lblFlowCount;
        private readonly System.Windows.Forms.Label _lblConnection;

        // Data
        private List<FlowRecord> _allFlows = new();
        private BindingList<FlowRecord> _displayedFlows = new();

        public CloudFlowExplorerControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);

            // --- Top panel: connection status + actions ---
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10, 10, 10, 5) };

            _lblConnection = new System.Windows.Forms.Label
            {
                Text = "Not connected. Use the connection button in the toolbar above to connect.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Location = new Point(10, 14)
            };

            _btnLoad = new Button
            {
                Text = "Load Cloud Flows",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelTop.Width - 150, 8),
                Width = 140,
                Height = 28,
                Enabled = false
            };
            _btnLoad.Click += OnLoadClick;

            // Search row
            var lblSearch = new System.Windows.Forms.Label { Text = "Search in JSON:", AutoSize = true, Location = new Point(10, 48) };
            _txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(120, 45),
                Width = panelTop.Width - 310,
                ForeColor = Color.Gray,
                Text = SearchPlaceholder
            };
            _txtSearch.Enter += (s, e) => { if (_txtSearch.Text == SearchPlaceholder) { _txtSearch.Text = ""; _txtSearch.ForeColor = SystemColors.WindowText; } };
            _txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = SearchPlaceholder; _txtSearch.ForeColor = Color.Gray; } };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { OnSearchClick(s, e); e.SuppressKeyPress = true; } };

            _btnSearch = new Button
            {
                Text = "Search",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelTop.Width - 160, 43),
                Width = 70,
                Height = 28
            };
            _btnSearch.Click += OnSearchClick;

            _lblFlowCount = new System.Windows.Forms.Label
            {
                Text = "",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelTop.Width - 85, 48)
            };

            panelTop.Controls.AddRange(new Control[] { _lblConnection, _btnLoad, lblSearch, _txtSearch, _btnSearch, _lblFlowCount });
            panelTop.Resize += (s, e) =>
            {
                _btnLoad.Location = new Point(panelTop.Width - 150, 8);
                _txtSearch.Width = panelTop.Width - 310;
                _btnSearch.Location = new Point(panelTop.Width - 160, 43);
                _lblFlowCount.Location = new Point(panelTop.Width - 85, 48);
            };

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
                RowHeadersVisible = false
            };
            _gridFlows.SelectionChanged += OnFlowSelectionChanged;
            _gridFlows.DataSource = _displayedFlows;

            _txtJsonDetail = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Cascadia Code", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(212, 212, 212)
            };

            _splitMain.Panel1.Controls.Add(_gridFlows);
            _splitMain.Panel2.Controls.Add(_txtJsonDetail);

            // --- Assemble ---
            Controls.Add(_splitMain);
            Controls.Add(panelTop);
        }

        /// <summary>
        /// Called by XrmToolBox whenever the active connection changes.
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            bool connected = Service != null;
            _btnLoad.Enabled = connected;
            _lblConnection.Text = connected
                ? $"Connected to: {ConnectionDetail?.OrganizationFriendlyName}"
                : "Not connected. Use the connection button in the toolbar above to connect.";
            _lblConnection.ForeColor = connected ? Color.Black : Color.Gray;
        }

        private void OnLoadClick(object? sender, EventArgs e)
        {
            ExecuteMethod(LoadFlows);
        }

        private void LoadFlows()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Cloud Flows...",
                Work = (worker, args) =>
                {
                    args.Result = LoadCloudFlows(Service);
                },
                PostWorkCallBack = args =>
                {
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

            List<FlowRecord> filtered;
            if (string.IsNullOrEmpty(keyword))
            {
                filtered = _allFlows;
            }
            else
            {
                filtered = _allFlows
                    .Where(f => (f.JsonDefinition ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                             || (f.Name ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                             || (f.Description ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            _displayedFlows = new BindingList<FlowRecord>(filtered);
            _gridFlows.DataSource = _displayedFlows;
            ConfigureGridColumns();
            _lblFlowCount.Text = $"{filtered.Count}/{_allFlows.Count}";
        }

        private void OnFlowSelectionChanged(object? sender, EventArgs e)
        {
            if (_gridFlows.CurrentRow?.DataBoundItem is FlowRecord flow)
            {
                try
                {
                    // Pretty-print JSON
                    if (!string.IsNullOrEmpty(flow.JsonDefinition))
                    {
                        var parsed = JToken.Parse(flow.JsonDefinition);
                        _txtJsonDetail.Text = parsed.ToString(Formatting.Indented);
                    }
                    else
                    {
                        _txtJsonDetail.Text = "(No JSON definition available)";
                    }
                }
                catch
                {
                    _txtJsonDetail.Text = flow.JsonDefinition ?? "(empty)";
                }

                HighlightSearchTerm();
            }
        }

        private void HighlightSearchTerm()
        {
            // TextBox doesn't support rich highlighting, but we can scroll to the first match
            var keyword = GetSearchKeyword();
            if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(_txtJsonDetail.Text)) return;

            var idx = _txtJsonDetail.Text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                _txtJsonDetail.Select(idx, keyword.Length);
                _txtJsonDetail.ScrollToCaret();
            }
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
        }

        // --- Dataverse query ---
        private static List<FlowRecord> LoadCloudFlows(IOrganizationService service)
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
                PageInfo = new PagingInfo { Count = 5000, PageNumber = 1, ReturnTotalRecordCount = true }
            };

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
