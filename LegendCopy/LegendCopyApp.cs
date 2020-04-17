using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitCommon.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace Elk
{
    [ExtApp(Name = "Legend Copy", Description = "Copy a legend view from one sheet to the same location on any other sheet(s).",
        Guid = "9fa4eed1-4272-4804-a3e3-24609a3a6e33", Vendor = "LOGT", VendorDescription = "LMNts And Timothy Logan",
        ForceEnabled = false, Commands = new[] { "Legend Copy" })]
    public class LegendCopyApp : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string path = typeof(LegendCopyApp).Assembly.Location;

            // Create the PushButtonData
            PushButtonData legendCopyPBD = new PushButtonData(
                "Legend Copy", "Legend\nCopy", path, typeof(LegendCopyCmd).FullName)
            {
                LargeImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.Legend_32x32.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                ToolTip = "Copy a legend view to multiple sheets in the same location.",
            };

            // Set default config values
            string helpPath = Path.Combine(Path.GetDirectoryName(typeof(LegendCopyApp).Assembly.Location), "help\\ImportExcel.pdf");
            string tabName = "Add-Ins";
            string panelName = "Views";
            if (RevitCommon.FileUtils.GetPluginSettings(typeof(LegendCopyApp).Assembly.GetName().Name, out Dictionary<string, string> settings))
            {
                // Settings retrieved, lets try to use them.
                if (settings.ContainsKey("help-path") && !string.IsNullOrWhiteSpace(settings["help-path"]))
                {
                    // Check to see if it's relative path
                    string hp = Path.Combine(Path.GetDirectoryName(typeof(LegendCopyApp).Assembly.Location), settings["help-path"]);
                    if (File.Exists(hp))
                    {
                        helpPath = hp;
                    }
                    else
                    {
                        helpPath = settings["help-path"];
                    }
                }
                if (settings.ContainsKey("tab-name") && !string.IsNullOrWhiteSpace(settings["tab-name"]))
                {
                    tabName = settings["tab-name"];
                }

                if (settings.ContainsKey("panel-name") && !string.IsNullOrWhiteSpace(settings["panel-name"]))
                {
                    panelName = settings["panel-name"];
                }
            }

            // Setup the help file
            ContextualHelp help = null;
            if (File.Exists(helpPath))
            {
                help = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
            }
            else if (Uri.TryCreate(helpPath, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                help = new ContextualHelp(ContextualHelpType.Url, helpPath);
            }

            if (help != null)
            {
                legendCopyPBD.SetContextualHelp(help);
            }

            // Add the button to the ribbon
            RevitCommon.UI.AddToRibbon(application, tabName, panelName, legendCopyPBD);

            return Result.Succeeded;
        }
    }
}