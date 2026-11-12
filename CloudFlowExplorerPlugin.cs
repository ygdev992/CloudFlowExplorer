// =============================================================================
// XrmToolBox Plugin Entry Point
//
// To regenerate the icon base64 strings after replacing the PNGs, run in PowerShell:
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-32.png"))
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-80.png"))
// =============================================================================

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace CloudFlowExplorer
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Cloud Flow Explorer")]
    [ExportMetadata("Description", "Search for a keyword across all Cloud Flow (Power Automate) definitions in the connected Dataverse environment, with a pretty-printed JSON viewer and match highlighting.")]
    [ExportMetadata("SmallImageBase64", "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAaLSURBVFhH1ZZrbBzVFcfvPHZnd70469ihCkQOJMhOvDhpmzYVgtiKbaoklIdCJNRSNSq0fuzOneeunaRpR4CgRDxCpCLRfOgHBG0VqiIU4AsgJ8QEaHHi9Tx2Z3bXjpM4MU4Jpo1MYfGe6o6zye7ECaYlSP1JR6O553/PPffMfQxC/w+s0T4IRZIjdwWU7B6/aB9gBSvDCuk8K9opTnZeCSXy267dnlnl7fc/s2HP61xIHUmyUnaUSowD2vYxoG1TgPrOzpr7/jGg3jPACDZwcm7/QtX+gTfOf0WtcOT7rJRLoeQkoOQZoMUcsLxlcoL5AoeHH+Ww/nAAm3/wYetvDJ8uoOQEEC0j5iAo2Q95430lIor+I0bKfYoSHwItjwEnZf5a05e5zasrUbtzpDEoZ3cxOPMpUsaBSk4CJ9kvenXzokYZuo0R8wWkngJGcKbDUnqLV3M5InKqmRWdIaSOA9V7BgKC+ZxXc0Xqt43VsDgzjhQyuP3vOnV4Tb+msTc8cnZp/a8yNzY8cvrGZdrQtd5+5awW+yN+ITOI5JNAJU5BtWLe59VclgA2nkWJSaDl43CNNHRnWDVbWOXYPxicKTC8WWBwusAqY5+EpXS3t2851/emljDYPks+B4vNU02aEfZqLmFxn1FP8+nPkToBHB7eT9r8vPEeWQeIt89bBkhQHzYcAI0GAAoA3Kc3XlAc7qIUUoUJqJIt3uu/hJBg7nBnL+agLml9l7T5Bet9JI0BipuzFiNPC6i48QXDm2N0fNYYKZ8LCObem7DDleK1asAycf0YUj8ENq4fqRhsLnzYHECJM8DGUkOlNr+QfhfJZQmcT4KK6UWWN6bZuDHti+vTNM4A3TsJ4UTm1vKYHDZ2kYrScXNmUUJfXu6r4CbNqWbi5kdkvwcEY3ep3S+kBysqQEwYAR9v5Fu1o5GOx/MLiHGi+SK1fQqqlEx7edxqbGygpDGglBMQVq3N5b4KapPpRjpuAUpMQFDUe0rtHDb/iHacA6SeBrItUeI0UDvOQQAbfynvz/Toe1HfFLBdekUFasVUIx0ziqQKATyslPsqCInGtynedrdNRNZ/VmpfIhsLw6q9M6xmd4ckYzd5htXcziXa8YUlTWcG6hZsd/7s/80nsPSJE5s1DdiSb5lm1LMx/XMysYBg/LrUfgk1khml+fRsBSTzilusxNZRCLQfLP6p7X2YuvWtQuGWN2eg9e2Z6bZ3Z9IbDxebiYZ8d7pHn3ETEPWkN8YFGrRMHRM3/+muAdF6xuufi9sHij/sMABaDwO0HgJofbsILYcAOmyA9QdndhFNRDRbKCEPlDIOVXL6J94YFbCCdYQkwPLGsNc3F639hXvaBgFaDgKsO1C8YG1HAdb1zybgbm2yC7ANi2R9tTdGBZxo7SK3GS2NQE3CusXr99JxqLiZJFA+eMvFBB4lGncyygQ5B0bX/P4DnzdGBTW92SgtOO5J58dmv9fv5Y6B4v0dKTJYZQLtBsCmVDGJ7h1YR7YfOUmDgvmYt/+c+Hj9JVIySiXfzFC9/nI2Hvisue1wsdChA7QfnTWSUMdg8Vz7/qn7UadhI+k4MLx1rn6Htdjbf04W9+XrGcH5Fzl8aHkUQmL6Aa+mnA0HPvvOXYPFBzYd+uLBje8Uf/7jfPHe9S9/tImJZQ4j+QRQ6kkIbz/2oLffFQnj1BaalE4cBVoZg6DiPDmv24yw5e9rUY9tIOUkIHEE6Lv3Fasbtl5+/1+OsDDcTcvHwD2GE6eAFe2RkOL0XrdzpJHcfuXaBm28bkEyew8nOq/S4uhsHzEP1B3PA718K7BNGILR2PzWQDnXSPqdjJg7QQ4R8nPh/iHx5gyLTduHzX6/YLzBYnOQwdZZilxY549rineAWr8HmGU/BabhF0Cv6AYmKkK4OfaEd4wvpUHprwsqzmOs6IyXVjS5Mcl5ccESk0Cpp4HB9rRfHd1b3fF0N7t8K9AruoBZ0eka3djpJhGK9jztHWNerNWc6oiUvTsoOU/6hPTrZOYsNod9QnqAE+3nw6rT/S1t9IaSvmp1PM5EhYtJNJaSECC8iv9dZfSrRDDa9VumSXAHZxp/6drFSnQ/5dVfFYIru1+g3SRmE3ArsaILfCu74brviY1e/dfOvn37mEBTT//FJEgCneCLxmHRGqni3+GqsbRVjHBNMYu+WQZ6ZQ/QUQn8K7uyq25Xq7zaq8b1a/GSwCrhJf/NOBtsxq/VfhPlny//ARQMmxM/C/3JAAAAAElFTkSuQmCC")]
    [ExportMetadata("BigImageBase64", "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")]
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

        // Third-party dependencies (ClosedXML and its transitive assemblies) are packaged
        // in a "CloudFlowExplorer" subfolder next to this plugin DLL rather than merged into
        // one assembly or left alongside it under Plugins\ directly, per XrmToolBox Tool
        // Library packaging rules. Hook AssemblyResolve so the CLR can find them there.
        public CloudFlowExplorerPlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
        }

        /// <summary>
        /// Resolves referenced assemblies that ship in the "CloudFlowExplorer" subfolder
        /// alongside this plugin's DLL, instead of directly under the XrmToolBox Plugins
        /// folder, avoiding version conflicts with other tools.
        /// </summary>
        private static Assembly? AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            // Base name of the assembly that failed to resolve (strip version/culture/public key token).
            string requestedName = new AssemblyName(args.Name).Name;

            // Only attempt to resolve assemblies this plugin actually references.
            bool isOwnDependency = currentAssembly.GetReferencedAssemblies()
                .Any(a => a.Name == requestedName);
            if (!isOwnDependency)
            {
                return null;
            }

            string pluginDirectory = Path.GetDirectoryName(currentAssembly.Location) ?? string.Empty;
            string dependencyFolder = Path.Combine(pluginDirectory, Path.GetFileNameWithoutExtension(currentAssembly.Location));
            string assemblyPath = Path.Combine(dependencyFolder, $"{requestedName}.dll");

            return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
        }

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new CloudFlowExplorerControl();
        }
    }
}
