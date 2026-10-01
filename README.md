# Cloud Flow Explorer

An [XrmToolBox](https://www.xrmtoolbox.com/) plugin to search for a keyword across **all Cloud Flow (Power Automate) definitions** in a Dataverse environment — without opening each flow one by one.

## Features

- Loads every Cloud Flow (`workflow` records with `category = 5`) from the connected environment.
- Loads only the Cloud Flows belonging to a specific solution, picked from a pop-up list of the environment's solutions.
- Full-text search across the flow name, description, and raw JSON definition (`clientdata`).
- Pretty-printed, syntax-friendly JSON viewer for the selected flow, with the first search match auto-scrolled into view.
- Grid view with Name, Description, Status, Owner, Created On and Modified On columns.

## Installation

### From the XrmToolBox Tool Library (recommended)

1. Open XrmToolBox.
2. Open the Tool Library (`Ctrl+T`).
3. Search for **"Cloud Flow Explorer"**.
4. Click **Install** and restart XrmToolBox.

### Manual build

```powershell
git clone https://github.com/ygdev992/CloudFlowExplorer.git
cd CloudFlowExplorer
dotnet build --configuration Release
.\deploy.ps1 -Force
```

## Usage

1. Open **Cloud Flow Explorer** from the XrmToolBox tool list.
2. Connect to a Dataverse environment using the connection button in the toolbar.
3. Click **Load Cloud Flows** to load every flow in the environment, or **Load Flows from Solution** to pick a solution from a pop-up list and load only the Cloud Flows it contains.
4. Type a keyword in **Search in JSON** and press Enter (or click **Search**) to filter flows whose name, description, or JSON definition contains it.
5. Select a row to view its pretty-printed JSON definition in the bottom panel.

## Development

- Target framework: `.NET Framework 4.8` (matches the XrmToolBox host application).
- Built on `XrmToolBoxPackage` + `MscrmTools.Xrm.Connection` for connection management, and `Microsoft.CrmSdk.*` for the Dataverse SDK types.
- `build.ps1` — restores and builds the plugin.
- `deploy.ps1 -Force` — builds and copies the plugin into your local XrmToolBox `Plugins` folder (closing XrmToolBox first if needed).

## License

[MIT](LICENSE)
