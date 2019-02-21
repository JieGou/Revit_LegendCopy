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
                if(sel.GetElementIds().Count != 1)
                {
                    TaskDialog.Show("Warning - 0", "Make sure only one Legend Viewport is selected before running this command.");
                    return Result.Succeeded;
                }

                
                // Get the selected element and verify it's a viewport
                Element e = uiDoc.Document.GetElement(sel.GetElementIds().First());
                if(e == null || e.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Viewports)
                {
                    TaskDialog.Show("Warning - 1", "Make sure only one Legend Viewport is selected before running this command.");
                    return Result.Succeeded;
                }
                
                
                // Get the viewport
                Viewport vp = e as Viewport;

                // Get the legend view's name and viewport type
                string viewName = vp.get_Parameter(BuiltInParameter.VIEWPORT_VIEW_NAME).AsString();
                ElementId vpTypeId = vp.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsElementId();
                View view = uiDoc.Document.GetElement(vp.ViewId) as View;

                // Make sure the view is a legend view
                if (view.ViewType != ViewType.Legend)
                {
                    TaskDialog.Show("Warning - 2", "Make sure only one Legend Viewport is selected before running this command.");
                    return Result.Succeeded;
                }


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
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(form) { Owner = handle };
                form.ShowDialog();

                // Log usage
                RevitCommon.FileUtils.WriteToHome("Legend Copy", commandData.Application.Application.VersionNumber, commandData.Application.Application.Username);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }


    
}
