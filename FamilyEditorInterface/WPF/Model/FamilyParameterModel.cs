using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FamilyEditorInterface.WPF
{
    public class FamilyParameterModel : INotifyPropertyChanged
    {
        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand TypeToInstanceCommand { get; private set; }

        private string _name;
        private double _value;
        private string _type;
        private string _typeOrInstance;
        private bool _associated;
        private bool _builtIn;
        private bool _shared;
        private int _precision;
        private bool _visible;
        private bool _suppres;
        
        public FamilyParameterModel()
        {
            DeleteCommand = new RelayCommand(o => Delete("DeleteTextBox"));
            TypeToInstanceCommand = new RelayCommand(o => TypeToInstance("TypeToInstance"));
        }

        public string Name
        {
            get { return _name; }
            set
            {
                //Utils.MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(_name, value));
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

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

        public int Precision
        {
            get { return _precision; }
            set
            {
                _precision = value;
                RaisePropertyChanged("Precision");
            }
        }

        public string TypeOrInstance
        {
            get { return _typeOrInstance; }
            set
            {
                _typeOrInstance = value;
                RaisePropertyChanged("TypeOrInstance");
            }
        }

        public bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                RaisePropertyChanged("Visible");
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                RaisePropertyChanged("Type");
            }
        }

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

        public bool BuiltIn
        {
            get { return _builtIn; }
            set
            {
                _builtIn = value;
                RaisePropertyChanged("BuiltIn");
            }
        }

        public bool Shared
        {
            get { return _shared; }
            set
            {
                _shared = value;
                RaisePropertyChanged("Shared");
            }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void Delete(object sender)
        {
            Utils.MakeRequest(RequestId.DeleteId, Name);
        }
        private void TypeToInstance(object sender)
        {
            TypeOrInstance = TypeOrInstance.Equals("Instance") ? "Type" : "Instance";
            Utils.MakeRequest(RequestId.TypeToInstance, Name, Type);
        }

        internal void SuppressUpdate()
        {
            this._suppres = true;
        }
    }
}
