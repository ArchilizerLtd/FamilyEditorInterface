using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.WPF
{
    public class FamilyParameterViewModel : INotifyPropertyChanged
    {
        private UIApplication uiapp;
        private Document doc;
        private ExternalEvent exEvent;
        private RequestHandler handler;
        internal bool IsClosed;
        internal bool _enabled;
        public FamilyParameterView view;
        private SortedList<string, FamilyParameter> famParam;

        private ObservableCollection<FamilyParameterModel> _valueParameters;
        private ObservableCollection<FamilyParameterModel> _checkParameters;

        public ObservableCollection<FamilyParameterModel> ValueParameters
        {
            get { return _valueParameters; }
            set
            {
                _valueParameters = value;
                RaisePropertyChanged("ValueParameters");
            }
        }
        public ObservableCollection<FamilyParameterModel> CheckParameters
        {
            get { return _checkParameters; }
            set
            {
                _checkParameters = value;
                RaisePropertyChanged("CheckParameters");
            }
        }

        public UIApplication _Application
        {
            get
            {
                return uiapp;
            }
            set
            {
                if (uiapp != value)
                {
                    uiapp = value;
                    _Document = uiapp.ActiveUIDocument.Document;
                }
            }
        }
        public Document _Document
        {
            get
            {
                return doc;
            }
            set
            {
                if (doc != value)
                {
                    doc = value;
                }
            }
        }

        public FamilyParameterViewModel(UIApplication uiapp, ExternalEvent exEvent, RequestHandler handler)
        {
            this._Application = uiapp;
            this.exEvent = exEvent;
            this.handler = handler;
            this.PopulateModel();
            this._enabled = true;
        }

        private void PopulateModel()
        {
            ValueParameters = new ObservableCollection<FamilyParameterModel>();
            CheckParameters = new ObservableCollection<FamilyParameterModel>();
            famParam = new SortedList<string, FamilyParameter>();

            //List<FamilyEditorItem> collectList = new List<FamilyEditorItem>();

            FamilyManager familyManager = doc.FamilyManager;
            FamilyType familyType = familyManager.CurrentType;

            //if (familyType == null)
            //{
            //    familyType = CreateDefaultFamilyType(familyManager);
            //}

            //int indexChk = 0;
            double value = 0.0;

            //famParam.Clear();

            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                if (!famEdit(fp, familyType)) continue;
                else famParam.Add(fp.Definition.Name, fp);
            }

            //add controlls
            List<ElementId> eId = new List<ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Dimension> dimList = collector
                .OfCategory(BuiltInCategory.OST_Dimensions)
                .WhereElementIsNotElementType()
                .Cast<Dimension>()
                .ToList();

            List<FamilyParameter> paramUsed = new List<FamilyParameter>();

            foreach (Dimension dim in dimList)
            {
                try
                {
                    if (dim.FamilyLabel != null) paramUsed.Add(dim.FamilyLabel);
                }
                catch (Exception)
                {

                }
            }

            foreach (FamilyParameter fp in famParam.Values)
            {
                bool associated = !fp.AssociatedParameters.IsEmpty || paramUsed.Any(x => x.Definition.Name.Equals(fp.Definition.Name));
                ///yes-no parameters
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));

                    eId.Add(fp.Id);

                    FamilyParameterModel newItem = new FamilyParameterModel(exEvent, handler); // collect data yes-no
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)

                    CheckParameters.Add(newItem);

                    //indexChk++;
                    continue;
                }
                ///slider parameters
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);

                //DisplayUnitType dut = this.doc.GetUnits().GetDisplayUnitType();
                //goUnits = Utils._goUnits();

                if (associated)
                {
                    FamilyParameterModel newItem = new FamilyParameterModel(exEvent, handler);  // collect data slider, value != 0
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)

                    ////some units are not supported yet
                    ////only if units are supported, add a text box
                    //if (goUnits)
                    //{
                    //    newItem.addTextbox();
                    //}

                    ValueParameters.Add(newItem);
                }
                else
                {
                    FamilyParameterModel newItem = new FamilyParameterModel(exEvent, handler); // collect data slider, value == 0
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)
                    
                    ValueParameters.Add(newItem);
                }
            }
            //sort(collectList);
        }
        
        /// <summary>
        /// Check parameter conditions
        /// </summary>
        /// <param name="fp"></param>
        /// <param name="ft"></param>
        /// <returns></returns>
        private Boolean famEdit(FamilyParameter fp, FamilyType ft)
        {
            if (!fp.StorageType.ToString().Equals("Double") && !fp.StorageType.ToString().Equals("Integer"))
            {
                return false;
            }
            else if (fp.IsDeterminedByFormula || fp.Formula != null)
            {
                return false;
            }
            else if (fp.IsReporting)
            {
                return false;
            }
            else if (fp.IsDeterminedByFormula)
            {
                return false;
            }
            return true;
        }


        #region ViewModel Maintenance
        internal void Close()
        {
            view.Close();

            IsClosed = true;
        }

        internal void Show(WindowHandle hWndRevit)
        {
            view = new FamilyParameterView(this);
            System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(view);
            x.Owner = hWndRevit.Handle;

            view.Show();
        }

        internal void Enable()
        {
            if(!_enabled)
            {
                _enabled = true;
                view.IsEnabled = true;
            }
        }

        internal void Disable()
        {
            if (_enabled)
            {
                _enabled = false;
                view.IsEnabled = false;
            }
        }

        internal void ThisDocumentChanged()
        {
            this.PopulateModel();
        }

        // Notify that the form is closed and effectivelly close shop
        private void FormClosed(object sender, EventArgs e)
        {
            this.Dispose();
        }

        internal void DocumentSwitched()
        {
            throw new NotImplementedException();
        }

        internal void Dispose()
        {
            // Revit handler stuff
            exEvent.Dispose();
            exEvent = null;
            handler = null;
            // This form is closed
            IsClosed = true;
            // Remove registered events
            view.Closed -= FormClosed;
        }
        #endregion


        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
