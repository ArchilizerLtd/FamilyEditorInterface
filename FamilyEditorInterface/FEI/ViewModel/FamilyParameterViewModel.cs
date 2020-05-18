using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dialog.Alerts;
using Dialog.Service;
using FamilyEditorInterface.Dialog.Alerts;
using FamilyEditorInterface.Requests;
using FamilyEditorInterface.Resources.WPF.Model;
using FamilyEditorInterface.Resources.WPF.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        private SortedList<string, FamilyParameterModel> famParamModels;
        private List<FamilyParameter> paramLabel;    //Parameters used as dimension drivers
        private List<FamilyParameter> paramFormula;    //Parameters used as formula drivers
        private FamilyManager familyManager;    //Family Manager contains all Parameter related stuff
        private FamilyType familyType;  //The current Family Type (a family can have multiple Types)

        public EventHandler PresenterClosed;
        private static IDialogService _dialogService;


        public ICommand ShuffleCommand { get; set; }
        public ICommand PrecisionCommand { get; set; }
        public ICommand DeleteUnusedCommand { get; set; }
        public ICommand ToggleCommand { get; set; }

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
        public Document Document
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
                    DocumentName = doc.Title.Replace(".rfa", "");
                }
            }
        }
        public string DocumentName
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
                    RaisePropertyChanged("DocumentName");
                }
            }
        }

        private bool _toggleVisibility;
        public bool ToggleVisibility
        {
            get { return _toggleVisibility; }
            set
            {
                _toggleVisibility = value;
                RaisePropertyChanged("ToggleVisibility");
            }
        }

        /// <summary>
        /// The Constructor
        /// </summary>
        /// <param name="document">Current Revit Doument (better be a Family one)</param>
        public FamilyParameterViewModel(Document document)
        {
            Application.Control.handler.EncounteredError += RollBackState;

            this.Document = document;
            
            ShuffleCommand = new RelayCommand(o => Shuffle("ShuffleButton"));
            PrecisionCommand = new RelayCommand(o => Precision("PrecisionButton"));
            DeleteUnusedCommand = new RelayCommand(o => DeleteUnused("DeleteUnusedButton"));
            ToggleCommand = new RelayCommand(o => ToggleTags("ToggleVisibility"));

            this.PopulateModel();
            this._enabled = true;

            //var manager = new DataTemplateManager();
            //manager.RegisterDataTemplate<AlertDialogViewModel, AlertDialogView>();

            _dialogService = new DialogService();

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
            Initialize();   //Routine initialization of all the variables 
            InitializeFamilyManager();  //Get the Family Manager and Family Type
            GetDriverAndFormulaParameters();  //Check which parameters are used in Dims and Arrays
            PopulateFamilyParameters();
            PopulateUICollections();    //HERE
        }


        //Initialize all the collection variables
        private void Initialize()
        {
            Utils.InitializeUnits(this.doc);    //Initializes the units (meters, feet, etc.)

            ValueParameters = new ObservableCollection<FamilyParameterModel>();
            ValueParameters.CollectionChanged += ValueParameters_CollectionChanged;
            BuiltInParameters = new ObservableCollection<FamilyParameterModel>();
            CheckParameters = new ObservableCollection<FamilyParameterModel>();
            famParam = new SortedList<string, FamilyParameter>();
            famParamModels = new SortedList<string, FamilyParameterModel>();
            paramLabel = new List<FamilyParameter>();
            paramFormula = new List<FamilyParameter>();
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
        //Populates the collection of Parameters that are used to drive dimensions, arrays or a used in formulas
        private void GetDriverAndFormulaParameters()
        {
            LabelsAndArrays.GetDims(doc, ref paramLabel);
            LabelsAndArrays.GetArrays(doc, ref paramLabel);
            LabelsAndArrays.GetFormulas(doc, ref paramFormula);
        }
        //Populate the (sorted) list of FamilyParameters
        private void PopulateFamilyParameters()
        {
            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                //For now, only allow integers and doubles
                if (fp.StorageType == StorageType.Double || fp.StorageType == StorageType.Integer)   
                {
                    famParamModels.Add(fp.Definition.Name, ItemRetriever.GetFamilyParameterModel(familyType, fp, paramLabel, paramFormula));
                }
            }
        }
        //Populate the UI Observable Collections
        private void PopulateUICollections()
        {
            var getParameters = ItemRetriever.GetParameters(famParamModels.Values);

            ValueParameters = new ObservableCollection<FamilyParameterModel>(getParameters.Item1);
            BuiltInParameters = new ObservableCollection<FamilyParameterModel>(getParameters.Item2);
            CheckParameters = new ObservableCollection<FamilyParameterModel>(getParameters.Item3);
        }
        #endregion

        #region View
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
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }
        #endregion

        #region Requests
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
                    double randValue = Math.Round(random.NextDouble() * (plus - minus) + minus);
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
                    double randValue = Math.Round(random.NextDouble() * (plus - minus) + minus);
                    item.SuppressUpdate();
                    item.Value = randValue;
                    requestValues.Add(new Tuple<string, double>(item.Name, randValue));
                }
            }

            if (requestValues.Count > 0) MakeRequest(RequestId.SlideParam, requestValues);
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
            if (e.PropertyName == "Name")
            {
                string _name = (sender as FamilyParameterModel).Name;
                string _oldName = (sender as FamilyParameterModel).OldName;
                RequestHandling.MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(_oldName, _name));
            }
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
                DialogUtils.Alert("Alert", new List<Message>() { new Message("Warning!", $"{values.Count.ToString()} Parameters will be removed.")});
                MakeRequest(RequestId.DeleteId, values);
            }
        }
        // Toggles Tags on/off
        private void ToggleTags(object sender)
        {
            Properties.Settings.Default.ToggleVisibility = !Properties.Settings.Default.ToggleVisibility;   //Toggles the User-defined visibility
            foreach (var param in ValueParameters)
            {
                param.Visible = Properties.Settings.Default.ToggleVisibility;
            }
            foreach (var param in BuiltInParameters)
            {
                param.Visible = Properties.Settings.Default.ToggleVisibility;
            }
            foreach (var param in CheckParameters)
            {
                param.Visible = Properties.Settings.Default.ToggleVisibility;
            }
        }
        // Makes requiest, renames Parameters
        private void MakeRequest(RequestId request, List<string> values)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            Application.Control.handler.Request.DeleteValue(values);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        // Makes request, changes values
        private void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            Application.Control.handler.Request.Value(values);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
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
        /// <summary>
        /// Retrieves WPF objects based on their type, super cool
        /// https://stackoverflow.com/questions/974598/find-all-controls-in-wpf-window-by-type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
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
