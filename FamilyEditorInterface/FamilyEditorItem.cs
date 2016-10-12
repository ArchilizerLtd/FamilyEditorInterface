using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface
{
    public class FamilyEditorItem
    {
        private Dictionary<string, double> checkBox;
        private Dictionary<string, double> textBox;
        private Dictionary<string, double> trackBar;
        private Dictionary<string, double> label;
        private string name;
        private double value;

        public FamilyEditorItem()
        {
            checkBox = new Dictionary<string, double>();
            textBox = new Dictionary<string, double>();
            trackBar = new Dictionary<string, double>();
            label = new Dictionary<string, double>();
            name = "";
            value = 0.0;
        }    

        public string Name()
        {
            return name;
        }
        private void Name(string s)
        {
            name = s;
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
            checkBox.Add(s, d);
            if (name == "") Name(s);
            if (value == 0.0) Value(d);
        }

        public Dictionary<string, double> getCheckbox()
        {
            return checkBox;
        }

        public void addTextbox(string s, double d)
        {
            textBox.Add(s, d);
            if (name == "") Name(s);
            if (value == 0.0) Value(d);
        }

        public Dictionary<string, double> getTextbox()
        {
            return textBox;
        }

        public void addTrackbar(string s, double d)
        {
            trackBar.Add(s, d);
            if (name == "") Name(s);
            if (value == 0.0) Value(d);
        }

        public Dictionary<string, double> getTrackbar()
        {
            return trackBar;
        }

        public void addLabel(string s, double d)
        {
            label.Add(s, d);
            if (name == "") Name(s);
            if (value == 0.0) Value(d);
        }

        public Dictionary<string, double> getLabel()
        {
            return label;
        }
    }
}
