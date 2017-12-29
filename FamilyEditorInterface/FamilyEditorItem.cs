using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyEditorInterface
{
    public class FamilyEditorItem
    {
        private Tuple<string, double> checkBox = null;
        private Tuple<string, string> textBox = null;
        private Tuple<string, int> trackBar = null;
        private Tuple<string, double> label = null;
        private Tuple<string, double> request = null;
        private Tuple<string, string, double> restore = null;
        private string name,oldName;
        private double value, oldValue;
        private string type;
        private int barValue;
        private string boxValue;
        private bool checkValue, initialName, initialValue;
        private bool associated;
        private bool builtIn, shared;
        private int precision;
        private bool visible;

        public FamilyEditorItem()
        {
            name = "";
            value = 0.0;
            request = new Tuple<string, double>(name, value);
            restore = new Tuple<string, string, double>(name, name, value);
            initialName = initialValue = true;
        }
        public string Name
        {
            get
            {
                return name;
            }
            internal set
            {
                name = value;
                if(initialName)
                {
                    oldName = value;
                    initialName = false;
                }
                RefreshValues();
            }
        }
        public double Value
        {
            get
            {
                return value;
            }
            internal set
            {
                this.value = value;
                if(initialValue)
                {
                    oldValue = value;
                    initialValue = false;
                }
                this.barValue = Convert.ToInt32(value * 100);
                this.boxValue = Math.Round(Utils.convertValueTO(this.value), this.precision).ToString();

                RefreshValues();
            }
        }
        public int Precision
        {
            get
            {
                return precision;
            }
            internal set
            {
                precision = value;
            }
        }
        public bool Visible
        {
            get
            {
                return visible;
            }
            internal set
            {
                visible = value;
            }
        }
        public int BarValue
        {
            get
            {
                return barValue;
            }
            internal set
            {
                this.value = value * 0.01;
                this.barValue = value;
                this.boxValue = Math.Round(Utils.convertValueTO(this.value), this.precision).ToString();

                RefreshValues();
            }
        }
        public string BoxValue
        {
            get
            {
                return boxValue;
            }
            set
            {
                if (value == null) return;
                try
                {
                    this.value = Utils.convertValueFROM(Convert.ToDouble(value));
                    this.barValue = Convert.ToInt32(this.value * 100);
                    this.boxValue = value;
                }
                catch(Exception)
                {
                    return;
                }

                RefreshValues();
            }
        }
        public bool CheckValue
        {
            get
            {
                return checkValue;
            }
            set
            {
                this.value = (double)(value ? 1.0 : 0.0);
                this.checkValue = value;

                RefreshValues();
            }
        }

        public Tuple<string, double> Request
        {
            get
            {
                return request;
            }
        }
        public Tuple<string, string, double> Restore
        {
            get
            {
                return restore;
            }
        }
        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public bool Associated
        {
            get
            {
                return associated;
            }
            internal set
            {
                associated = value;
            }
        }
        public bool BuiltIn
        {
            get
            {
                return builtIn;
            }
            internal set
            {
                builtIn = value;
            }
        }
        public bool Shared
        {
            get
            {
                return shared;
            }
            internal set
            {
                shared = value;
            }
        }
        public void RestoreDefaults()
        {
            this.name = this.oldName;
            this.Value = this.oldValue;
        }
        public void SaveDefaults()
        {
            this.oldName = this.name;
            this.oldValue = this.value;
        }
        private void RefreshValues()
        {
            if (checkBox != null)
            {
                checkBox = new Tuple<string, double>(this.name, this.value);
            }
            if (textBox != null)
            {
                textBox = new Tuple<string, string>(this.name, this.boxValue);
            }
            if (trackBar != null)
            {
                trackBar = new Tuple<string, int>(this.name, this.barValue);
            }
            if (label != null)
            {
                label = new Tuple<string, double>(this.name, this.value);
            }

            request = new Tuple<string, double>(this.name, this.value);
            restore = new Tuple<string, string, double>(this.name, this.oldName, this.oldValue);
        }
        public void addCheckbox()
        {
            checkBox = new Tuple<string, double>(name, value);
        }

        public Tuple<string, double> getCheckbox()
        {
            return checkBox;
        }

        public void addTextbox()
        {
            textBox = new Tuple<string, string> (name, boxValue);
        }

        public Tuple<string, string> getTextbox()
        {
            return textBox;
        }

        public void addTrackbar()
        {
            trackBar = new Tuple<string, int>(name, barValue);
        }

        public Tuple<string, int> getTrackbar()
        {
            return trackBar;
        }

        public void addLabel()
        {
            label = new Tuple<string, double>(name, value);
        }

        public Tuple<string, double> getLabel()
        {
            return label;
        }
    }
}
