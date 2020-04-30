using System;
using System.ComponentModel;

namespace FamilyEditorInterface.WPF
{
    /// <summary>
    /// Family Parameter Model
    /// Inherits from INotifyPropertyChanged and can be used in WPF
    /// </summary>
    public class FamilyParameterModel : INotifyPropertyChanged
    {
        #region Properties & Constructors
        public RelayCommand DeleteCommand { get; private set; } //Deletes the parameter
        public RelayCommand TypeToInstanceCommand { get; private set; } //Toggles between type and instance

        private string _name;   //private Name  
        private string _oldName;    //private Old Name
        private double _value;  //private Value
        private string _type;   //private Type
        private string _typeOrInstance; //private bool if it is a Type or an Instance parameter
        private bool _associated;   //private bool if the paramter is associated 
        private bool _builtIn;  //private bool if the parameter is Built-In (and cannot be deleted)
        private bool _shared;   //private bool if hte parameter is Shared
        private int _precision; //user defined precision
        private bool _visible;  //private bool if hte parameter is visible
        private bool _suppres;  //suppres request in case of shuffle or mass request
        
        /// <summary>
        /// Constructor
        /// </summary>
        public FamilyParameterModel()
        {
            DeleteCommand = new RelayCommand(o => Delete("DeleteTextBox"));
            TypeToInstanceCommand = new RelayCommand(o => TypeToInstance("TypeToInstance"));
        }
        #endregion

        #region Public Fields
        //The old name of the Paramter, in case we rename it
        public string OldName
        {
            get { return _oldName; }
            set
            {
                _oldName = value;
                RaisePropertyChanged("OldName");
            }
        }
        //Parameter Name
        public string Name
        {
            get { return _name; }
            set
            {
                if(_name != value)
                {
                    OldName = _name;
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        //Parameter Value (Revit generic value, can be any Unit type)
        public double Value
        {
            get { return _value; }
            set
            {
                if(value <= 0 && Type != Autodesk.Revit.DB.ParameterType.YesNo.ToString())
                {
                    // Reset the value
                    RaisePropertyChanged("Value");
                }
                else
                {
                    // Suppres request in case of Shuffle, or mass request (can only make 1 single bulk request at a time)
                    if (!_suppres) Utils.MakeRequest(RequestId.SlideParam, new Tuple<string, double>(Name, value));
                    else _suppres = false;
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }
        //???
        public int Precision
        {
            get { return _precision; }
            set
            {
                _precision = value;
                RaisePropertyChanged("Precision");
            }
        }
        //If it is a Type or an Instance parameter
        public string TypeOrInstance
        {
            get { return _typeOrInstance; }
            set
            {
                _typeOrInstance = value;
                RaisePropertyChanged("TypeOrInstance");
            }
        }
        //If the Paramter is Visible (only through API)
        public bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                RaisePropertyChanged("Visible");
            }
        }
        //Parameter Type
        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                RaisePropertyChanged("Type");
            }
        }
        //If the parameter is associated
        public bool Associated
        {
            get { return _associated; }
            set
            {
                _associated = value;
                if (_associated && !Properties.Settings.Default.AssociatedVisibility) Visible = false;
                RaisePropertyChanged("Associated");
            }
        }
        //If it's a built-in paramter
        public bool BuiltIn
        {
            get { return _builtIn; }
            set
            {
                _builtIn = value;
                RaisePropertyChanged("BuiltIn");
            }
        }
        //If it's a Shared parameter
        public bool Shared
        {
            get { return _shared; }
            set
            {
                _shared = value;
                RaisePropertyChanged("Shared");
            }
        }

        #endregion

        #region Methods
        //Makes a Delete Request to Delete the Parameter
        private void Delete(object sender)
        {
            Utils.MakeRequest(RequestId.DeleteId, Name);
        }
        //Makes a TypeToInstance request to Toggle between the two
        private void TypeToInstance(object sender)
        {
            TypeOrInstance = TypeOrInstance.Equals("Instance") ? "Type" : "Instance";
            Utils.MakeRequest(RequestId.TypeToInstance, Name, Type);
        }        
        //Will force a suppres state
        internal void SuppressUpdate()
        {
            this._suppres = true;
        }
        #endregion

        #region Interface Implementation
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
