using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Media.Imaging;
using RevitCommon.Attributes;

namespace Elk
{
    [Transaction(TransactionMode.Manual)]
    public class LegendCopyCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;

                int version = Convert.ToInt32(uiDoc.Document.Application.VersionNumber);
                // Get the Revit window handle
                IntPtr handle = IntPtr.Zero;
                if (version < 2019)
                    handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                else
                    handle = commandData.Application.GetType().GetProperty("MainWindowHandle") != null
                        ? (IntPtr)commandData.Application.GetType().GetProperty("MainWindowHandle").GetValue(commandData.Application)
                        : IntPtr.Zero;

                // Get the current selection and make sure there's only one item selected.
                Selection sel = uiDoc.Selection;
                if (sel.GetElementIds().Count == 1)
                {
                    // Get the selected element
                    Element e = uiDoc.Document.GetElement(sel.GetElementIds().First());

                    // Make sure the element was retrieved and that it's a viewport element
                    if (e != null && e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Viewports)
                    {
                        // Get the viewport
                        Viewport vp = e as Viewport;

                        // Get the legend view's name
                        string viewName = vp.get_Parameter(BuiltInParameter.VIEWPORT_VIEW_NAME).AsString();

                        // Get the viewport type
                        ElementId vpTypeId = vp.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsElementId();
                        View view = uiDoc.Document.GetElement(vp.ViewId) as View;

                        // Make sure the view is a legend view
                        if (view.ViewType == ViewType.Legend)
                        {
                            // Get the location of the viewport
                            XYZ loc = vp.GetBoxCenter();

                            FilteredElementCollector sheetCollector = new FilteredElementCollector(uiDoc.Document);
                            sheetCollector.OfClass(typeof(ViewSheet));
                            List<ViewSheet> sheets = new List<ViewSheet>();
                            foreach (ViewSheet vs in sheetCollector)
                            {
                                sheets.Add(vs);
                            }

                            // Pass the location, view, and viewport type to the form
                            LegendCopyForm form = new LegendCopyForm(uiDoc.Document, view, vpTypeId, loc, sheets);
                            System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(form);
                            wih.Owner = handle;
                            form.ShowDialog();
                        }
                        else
                            TaskDialog.Show("Warning - 2", "Make sure only one Legend Viewport is selected before running this command.");
                    }
                    else
                        TaskDialog.Show("Warning - 1", "Make sure only one Legend Viewport is selected before running this command.");
                }
                else
                    TaskDialog.Show("Warning - 0", "Make sure only one Legend Viewport is selected before running this command.");

                RevitCommon.HKS.WriteToHome("Legend Copy", commandData.Application.Application.VersionNumber, commandData.Application.Application.Username);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }


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
                // Set the help file path
                System.IO.FileInfo fi = new System.IO.FileInfo(typeof(LegendCopyApp).Assembly.Location);
                System.IO.DirectoryInfo directory = fi.Directory;
                helpPath = directory.FullName + "\\help\\LegendCopy.pdf";

                // Set the tab name
                tabName = Properties.Settings.Default.TabName;
                panelName = Properties.Settings.Default.PanelName;
            }
            else
            {
                // Check for nulls in the returned strings
                if (helpPath == null)
                {
                    // Set the help file path
                    System.IO.FileInfo fi = new System.IO.FileInfo(typeof(LegendCopyApp).Assembly.Location);
                    System.IO.DirectoryInfo directory = fi.Directory;
                    helpPath = directory.FullName + "\\help\\LegendCopy.pdf";
                }

                if (tabName == null)
                    tabName = Properties.Settings.Default.TabName;

                if (panelName == null)
                    panelName = Properties.Settings.Default.PanelName;
            }


            //string panelName = Properties.Settings.Default.PanelName;

            // HKS Centric stuff
            // ******************************************

            // Set the help file
            //System.IO.FileInfo fi = new System.IO.FileInfo(path);
            //System.IO.DirectoryInfo directory = fi.Directory;
            //string helpPath = directory.FullName + "\\help\\LegendCopy.pdf";
            if (System.IO.File.Exists(helpPath))
            {
                ContextualHelp help = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
                legendCopyPBD.SetContextualHelp(help);
            }
            
            //int version = 0;
            //if(int.TryParse(application.ControlledApplication.VersionNumber, out version))
            //{
            //    if (version < 2017)
            //        panelName = "Tools";
            //}

            // ******************************************
            // End of HKS Centric Stuff

            // Add the button to the ribbon
            RevitCommon.UI.AddToRibbon(application, tabName, panelName, legendCopyPBD);

            return Result.Succeeded;
        }
    }
}
