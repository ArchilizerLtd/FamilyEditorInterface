using Autodesk.Revit.DB;
using FamilyEditorInterface.Helpers;
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
        private string _type;   //private Type
        private string _typeOrInstance; //private bool if it is a Type or an Instance parameter
        private bool _associated;   //private bool if the paramter is associated 
        private bool _builtIn;  //private bool if the parameter is Built-In (and cannot be deleted)
        private bool _shared;   //private bool if hte parameter is Shared
        private int _precision; //user defined precision
        private bool _visible;  //private bool if the parameter is visible
        private bool _suppres;  //suppres request in case of shuffle or mass request
        private bool _readOnly; //read-only parameter
        private bool _modifiable;   //indicates if the itneractive user can modify the value of the parameter
        private bool _formula;  //Is determined by a formula
        private bool _reporting;    //If it is a reporting parameter
        private bool _usedInFormula;    //If the parameter is used in any formulas
        private bool _label;  //If the parameter is used to drive dimensions
        private bool _isUsed;
        private bool _edit;
        private bool _tagVisible;   //Visibility of the Parameter Tags
        private bool _activated;  //Used to highlight the control when shuffling 

        private double _rvalue; //internal revit value
        private double _value; //project revit value
        private string _uivalue; //private UIValue - will be used in the WPF UI Window
        private string _olduivalue;

        private const double error = 0.001; //The margin of error during conversion

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
        public double RevitValue 
        {
            get { return _rvalue; }
            set
            {
                if (_rvalue != value)
                {
                    _rvalue = value;
                    RaisePropertyChanged("RevitValue");
                }
            }
        }
        //UI Friendly Value
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                }
            }
        }
        //UI Friendly Value
        public string OldUIValue
        {
            get { return _olduivalue; }
            set
            {
                if (_olduivalue != value)
                {
                    _olduivalue = value;
                }
            }
        }
        //UI Friendly Value
        public string UIValue
        {
            get { return _uivalue; }
            set
            {
                if (_uivalue != value)
                {
                    OldUIValue = _uivalue;
                    _uivalue = value;
                    RaisePropertyChanged("UIValue");
                }
            }
        }
        //Executes after RaisePropertyChange of UIValue
#if RELEASE2020
        private void SetUIValue()
        {            
            double newValue = ValueConvertUtils.DoubleFromStringConvert(StorageType, UnitType, UIValue); //Get the double representation of the value based on the display unit type
            //What could go wrong?
            //1. The conversion precision fucks it up, leading to circular reference 
            //2. The string input from the UI was bad, and returned 0.0
            if(newValue == 0.0 || Math.Abs(Value - newValue) < error) //There was an error, set back the old value
            {
                UIValue = ValueConvertUtils.StringFromDoubleConvert(UnitType, Precision, Value);
            }
            else
            {
                Value = (double)newValue;
                if(StorageType == StorageType.Integer)
                {
                    RevitValue = Value;
                }
                else
                {
                    RevitValue = Utils.GetDutValueFrom(UnitType, Value); //Make the change in the Value. This should propagate a series of changes as well
                }
            }
        }
#elif RELEASE2021 || RELEASE2022 || RELEASE2023       
        private void SetUIValue()
        {
            double newValue = ValueConvertUtils.DoubleFromStringConvert(StorageType, UnitType, UIValue); //Get the double representation of the value based on the display unit type
            //What could go wrong?
            //1. The conversion precision fucks it up, leading to circular reference 
            //2. The string input from the UI was bad, and returned 0.0
            if (newValue == 0.0 || Math.Abs(Value - newValue) < error) //There was an error, set back the old value
            {
                UIValue = ValueConvertUtils.StringFromDoubleConvert(UnitType, Precision, Value);
            }
            else
            {
                Value = (double)newValue;
                if (StorageType == StorageType.Integer)
                {
                    RevitValue = Value;
                }
                else
                {
                    RevitValue = Utils.GetDutValueFrom(UnitType, Value); //Make the change in the Value. This should propagate a series of changes as well
                }
            }
        }

#endif
        private void SetRevitValue()
        {
            //What could go wrong?
            //1. The Value is already set and we go into circular reference - that should be resolved in the Revit value (skip if we are == value)
            //2. The UIValue is already set and we go into circular reference ...
            Value = Utils.GetDutValueTo(StorageType, UnitType, RevitValue);  
            UIValue = ValueConvertUtils.StringFromDoubleConvert(UnitType, Precision, Value);
            if (!_suppres) RequestHandling.MakeRequest(RequestId.ChangeParam, new Tuple<string, double>(Name, RevitValue)); // Suppres request in case of Shuffle, or mass request (can only make 1 single bulk request at a time)
            else _suppres = false;  
        }
        //Precision based on the DisplayUnitType
        public int Precision
        {
            get { return _precision; }
            set
            {
                _precision = value;
                RaisePropertyChanged("Precision");
            }
        }
        //Used to highlight the control when editing
        public bool Activated
        {
            get { return _activated; }
            set
            {
                _activated = value;
                RaisePropertyChanged("Activated");
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
                if (Editable)
                {
                    _visible = true;   //Only apply to not editable
                }
                else
                {
                    _visible = value;
                }
                RaisePropertyChanged("Visible");
            }
        }
        //If the Paramter Tag is Visible 
        public bool TagVisible
        {
            get { return _tagVisible; }
            set
            {
                _tagVisible = value;
                RaisePropertyChanged("TagVisible");
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
                if (_associated && !Properties.Settings.Default.AssociatedVisibility) TagVisible = false;
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
        //Sets if the parameter is greyed out in the UI
        public bool Editable
        {
            get { return _edit; }
            set
            {
                _edit = value;
                RaisePropertyChanged("Editable");
            }
        }
        //Revit Display Unit Type to help us display the correct unit in the UI

#if RELEASE2020
        public DisplayUnitType UnitType { get; internal set; }
#elif RELEASE2021 || RELEASE2022 || RELEASE2023
        public ForgeTypeId UnitType { get; internal set; }      
#endif

        public ParamType ParamType { get; internal set; }
        public StorageType StorageType { get; internal set; }
#endregion

#region RequestChanges

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
            if (propertyName.Equals("UIValue"))
            {
                SetUIValue();
            }
            if (propertyName.Equals("RevitValue"))
            {
                SetRevitValue();
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
