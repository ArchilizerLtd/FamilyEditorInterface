using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public ICommand ShuffleCommand { get; set; }

        private ObservableCollection<FamilyParameterModel> _valueParameters;
        private ObservableCollection<FamilyParameterModel> _builtInParameters;
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
        public ObservableCollection<FamilyParameterModel> BuiltInParameters
        {
            get { return _builtInParameters; }
            set
            {
                _builtInParameters = value;
                RaisePropertyChanged("BuiltInParameters");
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

            ShuffleCommand = new RelayCommand(o => Shuffle("ShuffleButton"));

            Utils.Init(this.doc);

            this.PopulateModel();
            this._enabled = true;
        }

        private void PopulateModel()
        {
            ValueParameters = new ObservableCollection<FamilyParameterModel>();
            ValueParameters.CollectionChanged += ValueParameters_CollectionChanged;
            BuiltInParameters = new ObservableCollection<FamilyParameterModel>();
            CheckParameters = new ObservableCollection<FamilyParameterModel>();
            famParam = new SortedList<string, FamilyParameter>();
            
            FamilyManager familyManager = doc.FamilyManager;
            FamilyType familyType = familyManager.CurrentType;
            
            double value = 0.0;

            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                if (!famEdit(fp, familyType)) continue;
                else famParam.Add(fp.Definition.Name, fp);
            }

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
                bool builtIn = fp.Id.IntegerValue < 0;
                ///yes-no parameters
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));

                    //eId.Add(fp.Id);

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

                    continue;
                }
                ///slider parameters
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);

                if (!builtIn)
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

                    BuiltInParameters.Add(newItem);
                }
            }            
        }

        private void ValueParameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FamilyParameterModel item in e.NewItems)
                    item.PropertyChanged += MyType_PropertyChanged;

            if (e.OldItems != null)
                foreach (FamilyParameterModel item in e.OldItems)
                    item.PropertyChanged -= MyType_PropertyChanged;
        }
        void MyType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Delete") return;
                //_valueParameters.Remove((FamilyParameterModel)sender);
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
        /// <summary>
        /// Show the Window, Start the Form
        /// </summary>
        /// <param name="hWndRevit"></param>
        internal void Show(WindowHandle hWndRevit)
        {
            view = new FamilyParameterView(this);
            System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(view);
            x.Owner = hWndRevit.Handle;
            view.Closed += FormClosed;
            view.Show();
        }
        /// <summary>
        /// Shuffle parameter values
        /// </summary>
        private void Shuffle(object sender)
        {
            SingleRandom random = SingleRandom.Instance;
            List<Tuple<string, double>> requestValues = new List<Tuple<string, double>>();

            foreach (var item in _valueParameters)
            {
                if (item.Value != 0)
                {
                    double v = item.Value;
                    double plus = (v + 0.25 * v);    // plus minus values - around the current value +-25%
                    double minus = (v - 0.25 * v);
                    double randValue = random.NextDouble() * (plus - minus) + minus;
                    item.SuppressUpdate();
                    item.Value = randValue;
                    requestValues.Add(new Tuple<string, double>(item.Name, randValue));
                }
                if (requestValues.Count > 0) MakeRequest(RequestId.SlideParam, requestValues);
            }
        }
        private void MakeRequest(RequestId request, string deleteValue)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.DeleteValue(new List<string>() { deleteValue });
            handler.Request.Make(request);
            exEvent.Raise();
        }
        private void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(values);
            handler.Request.Make(request);
            exEvent.Raise();
        }

        internal void Enable()
        {
            if(!_enabled)
            {
                _enabled = true;
                view.IsEnabled = true;
                view.Visibility = System.Windows.Visibility.Visible;
            }
        }

        internal void Disable()
        {
            if (_enabled)
            {
                _enabled = false;
                view.IsEnabled = false;
                view.Visibility = System.Windows.Visibility.Hidden;
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
            Utils.Init(this.doc);
            this.PopulateModel();
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    /// <summary>
    /// The Command interface that will let us relay button events to view model methods
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter ?? "<N/A>");
        }

    }
}
