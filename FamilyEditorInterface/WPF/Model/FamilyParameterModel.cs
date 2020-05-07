using Autodesk.Revit.DB;
using FamilyEditorInterface.Requests;
using System;
using System.ComponentModel;

namespace FamilyEditorInterface.WPF
{
    public enum ParamType
    {
        YesNo,
        Area,
        Angle,
        Material,
        Supported,
        Length
    }
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
        private double _value;  //private Value - the internal Revit value (in feet)
        private double _uivalue;  //private UIValue - will be used in the WPF UI Window
        private string _type;   //private Type
        private string _typeOrInstance; //private bool if it is a Type or an Instance parameter
        private bool _associated;   //private bool if the paramter is associated 
        private bool _builtIn;  //private bool if the parameter is Built-In (and cannot be deleted)
        private bool _shared;   //private bool if hte parameter is Shared
        private int _precision; //user defined precision
        private bool _visible;  //private bool if hte parameter is visible
        private bool _suppres;  //suppres request in case of shuffle or mass request
        private bool _readOnly; //read-only parameter
        private bool _modifiable;   //indicates if the itneractive user can modify the value of the parameter
        private bool _formula;  //Is determined by a formula
        private bool _reporting;    //If it is a reporting parameter
        private bool _usedInFormula;    //If the parameter is used in any formulas
        private bool _label;  //If the parameter is used to drive dimensions
        private bool _isUsed;
        private bool conversion = false;    //We need to stop the  circular reference during Value and UIValue change

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
                _value = value;
                RaisePropertyChanged("Value");
            }
        }
        //UI Friendly Value
        public double UIValue
        {
            get { return _uivalue; }
            set
            {
                if (_uivalue != value)
                {
                    _uivalue = value;
                    RaisePropertyChanged("UIValue");
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
        //If it's a read-only paramter
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;
                RaisePropertyChanged("ReadOnly");
            }
        }
        //If the paramter is modifiable
        public bool Modifiable
        {
            get { return _modifiable; }
            set
            {
                _modifiable = value;
                RaisePropertyChanged("Modifiable");
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
        //If it's determined by formula
        public bool Formula
        {
            get { return _formula; }
            set
            {
                _formula = value;
                RaisePropertyChanged("Formula");
            }
        }
        //If it's determined by formula
        public bool Reporting
        {
            get { return _reporting; }
            set
            {
                _reporting = value;
                RaisePropertyChanged("Reporting");
            }
        }
        //If the parameter is used in any formulas
        public bool UsedInFormula
        {
            get { return _usedInFormula; }
            set
            {
                _usedInFormula = value;
                RaisePropertyChanged("UsedInFormula");
            }
        }
        //If the parameter is used to drive dimensions
        public bool Label
        {
            get { return _label; }
            set
            {
                _label = value;
                RaisePropertyChanged("Label");
            }
        }
        //Ultimately, if the parameter is in use
        public bool IsUsed
        {
            get { return _isUsed; }
            set
            {
                _isUsed = value;
                RaisePropertyChanged("IsUsed");
            }
        }
        //Revit Display Unit Type to help us display the correct unit in the UI
        public DisplayUnitType DisplayUnitType { get; internal set; }
        public ParamType ParamType { get; internal set; }
        public StorageType StorageType { get; internal set; }
        #endregion

        #region RequestChanges
        //Executes after RaisePropertyChange of UIValue
        private void ChangeUIValue()
        {
            if (!conversion)
            {
                //From UI to Revit
                conversion = true;
                double revitValue = 0.0;

                revitValue = (StorageType == StorageType.Integer) ? UIValue : Utils.GetDutValueFrom(DisplayUnitType, UIValue); //If integer, don't convert
                revitValue = (ParamType != ParamType.YesNo) ? UIValue : Utils.GetYesNoValue(UIValue);   //If it's a check box, give 0 or 1 instead of -1 or 1

                Value = revitValue;

                // Suppres request in case of Shuffle, or mass request (can only make 1 single bulk request at a time)
                if (!_suppres) RequestHandling.MakeRequest(RequestId.SlideParam, new Tuple<string, double>(Name, revitValue));
                else _suppres = false;
            }
            else
            {
                //From Revit to UI
                conversion = false;
            }
        }
        //Executes after RaisePropertyChange of Value
        private void ChangeValue()
        {
            if (!conversion)
            {
                //From Revit to UI
                conversion = true;
                UIValue = (StorageType == StorageType.Integer) ? Math.Round(Value, Precision) : Math.Round(Utils.GetDutValueTo(DisplayUnitType, Value), Precision);   //If integer, don't convert
            }
            else
            {
                //From UI to Revit
                conversion = false;
            }

        }
        //Changes the Name of the Parameter
        private void ChangeName()
        {
            RequestHandling.MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(OldName, Name));
        }
        //Makes a Delete Request to Delete the Parameter
        private void Delete(object sender)
        {
            RequestHandling.MakeRequest(RequestId.DeleteId, Name);
        }
        //Makes a TypeToInstance request to Toggle between the two
        private void TypeToInstance(object sender)
        {
            TypeOrInstance = TypeOrInstance.Equals("Instance") ? "Type" : "Instance";
            RequestHandling.MakeRequest(RequestId.TypeToInstance, Name, Type);
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
            if (propertyName.Equals("Value"))
            {
                ChangeValue();
            }
            if (propertyName.Equals("UIValue"))
            {
                ChangeUIValue();
            }
            if (propertyName.Equals("Name"))
            {
                if(OldName != null) ChangeName();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
