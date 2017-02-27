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
        private string name;
        private double value;
        private string type;
        private int barValue;
        private string boxValue;

        public FamilyEditorItem()
        {
            name = "";
            value = 0.0;
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
        public string Name
        {
            get
            {
                return name;
            }
            internal set
            {
                name = value;
                if (checkBox != null)
                {
                    checkBox = new Tuple<string, double>(value, checkBox.Item2);
                }
                if (textBox != null)
                {
                    textBox = new Tuple<string, string>(value, textBox.Item2);
                }
                if (trackBar != null) 
                {
                    trackBar = new Tuple<string, int>(value, trackBar.Item2);
                }
                if (label != null)
                {
                    label = new Tuple<string, double>(value, label.Item2);
                }
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
                this.value = Utils.convertValueFROM(Convert.ToDouble(value));
                this.barValue = Convert.ToInt32(this.value * 100);
                this.boxValue = value;
                
                if (checkBox != null)
                {
                    checkBox = new Tuple<string, double>(checkBox.Item1, this.value);
                }
                if (textBox != null)
                {
                    textBox = new Tuple<string, string>(textBox.Item1, this.boxValue);
                }
                if (trackBar != null)
                {
                    trackBar = new Tuple<string, int>(trackBar.Item1, this.barValue);
                }
                if (label != null)
                {
                    label = new Tuple<string, double>(label.Item1, this.value);
                }
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
                this.boxValue = Math.Round(Utils.convertValueTO(this.value), 2).ToString();

                if (checkBox != null)
                {
                    checkBox = new Tuple<string, double>(checkBox.Item1, this.value);
                }
                if (textBox != null)
                {
                    textBox = new Tuple<string, string>(textBox.Item1, this.boxValue);
                }
                if (trackBar != null)
                {
                    trackBar = new Tuple<string, int>(trackBar.Item1, this.barValue);
                }
                if (label != null)
                {
                    label = new Tuple<string, double>(label.Item1, this.value);
                }
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
                this.barValue = Convert.ToInt32(value * 100);
                this.boxValue = Math.Round(Utils.convertValueTO(this.value), 2).ToString();


                if (checkBox != null)
                {
                    checkBox = new Tuple<string, double>(checkBox.Item1, value);
                }
                if (textBox != null)
                {
                    textBox = new Tuple<string, string>(textBox.Item1, this.boxValue);
                }
                if (trackBar != null)
                {
                    trackBar = new Tuple<string, int>(trackBar.Item1, this.barValue);
                }
                if (label != null)
                {
                    label = new Tuple<string, double>(label.Item1, value);
                }
            }
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
