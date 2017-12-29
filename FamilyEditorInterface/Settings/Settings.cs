using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyEditorInterface.Settings
{
    public partial class Settings : Form
    {
        private int precision;
        private bool systemParam;
        public bool SystemParam
        {
            get
            {
                return systemParam;
            }
            internal set
            {
                systemParam = value;
                sysParamChk.Checked = systemParam;
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
                roudingTextBox.Text = this.Convert(precision);
                roudingDropDown.SelectedIndex = precision;
            }
        }
        public Settings()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = sender as ComboBox;
            roudingTextBox.Text = this.Convert(box.SelectedIndex);
            precision = box.SelectedIndex;
        }
        private string Convert(int index)
        {
            if (index == 0) return "1";
            if (index == 1) return "0.1";
            if (index == 2) return "0.01";
            return "default";
        }
        // Escape button
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void sysParamChk_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            systemParam = chk.Checked;
        }
    }
}
