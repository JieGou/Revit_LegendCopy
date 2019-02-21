using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Media.Imaging;
using RevitCommon.Attributes;

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


            // Check for a settings file
            if (!RevitCommon.FileUtils.GetPluginSettings(typeof(LegendCopyApp).Assembly.GetName().Name, out string helpPath, out string tabName, out string panelName))
            {
                // Set the help file path to a default location
                helpPath = Path.Combine(Path.GetDirectoryName(typeof(LegendCopyApp).Assembly.Location), "help\\LegendCopy.pdf");

                // Set the tab name
                tabName = "Add-Ins";
                panelName = "Views";
            }
            else
            {
                // Check for nulls in the returned strings
                if (string.IsNullOrWhiteSpace(helpPath))
                    helpPath = Path.Combine(Path.GetDirectoryName(typeof(LegendCopyApp).Assembly.Location), "help\\LegendCopy.pdf");


                if (string.IsNullOrWhiteSpace(tabName))
                    tabName = "Add-Ins";

                if (string.IsNullOrWhiteSpace(panelName))
                    panelName = "Views";
            }

            // Setup the help file
            if(File.Exists(helpPath))
            {
                ContextualHelp help = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
                legendCopyPBD.SetContextualHelp(help);
            }
            else if(Uri.TryCreate(helpPath, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                ContextualHelp help = new ContextualHelp(ContextualHelpType.Url, helpPath);
                legendCopyPBD.SetContextualHelp(help);
            }

            // Add the button to the ribbon
            RevitCommon.UI.AddToRibbon(application, tabName, panelName, legendCopyPBD);

            return Result.Succeeded;
        }
    }
}
