using Autodesk.Revit.DB;
using FamilyEditorInterface.Associate.WPFSelectParameters.Model;
using FamilyEditorInterface.Associate.WPFSelectParameters.View;
using FamilyEditorInterface.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel
{
    public class ParameterSelectorViewModel : INotifyPropertyChanged
    {
        #region Properties and Fields
        public ParameterSelectorView view;    //The UI View for the Plugin

        public ICommand CloseCommand { get; set; }  //Close Command
        private Dictionary<FamilyParameter, bool> revitParameters;  //Contains all the Revit parameters in the current Family

        public string buttonName { get; private set; }  //The name of the Button, either be "Push" or "Pull" or something else
        public List<FamilyParameter> selectedParameters { get; internal set; }  //Contains all selected family parameters
        public List<ParameterSelectorModel> selectedFamilyParameters { get; internal set; }  //Contains all selected family parameters

        private ObservableCollection<ParameterSelectorModel> _parameters;
        public ObservableCollection<ParameterSelectorModel> Parameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters = value;
                RaisePropertyChanged("Parameters");
            }
        }

        #endregion

        #region Constructors & Initializers
        /// <summary>
        /// Public Constructor of ParameterSelectorViewModel
        /// </summary>
        /// <param name="parameters">All FamilyParamters to display</param>
        public ParameterSelectorViewModel(Dictionary<FamilyParameter, bool> parameters, string button)
        {
            this.revitParameters = parameters;
            this.buttonName = button; 

            Initialize();
        }
        //Initialize the Observable Collection containing all FamilyParameters to display to the User for selection
        private void Initialize()
        {
            Parameters = PopulateParameters();
            CloseCommand = new RelayCommand(o => Close("CloseButton"));
        }
        #endregion

        #region Methods
        //Populates the list (ObservableCollection) of Models to display in the UI
        private ObservableCollection<ParameterSelectorModel> PopulateParameters()
        {
            var models = new List<ParameterSelectorModel>();

            foreach(KeyValuePair<FamilyParameter, bool> pair in revitParameters)
            {
                var par = pair.Key;

                if (par.Id.IntegerValue < 0) continue; //Skip built-in parameters

#if RELEASE2020 || RELEASE2021 
                models.Add(new ParameterSelectorModel()
                {
                    Name = par.Definition.Name,
                    Group = Utils.GetReadableGroupName(par.Definition.ParameterGroup),
                    Parameter = par,
                    ParameterGroup = par.Definition.ParameterGroup,
                    ParameterType = par.Definition.ParameterType,
                    IsInstance = par.IsInstance,
                    IsShared = par.IsShared,
                    Exists = pair.Value
                });
#elif RELEASE2022 || RELEASE2023
                models.Add(new ParameterSelectorModel()
                {
                    Name = par.Definition.Name,
                    Group = Utils.GetReadableGroupName(par.Definition.ParameterGroup),
                    Parameter = par,
                    ParameterGroup = par.Definition.GetGroupTypeId(),
                    ParameterType = par.Definition.GetDataType(),
                    IsInstance = par.IsInstance,
                    IsShared = par.IsShared,
                    Exists = pair.Value
                }); 
#endif
            }

            models = models.OrderBy(x => x.Group).ToList();
            return new ObservableCollection<ParameterSelectorModel>(models);
        }
            


#endregion

        #region View
        //Close the View and save the selected Family Paramters
        private void Close(string v)
        {
            var selectedItems = view.propertiesListBox.SelectedItems;
            selectedParameters = new List<FamilyParameter>();
            selectedFamilyParameters = new List<ParameterSelectorModel>();
            foreach (var item in selectedItems)
            {
                selectedParameters.Add((item as ParameterSelectorModel).Parameter);
                selectedFamilyParameters.Add(item as ParameterSelectorModel);
            }
            view.Close();
        }
        /// <summary>
        /// Show the Window, Start the Form
        /// </summary>
        /// <param name="hWndRevit"></param>
        public void Show()
        {
            view = new ParameterSelectorView(this);
            //System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(view);
            //x.Owner = hWndRevit.Handle;
            try
            {
                view.ShowDialog();
            }
            catch (Exception ex)
            {
                //Show error message
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
}
