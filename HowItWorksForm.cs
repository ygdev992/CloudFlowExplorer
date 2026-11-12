namespace CloudFlowExplorer
{
    internal sealed class HowItWorksForm : Form
    {
        public HowItWorksForm()
        {
            Text = "How it works";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 420);
            BackColor = FluentStyle.Surface;
            Font = FluentStyle.Regular;

            var header = new System.Windows.Forms.Label
            {
                Text = "How it works",
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = FluentStyle.Navy,
                ForeColor = Color.White,
                Font = FluentStyle.Title,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            var body = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = FluentStyle.Surface,
                ForeColor = FluentStyle.TextPrimary,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                TabStop = false
            };

            AddHeading(body, "1. Load your flows");
            AddText(body, "Use \"Load Cloud Flows\" to read every cloud flow of the environment, or \"Load Flows from Solution\" to limit the list to one solution.\n\n");
            AddHeading(body, "2. Search inside the flow definitions");
            AddText(body, "Type a keyword and press Enter or click Search. The keyword is looked up in the JSON definition, name and description of each flow.\n\n");
            AddHeading(body, "3. Choose the search mode");
            AddText(body, "Contains: finds the text anywhere. \"acc\" matches \"account\" and \"accountid\".\n");
            AddText(body, "Whole word: matches the complete word only. \"account\" matches \"account\" but not \"accountid\".\n");
            AddText(body, "The active mode is always shown above the grid. Changing it re-runs the current search.\n\n");
            AddHeading(body, "4. Inspect the result");
            AddText(body, "Select a flow to see its definition on the right. Every occurrence is highlighted: use the arrows to jump between them, or copy the JSON with the copy button.\n\n");
            AddHeading(body, "5. Export");
            AddText(body, "\"Export to Excel\" saves the flows currently shown in the grid.");

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = FluentStyle.Surface, Padding = new Padding(0, 10, 16, 10) };
            var btnClose = new Button { Text = "Close", Width = 90, Dock = DockStyle.Right, DialogResult = DialogResult.OK };
            FluentStyle.StyleButton(btnClose, primary: true);
            footer.Controls.Add(btnClose);

            var bodyHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), BackColor = FluentStyle.Surface };
            bodyHost.Controls.Add(body);

            Controls.Add(bodyHost);
            Controls.Add(footer);
            Controls.Add(header);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private static void AddHeading(RichTextBox box, string text)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionFont = FluentStyle.Semibold;
            box.SelectionColor = FluentStyle.Accent;
            box.AppendText(text + "\n");
        }

        private static void AddText(RichTextBox box, string text)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionFont = FluentStyle.Regular;
            box.SelectionColor = FluentStyle.TextPrimary;
            box.AppendText(text);
        }
    }
}
