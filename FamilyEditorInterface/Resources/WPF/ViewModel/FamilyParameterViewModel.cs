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
    /// <summary>
    /// Family Editor Interface ViewModel Class
    /// </summary>
    public class FamilyParameterViewModel : INotifyPropertyChanged
    {
        #region Properties & Constructors
        private Document doc;   //The curret Revit document (better has a family ;)
        private string documentName;    //The document name
        internal bool _isClosed;    //Keep track if the view is closed
        internal bool _enabled; //Keep track if hte view is enabled
        public FamilyParameterView view;    //The UI for the Plugin
        private SortedList<string, FamilyParameter> famParam;   
        private List<FamilyParameter> paramLabel;    //Parameters used as dimension drivers
        private List<FamilyParameter> paramFormula;    //Parameters used as formula drivers
        private FamilyManager familyManager;    //Family Manager contains all Parameter related stuff
        private FamilyType familyType;  //The current Family Type (a family can have multiple Types)

        public EventHandler PresenterClosed;

        public ICommand ShuffleCommand { get; set; }
        public ICommand PrecisionCommand { get; set; }
        public ICommand DeleteUnusedCommand { get; set; }
        public ICommand VisibilityCommand { get; set; }

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
                    _DocumentName = doc.Title.Replace(".rfa","");
                }
            }
        }
        public string _DocumentName
        {
            get
            {
                return documentName;
            }
            set
            {
                if (documentName != value)
                {
                    documentName = value;
                    RaisePropertyChanged("_DocumentName");
                }
            }
        }

        /// <summary>
        /// The Constructor
        /// </summary>
        /// <param name="document">Current Revit Doument (better be a Family one)</param>
        public FamilyParameterViewModel(Document document)
        {
            Application.handler.EncounteredError += RollBackState;

            this._Document = document;

            ShuffleCommand = new RelayCommand(o => Shuffle("ShuffleButton"));
            PrecisionCommand = new RelayCommand(o => Precision("PrecisionButton"));
            DeleteUnusedCommand = new RelayCommand(o => DeleteUnused("DeleteUnusedButton"));
            VisibilityCommand = new RelayCommand(o => ChangeVisibility("ToggleVisibility"));

            this.PopulateModel();
            this._enabled = true;
        }
        #endregion

        #region Main Methods
        // Force update in case of error
        private void RollBackState(object sender, EventArgs e)
        {
            PopulateModel();
        }
        //A sequence of methods that set up the function and UI of the Plugin
        //The main method of the View Model
        private void PopulateModel()
        {
            Initialize();
            InitializeFamilyManager();
            PopulateFamilyParameters();
            GetUsedDriverParameters();
            GetUsedFormulaParameters();
            PopulateUICollections();
            ParameterIsUsedValue();
        }
        //Populate the IsUsed value of the parameter and the Visibility of the parameter in the UI
        private void ParameterIsUsedValue()
        {
            foreach (var param in ValueParameters)
            {
                param.IsUsed = IsUsedParameter(param);  //Set the IsUsed property
                param.Visible = param.IsUsed ? true : Properties.Settings.Default.AssociatedVisibility; //Set the visibility in the UI
            }
            foreach (var param in BuiltInParameters)
            {
                param.IsUsed = IsUsedParameter(param);  //Set the IsUsed property
                param.Visible = param.IsUsed ? true : Properties.Settings.Default.AssociatedVisibility; //Set the visibility in the UI
            }
            foreach (var param in CheckParameters)
            {
                param.IsUsed = IsUsedParameter(param);  //Set the IsUsed property
                param.Visible = param.IsUsed ? true : Properties.Settings.Default.AssociatedVisibility; //Set the visibility in the UI
            }
        }
        //Determins if a parameter is used in this Family or not
        private bool IsUsedParameter(FamilyParameterModel param)
        {
            if (param.Associated) return true;
            if (param.Label) return true;
            if (param.UsedInFormula) return true;
            return false;
        }
        //Populate the UI Observable Collections
        private void PopulateUICollections()
        {
            //List<ElementId> eId = new List<ElementId>();
            double value = 0.0;

            foreach (FamilyParameter fp in famParam.Values)
            {
                bool builtIn = fp.Id.IntegerValue < 0;

                //yes-no parameters
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));

                    CheckParameters.Add(FamilyParameterItem(fp, value));

                    continue;
                }

                //slider parameters
                if (fp.StorageType == StorageType.Double)
                {
                    value = Convert.ToDouble(familyType.AsDouble(fp));
                }
                else if (fp.StorageType == StorageType.Integer)
                {
                    value = Convert.ToDouble(familyType.AsInteger(fp));
                }

                //eId.Add(fp.Id);

                //value parameters
                if (!builtIn)
                {
                    ValueParameters.Add(FamilyParameterItem(fp, value));
                }
                //built-in parameters
                else
                {
                    BuiltInParameters.Add(FamilyParameterItem(fp, value));
                }
            }
        }
        
        //Populate the (sorted) list of FamilyParameters
        private void PopulateFamilyParameters()
        {
            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                // Do not duplicate existing parameters?
                // TO DO: Set a better existing check based on ElementId
                if (!famParam.ContainsKey(fp.Definition.Name)) famParam.Add(fp.Definition.Name, fp);
                //if (!famEdit(fp, familyType)) continue;
                //else
                //{
                //    if (!famParam.ContainsKey(fp.Definition.Name))
                //        famParam.Add(fp.Definition.Name, fp);
                //}
            }
        }
        //Initialize the Family Manager for the current Document and the FamilyType
        private void InitializeFamilyManager()
        {
            familyManager = doc.FamilyManager;    //Family Manager contains all Parameter related stuff
            familyType = familyManager.CurrentType;  //The current Family Type (a family can have multiple Types)

            //In case of a new Family, we will need a default Family Type
            if (familyType == null)
            {
                familyType = CreateDefaultFamilyType(familyManager);
            }
        }
        //Initialize all the collection variables
        private void Initialize()
        {
            Utils.InitializeUnits(this.doc);    //Initializes the units (meters, feet, etc.

            ValueParameters = new ObservableCollection<FamilyParameterModel>();
            ValueParameters.CollectionChanged += ValueParameters_CollectionChanged;
            BuiltInParameters = new ObservableCollection<FamilyParameterModel>();
            CheckParameters = new ObservableCollection<FamilyParameterModel>();
            famParam = new SortedList<string, FamilyParameter>();
            paramLabel = new List<FamilyParameter>();
            paramFormula = new List<FamilyParameter>();
        }
        //Populates the collection of Parameters that are used to drive dimensions
        private void GetUsedDriverParameters()
        {
            List<Dimension> dimList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Dimensions)
                .WhereElementIsNotElementType()
                .Cast<Dimension>()
                .ToList();

            foreach (Dimension dim in dimList)
            {
                try
                {
                    if (dim.FamilyLabel != null) paramLabel.Add(dim.FamilyLabel);
                }
                catch (Exception)
                {

                }
            }
        }
        private void GetUsedFormulaParameters()
        {
            foreach(FamilyParameter fp in famParam.Values)
            {
                if (fp.Formula != null)
                {
                    //If the formula contains the name of the FamilyParameter, add it to the list
                    string formula = fp.Formula;
                    famParam.Keys.ToList().ForEach(x => { if (formula.Contains(x)) paramFormula.Add(famParam[x]); });   
                }
            }
        }
        //Create a checkbox FamilyParameterModel item
        private FamilyParameterModel FamilyParameterItem(FamilyParameter fp, double value)
        {
            //Collect data depending on the type of paramter
            FamilyParameterModel newItem = new FamilyParameterModel(); //Create new FamilyParamterModel
            newItem.Precision = Properties.Settings.Default.Precision;  //The precision set by the User in the Settings
            newItem.Name = fp.Definition.Name;  //The name of the Parameter
            newItem.Value = value;  //The Value of the parameter (can be yes/no, double, integer, string, ...
            newItem.Type = fp.Definition.ParameterType.ToString();  //The parameter type
            newItem.Associated = !fp.AssociatedParameters.IsEmpty;    //If the parameter is being associated
            newItem.BuiltIn = fp.Id.IntegerValue < 0;
            newItem.Modifiable = fp.UserModifiable;
            newItem.Formula = fp.IsDeterminedByFormula;
            newItem.Label = paramLabel.Any(f => f.Id == fp.Id);
            newItem.Reporting = fp.IsReporting;
            newItem.UsedInFormula = paramFormula.Any(f => f.Id == fp.Id);
            newItem.ReadOnly = fp.IsReadOnly;
            newItem.Shared = fp.IsShared;
            newItem.TypeOrInstance = fp.IsInstance ? "Instance" : "Type";

            return newItem;
        }
        //The collection has changed, notify the UI
        private void ValueParameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FamilyParameterModel item in e.NewItems)
                    item.PropertyChanged += MyType_PropertyChanged;

            if (e.OldItems != null)
                foreach (FamilyParameterModel item in e.OldItems)
                    item.PropertyChanged -= MyType_PropertyChanged;
        }
        //The delegate used for collection changed notification
        private void MyType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                string _name = (sender as FamilyParameterModel).Name;
                string _oldName = (sender as FamilyParameterModel).OldName;
                Utils.MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(_oldName, _name));
            }
        }
        //Check parameter conditions
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
        #endregion

        #region ViewModel Maintenance
        /// <summary>
        /// Terminates the View
        /// </summary>
        internal void Close()
        {
            view.Close();
            _isClosed = true;
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
            try
            {
                view.Show();
            }
            catch(Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
        // Shuffle parameter values
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
            }

            foreach (var item in _builtInParameters)
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
            }

            if (requestValues.Count > 0) MakeRequest(RequestId.SlideParam, requestValues);
        }
        // Change Precision
        private void Precision(object sender)
        {
            Settings settings = new Settings();
            var holder = Properties.Settings.Default.Precision;
            if (settings.ShowDialog() == true)
            {
                if (Properties.Settings.Default.Precision != holder)
                    PopulateModel();
            }
        }
        // Delete Unused Parameters
        private void DeleteUnused(object sender)
        {
            List<string> values = new List<string>();

            foreach(var item in ValueParameters)
            {
                if (!item.IsUsed) values.Add(item.Name);
            }

            foreach (var item in CheckParameters)
            {
                if (!item.IsUsed) values.Add(item.Name);
            }

            if (values.Count > 0)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("{0} Parameters will be removed.", values.Count.ToString()));
                MakeRequest(RequestId.DeleteId, values);
            }
        }
        // Toggle Visibility of Parameters which are not associated
        private void ChangeVisibility(object sender)
        {
            Properties.Settings.Default.AssociatedVisibility = !Properties.Settings.Default.AssociatedVisibility;

            foreach (var item in ValueParameters)
                if (!item.Associated)
                    item.Visible = Properties.Settings.Default.AssociatedVisibility;

            foreach (var item in BuiltInParameters)
                if (!item.Associated)
                    item.Visible = Properties.Settings.Default.AssociatedVisibility;

            foreach (var item in CheckParameters)
                if (!item.Associated)
                    item.Visible = Properties.Settings.Default.AssociatedVisibility;
        }
        // Makes requiest, renames Parameters
        private void MakeRequest(RequestId request, List<string> values)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            Application.handler.Request.DeleteValue(values);
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        // Makes request, changes values
        private void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            Application.handler.Request.Value(values);
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        /// <summary>
        /// Activate the View
        /// </summary>
        internal void Enable()
        {
            if(!_enabled)
            {
                _enabled = true;
                view.IsEnabled = true;
                view.Visibility = System.Windows.Visibility.Visible;
            }
        }
        /// <summary>
        /// Disactivate the View
        /// </summary>
        internal void Disable()
        {
            if (_enabled)
            {
                _enabled = false;
                view.IsEnabled = false;
                view.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        /// <summary>
        /// In case the document has changed, repopulate the model with the new Parameters
        /// </summary>
        internal void ThisDocumentChanged()
        {
            this.PopulateModel();
        }
        // Notify that the form is closed and effectivelly close shop
        private void FormClosed(object sender, EventArgs e)
        {
            this.Dispose();
        }
        /// <summary>
        /// Again, the document has changed, repopulate the model with the new Parameters
        /// </summary>
        internal void DocumentSwitched()
        {
            this.PopulateModel();
        }
        // Close down the presenter and raise the event
        internal void Dispose()
        {
            // This form is closed
            _isClosed = true;
            // Remove registered events
            view.Closed -= FormClosed;
            if (PresenterClosed != null)
                PresenterClosed(this, new EventArgs());
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Creates default family type.
        /// </summary>
        /// <param name="familyManager">The family manager.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Aborted by user.</exception>
        private FamilyType CreateDefaultFamilyType(FamilyManager familyManager)
        {
            FamilyType familyType = null;

            TaskDialog td = new TaskDialog("No Family Type");
            td.MainInstruction = "Create Default Family Type.";
            string s = "This might be a new Family with no existing Parameters or Family Types." + Environment.NewLine + Environment.NewLine +
            "In order to use this plugin, you can either create a new Parameter/Family Type from the Family Types Dialog" +
                " and restart the plugin or create a Default Family Type by accepting this message." + Environment.NewLine + Environment.NewLine +
                    "You can always delete the Default Family Parameter later.";
            td.MainContent = s;
            td.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

            TaskDialogResult tResult = td.Show();

            if (TaskDialogResult.Yes == tResult)
            {
                using (Transaction t = new Transaction(doc, "Create Family Type"))
                {
                    t.Start();
                    familyType = familyManager.NewType("Default");
                    t.Commit();
                }
            }
            else
            {
                throw new Exception("Aborted by user.");
            }

            return familyType;
        }
        #endregion

        #region Interface Implementation
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    #region Commands
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
    #endregion
}
