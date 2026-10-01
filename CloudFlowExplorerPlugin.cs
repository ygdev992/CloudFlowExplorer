// =============================================================================
// XrmToolBox Plugin Entry Point
//
// To regenerate the icon base64 strings after replacing the PNGs, run in PowerShell:
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-32.png"))
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-80.png"))
// =============================================================================

using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace CloudFlowExplorer
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Cloud Flow Explorer")]
    [ExportMetadata("Description", "Search for a keyword across all Cloud Flow (Power Automate) definitions in the connected Dataverse environment, with a pretty-printed JSON viewer and match highlighting.")]
    [ExportMetadata("SmallImageBase64", "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAHqSURBVFhHxZe/SwMxFMczdnTs6Nixf0JHx45u7aCJiEMXoZsdXKXg0lE3waWD0uRcDhERRLxBoRQEwaWDyC1KQZHIy7WW5uV+5WL9wmdJLnl5ecnLO0JsxXgN0fBL+mfuREWF0EGbMBEQJmQ8fEiY1yUbXlWfwk6MrxImTrChDFDhF1sI4x3C+ARNnBuvmy88DX+FMK+PJyoAFTdk+7ysm8JSxtPibAkVzyqkiWKco4FuCeLDoWKOBriHiiPdNFGn1cmBywqvLS7A9aFLJ5gbV96jD2R515e1g1srhuN3uXZ4h+ZcgIr61Hsc++r+tZx8fsui4o+vsrJ3hY1HC5ieBZU+sfcw2B+9WQEKP75kuz+SpZ0LbDwiJCo54I7C9C5flBN6OyJ6yQwdy4JsDtZR4zIhlG+hxmVCGG+ixmXy/2cAKh290QH13j1qMxLlARHqHXB3W6dD2Tl7sgIE+QASmj73HD6JFgAZSeuEdOpCkIxiFwEl23QBdb0TdqB5/IA8ywooNRnBDfyV4yoo1usZVIwXCxPDLvwpVLTmxmeyLb/zAgWqUVCQqsLRMMgdYXJhGuUFdC3dACWfXoqZBBWS+50AzzMYnykKh2+YyIYgeduTBM+1/W6E5tOeV3Bf1bOd+aclUIZhF1P0A23PTbuzCrOQAAAAAElFTkSuQmCC")]
    [ExportMetadata("BigImageBase64", "iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAYAAACOEfKtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAARASURBVHhe7ZwxaBRBFIanTJkyZUpLy5QpU6ZMlxS6G8QiFoJYXRcbCdhcYWElKa9QMrsiHKkiKh6CEgSJkMJDRBYFOVFk5c3smsu8vb3dmZ29mdn54Wtye9mZudn35r15s4SYpt2jNRLS9VK2h0vi17onGIQg2iRhvE+CaEiC6BMJo7QyQTRm3wvjA3L9aIvceLoi3sI9hXSVhLRHgugEDUgzjNiAXouvire2V9vDZRLQXd451GGN0FMSHN2xd2byR3SPhFGCO9cmdMJmPfyQ1ghsUl2bphtuM/fEppol5iXbflRrAj8sOC+jBHaGecSCBpvLiATRFbEr7Qs8nmmPa3USEkQbYpfaE1vHgZFGDbOLhdhG7mFxY+yl306Ew5cnjwoaYD9gx7Uud/jg2eYs6gH2XNviO4wO0Q1dBELNxh9nCIvEG7nNoTgE8mLeFt3AfWDSKAvWeS4sVWRRilp4hGHrIrkh6EQ+PaYvb2cXMIlqO5Wu2r1Z1I5WTM+qtA2kwyrPQp5Bxv+k88T74lBh8WhjjL/sYQ5lbpTiSJJg9e4x+lszxAfikF0IRnfhexjNMPzwLR2MvmgYyLJljUPhGgwgaPL7b7pPz9LlW8/RNQr0xaHjgq1AfHEpa/depPTdV9ZQkzX+/ivdffwetV+SBHtkFrKhC0uBXzX5+Udsq9Eanf9I1++/RH2pDQrxwEWLF81h48FrsX3WqH98jvpTEyFbI7FwBgNtm8DU9J58TJduPkP9qUlyMXiQxsYXVALsiun2L9fhq8/pyu0h6oM0/71xSHfQh5aTe2HQyVnCnJ14jTq0lz++ffyh3cAAgufdevgWfdYclPIBdDBttdl/04SdKwdC3mwGOhF9LIQsfMMfeKrBa5ILPvBUIytJwx94quHiEqZV/AAq4tP3ivgZqIgfQEX8/q8ifhmjCKtUF//oqU6Wzupu9ZUqWTKhdjbadJrfziwky0o7WLqrb194CkgDZvlAJ6oRptG8L5yRVykoOJKO7gtzLp1ykqhE7fS+MDjeS5vrEodnOr0vDOdlLgmmo3jRHDq9LwxJGCSJusBu7guzx7fgSBh4FXSxnejdF44H4tBxSRQYmYrWfWFUWDQtR2ahvn3hWbMvF9vm9LHxTGZWp07LkVnYPPNmXy5fqV9AlQr9aTkYH6tRVp1fJD8Lp6g7+3Lx1815hwJvYpJW5/eNqxzvmqfOeuWqXreKoBoT3cBl6Ck+C6IiVohe/yCOpSTsZZGNi72B0vVqVva+wXWx682Jpf+dnYkw8zQOXi72Wk/X3mAEk0LHY1smZ45HxIPiBGkbsn6dWDdE0yFeoGSZc6ETtQijafFlTs+O0C8+kItt2xA/d2KobYwH7TsKWbHlDjRY7MQCgBUDnIexUtw+Lqh4idLyDSCbxG3kTgvrR3i98Z65Nq4J8WTtBjPm6idFobaxzzzqAtZy/wBjvlAVKJ3ixAAAAABJRU5ErkJggg==")]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SecondaryFontColor", "Gray")]
    [ExportMetadata("IsOpenSource", true)]
    public class CloudFlowExplorerPlugin : PluginBase, IGitHubPlugin
    {
        // IGitHubPlugin: links this plugin to its GitHub repository (shown in the
        // "Check for update" / about dialog within XrmToolBox).
        public string RepositoryName => "CloudFlowExplorer";
        public string UserName => "ygdev992";

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new CloudFlowExplorerControl();
        }
    }
}
