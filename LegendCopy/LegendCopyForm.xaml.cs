﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Autodesk.Revit.DB;

namespace Elk
{
    /// <summary>
    /// Interaction logic for LegendCopyForm.xaml
    /// </summary>
    public partial class LegendCopyForm : Window
    {
        // Revit Info
        Document _doc;
        XYZ _loc;
        ElementId _vpType;
        View _view;
        List<ViewSheet> _sheets;

        List<string> sheetParameterNames;
        List<Parameter> sheetParameters;
        List<ViewSheet> displaySheets;

        Parameter filterParam = null;

        public LegendCopyForm(Document doc, View view, ElementId vpType, XYZ loc, List<ViewSheet> sheets)
        {
            _doc = doc;
            _loc = loc;
            _vpType = vpType;
            _view = view;
            _sheets = sheets;

            // fill the list of display sheets.  This will be the visible list rather than the full
            // sheet set in case the filter is used.
            displaySheets = sheets;
            displaySheets.Sort((x, y) => x.SheetNumber.CompareTo(y.SheetNumber));

            InitializeComponent();
            GetSheetParameters();
            GetDisplaySheets();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Transaction trans = new Transaction(_doc, "Legend Copy");
                trans.Start();
                foreach (object obj in sheetsListBox.SelectedItems)
                {
                    try
                    {
                        ViewSheet vs = obj as ViewSheet;
                        Viewport vp = Viewport.Create(_doc, vs.Id, _view.Id, _loc);
                        vp.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).Set(_vpType);
                    }
                    catch { }
                }
                trans.Commit();
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", ex.Message);
            }
            Close();
        }

        private void GetSheetParameters()
        {
            sheetParameterNames = new List<string>();
            sheetParameters = new List<Parameter>();

            ViewSheet sheet = _sheets.First();
            foreach (Parameter p in sheet.Parameters)
            {
                if (!sheetParameterNames.Contains(p.Definition.Name))
                {
                    sheetParameters.Add(p);
                    sheetParameterNames.Add(p.Definition.Name);
                }
            }

            sheetParameterNames.Sort();
            sheetParameterNames.Insert(0, "(None)");
            filterComboBox.ItemsSource = sheetParameterNames;
            filterComboBox.SelectedIndex = 0;
        }

        private void filterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filterComboBox.SelectedIndex != 0)
            {
                string paramName = filterComboBox.SelectedItem as string;
                foreach (Parameter p in sheetParameters)
                {
                    if (paramName == p.Definition.Name)
                    {
                        filterParam = p;
                        break;
                    }
                }
            }
            else
            {
                filterParam = null;
            }

            if (filterParam == null || (filterTextBox.Text != null || filterTextBox.Text.Length > 0))
            {
                GetDisplaySheets();
            }
        }

        private void filterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            GetDisplaySheets();
        }

        private void GetDisplaySheets()
        {
            displaySheets = new List<ViewSheet>();
            if (filterComboBox.SelectedIndex > 0 && filterTextBox.Text != string.Empty)
            {
                List<ViewSheet> tempSheets = new List<ViewSheet>();
                foreach (ViewSheet vs in _sheets)
                {
                    try
                    {
                        Parameter param = vs.get_Parameter(filterParam.Definition);
                        if (null != param && (param.AsString().ToLower().Contains(filterTextBox.Text.ToLower()) || param.AsValueString().ToLower().Contains(filterTextBox.Text.ToLower())))
                        {
                            tempSheets.Add(vs);
                        }
                    }
                    catch { }
                }

                displaySheets = tempSheets;
            }
            else
                displaySheets = _sheets;

            displaySheets.Sort((x, y) => x.SheetNumber.CompareTo(y.SheetNumber));
            sheetsListBox.ItemsSource = displaySheets;
        }
    }
}
