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
        private Tuple<string, double> textBox = null;
        private Tuple<string, double> trackBar = null;
        private Tuple<string, double> label = null;
        private string name;
        private double value;

        public FamilyEditorItem()
        {
            name = "";
            value = 0.0;
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
                    textBox = new Tuple<string, double>(value, textBox.Item2);
                }
                if (trackBar != null) 
                {
                    trackBar = new Tuple<string, double>(value, trackBar.Item2);
                }
                if (label != null)
                {
                    label = new Tuple<string, double>(value, label.Item2);
                }
            }
        }
        public double Value()
        {
            return value;
        }
        private void Value(double d)
        {
            value = d;
        }
        public void addCheckbox(string s, double d)
        {
            checkBox = new Tuple<string, double>(s, d);
            if (name == "") Name = s;
            if (value == 0.0) Value(d);
        }

        public Tuple<string, double> getCheckbox()
        {
            return checkBox;
        }

        public void addTextbox(string s, double d)
        {
            textBox = new Tuple< string, double> (s, d);
            if (name == "") Name = s;
            if (value == 0.0) Value(d);
        }

        public Tuple<string, double> getTextbox()
        {
            return textBox;
        }

        public void addTrackbar(string s, double d)
        {
            trackBar = new Tuple<string, double>(s, d);
            if (name == "") Name = s;
            if (value == 0.0) Value(d);
        }

        public Tuple<string, double> getTrackbar()
        {
            return trackBar;
        }

        public void addLabel(string s, double d)
        {
            label = new Tuple<string, double>(s, d);
            if (name == "") Name = s;
            if (value == 0.0) Value(d);
        }

        public Tuple<string, double> getLabel()
        {
            return label;
        }
    }
}
