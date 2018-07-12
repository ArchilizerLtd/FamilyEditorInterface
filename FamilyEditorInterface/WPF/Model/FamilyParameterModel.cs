using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.WPF
{
    public class FamilyParameterModel : INotifyPropertyChanged
    {
        private ExternalEvent exEvent;
        private RequestHandler handler;
        private string _name;
        private double _value;
        private string _type;
        private bool _associated;
        private bool _builtIn;
        private bool _shared;
        private int _precision;
        private bool _visible;

        public FamilyParameterModel(ExternalEvent exEvent, RequestHandler handler)
        {
            this.exEvent = exEvent;
            this.handler = handler;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(_name, value));
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged("Value");
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


        #region Request Handling
        private void MakeRequest(RequestId request, Tuple<string, string> renameValue)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.RenameValue(new List<Tuple<string, string>>() { renameValue });
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
        private void MakeRequest(RequestId request, string deleteValue)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.DeleteValue(new List<string>() { deleteValue });
            handler.Request.Make(request);
            exEvent.Raise();
        }
        private void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(new List<Tuple<string, double>>() { value });
            handler.Request.Make(request);
            exEvent.Raise();
        }
        private void MakeRequest(RequestId request, List<Tuple<string, string, double>> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.AllValues(value);
            handler.Request.Make(request);
            exEvent.Raise();
        }
        #endregion

    }
}
