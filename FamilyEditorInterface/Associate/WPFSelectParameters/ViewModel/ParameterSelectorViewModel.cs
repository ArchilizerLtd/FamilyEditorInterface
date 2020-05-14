using Autodesk.Revit.DB;
using FamilyEditorInterface.Associate.WPFSelectParameters.Model;
using FamilyEditorInterface.Associate.WPFSelectParameters.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Associate.WPFSelectParameters.ViewModel
{
    public class ParameterSelectorViewModel : INotifyPropertyChanged
    {

        public ParameterSelectorView view;    //The UI for the Plugin

        private ObservableCollection<ParameterSelectorModel> _parameters;
        private List<FamilyParameter> revitParameters;

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

        public ParameterSelectorViewModel(List<FamilyParameter> parameters)
        {
            this.revitParameters = parameters;

            Initialize();
        }

        private void Initialize()
        {
            Parameters = PopulateParameters();
        }
        //Populates the list (ObservableCollection) of Models to display in the UI
        private ObservableCollection<ParameterSelectorModel> PopulateParameters()
        {
            var models = new List<ParameterSelectorModel>();

            foreach(var par in revitParameters)
            {
                if (par.Id.IntegerValue < 0) continue; //Skip built-in parameters
                models.Add(new ParameterSelectorModel() { Name = par.Definition.Name, Group = Utils.GetReadableGroupName(par.Definition.ParameterGroup), Parameter = par });
            }

            return new ObservableCollection<ParameterSelectorModel>(models);
        }
        #region View
        /// <summary>
        /// Terminates the View
        /// </summary>
        internal void Close()
        {
            view.Close();
        }
        /// <summary>
        /// Show the Window, Start the Form
        /// </summary>
        /// <param name="hWndRevit"></param>
        public void Show()
        {
            view = new ParameterSelectorView(this);
            System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(view);
            //x.Owner = hWndRevit.Handle;
            try
            {
                view.Show();
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
