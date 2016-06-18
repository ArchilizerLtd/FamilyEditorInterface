using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace FamilyEditorInterface
{
    public partial class Interface : System.Windows.Forms.Form
    {
        private Autodesk.Revit.UI.ExternalEvent exEvent;
        private RequestHandler handler;
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Autodesk.Revit.DB.Document doc;
        private SortedList<string, FamilyParameter> famParam;
        private Assembly _assembly;
        private Stream _imageStream;
        private Bitmap _imageMini;
        private Bitmap _imageMaxi;
        private bool messageTriggered;
        private bool minimizedState;
        private DisplayUnitType dut;
        public const double METERS_IN_FEET = 0.3048;
        private TrackBar[] track;
        private CheckBox[] check; 
        private Label[] labels;
        private System.Windows.Forms.TextBox[] input;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exEvent"></param>
        /// <param name="handler"></param>
        public Interface(UIApplication uiapp, Autodesk.Revit.UI.ExternalEvent exEvent, RequestHandler handler)
        {
            this.uiapp = uiapp;
            this.uidoc = uiapp.ActiveUIDocument;
            this.doc = uidoc.Document;
            this.exEvent = exEvent;
            this.handler = handler;
            this.famParam = new SortedList<string, FamilyParameter>();
            InitializeComponent();
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(Screen.PrimaryScreen.WorkingArea.Width - 400, Convert.ToInt32(Screen.PrimaryScreen.WorkingArea.Height * 0.5 - this.Height * 0.5));
            CollectData();
            ImageLoad();
        }
        /// <summary>
        /// Prepare the minimize and maximize icons
        /// </summary>
        private void ImageLoad()
        {
            _assembly = Assembly.GetExecutingAssembly();
            _imageStream = _assembly.GetManifestResourceStream("FamilyEditorInterface.minimize.png");
            _imageMini = new Bitmap(_imageStream);
            _imageStream = _assembly.GetManifestResourceStream("FamilyEditorInterface.maximize.png");
            _imageMaxi = new Bitmap(_imageStream);
        }
        internal Autodesk.Revit.DB.Document Document()
        {
            if (this.doc.IsValidObject) return this.doc;
            else return null;
        }
        internal void Document(Autodesk.Revit.DB.Document doc)
        {
            this.doc = doc;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
        /// <summary>
        /// Check parameter conditions
        /// </summary>
        /// <param name="fp"></param>
        /// <param name="ft"></param>
        /// <returns></returns>
        private Boolean famEdit(FamilyParameter fp, FamilyType ft)
        {
            //double valueDouble;
            //int valueInt;
            if (!fp.StorageType.ToString().Equals("Double") && !fp.StorageType.ToString().Equals("Integer"))
            {
                return false;
            }
            //else if (fp.UserModifiable)
            //{
            //    return false;
            //}
            else if (fp.IsDeterminedByFormula || fp.Formula != null)
            {
                return false;
            }
            else if (!ft.HasValue(fp))
            {
                return false;
            }
            else if (ft.AsDouble(fp) == null && ft.AsInteger(fp) == null)
            {
                return false;
            }
            //else if (!double.TryParse(ft.AsDouble(fp).ToString(), out valueDouble) && !int.TryParse(ft.AsInteger(fp).ToString(), out valueInt))
            //{
            //    return false;
            //}
            else if (fp.IsReporting)
            {
                return false;
            }
            else if (fp.IsDeterminedByFormula)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Get all parameters
        /// </summary>
        private void CollectData()
        {

            if (!doc.IsFamilyDocument)
            {
                //Command.global_message =
                //  "Please run this command in a family document.";

                //TaskDialog.Show("Message", Command.global_message);
            }

            FamilyManager familyManager = doc.FamilyManager;
            FamilyType familyType = familyManager.CurrentType;

            //this.panel1.Controls.Clear();
            this.flowLayoutPanel1.Controls.Clear();
            this.flowLayoutPanel2.Controls.Clear();
            int index = 0;
            int indexChk = 0;
            double value = 0.0;

            track = new TrackBar[familyManager.Parameters.Size];
            check = new CheckBox[familyManager.Parameters.Size];
            labels = new Label[familyManager.Parameters.Size];
            input = new System.Windows.Forms.TextBox[familyManager.Parameters.Size];


            famParam.Clear();

            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                if (!famEdit(fp, familyType)) continue;
                else famParam.Add(fp.Definition.Name, fp);

            }
            //sort by name

            //add controlls
            List<ElementId> eId = new List<ElementId>();
            if (famParam.Count == 0)
            {
                splitContainer1.Panel1.Controls.Add(WarningLabel("No active parameters"));
            }
            ///yes-no parameters
            foreach (FamilyParameter fp in famParam.Values)
            {
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                    eId.Add(fp.Id);
                    check[indexChk] = new CheckBox();
                    check[indexChk].Name = fp.Definition.Name;
                    check[indexChk].Text = Truncate(fp.Definition.Name, 10);
                    check[indexChk].Checked = Convert.ToInt32(value) == 1 ? true : false;
                    check[indexChk].MouseUp += new System.Windows.Forms.MouseEventHandler(checkBox_MouseUp);
                    check[indexChk].Click += new EventHandler(trackBar_ValueChanged);
                    check[indexChk].Tag = indexChk;
                    check[indexChk].Padding = new Padding(3, 3, 3, 3);
                    check[indexChk].Margin = new Padding(0, 0, 50, 5);
                    this.flowLayoutPanel2.Controls.Add(check[indexChk]);
                    indexChk++;
                    continue;
                }
                if (this.flowLayoutPanel2.Controls.Count == 0)
                {
                    Label error = new Label();
                    error.Text = "This family has no Yes/No parameters.";
                    error.TextAlign = ContentAlignment.MiddleCenter;
                    error.AutoSize = true;
                    error.Padding = new Padding(55, 35, 0, 0);
                    error.ForeColor = System.Drawing.Color.DarkGray;
                    this.flowLayoutPanel2.Controls.Add(error);
                };
                ///slider parameters
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);
                double trueValue = 0.0;
                //DisplayUnitType dut = this.doc.GetUnits().GetDisplayUnitType();
                dut = this.doc.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits;
                if (fp.Definition.ParameterType == ParameterType.Length)
                {
                    switch (dut)
                    {
                        case DisplayUnitType.DUT_METERS:
                            trueValue = value * METERS_IN_FEET;
                            break;
                        case DisplayUnitType.DUT_CENTIMETERS:
                            trueValue = value * METERS_IN_FEET * 100;
                            break;
                        case DisplayUnitType.DUT_DECIMAL_FEET:
                            trueValue = value;
                            break;
                        case DisplayUnitType.DUT_DECIMAL_INCHES:
                            trueValue = value * 12;
                            break;
                        case DisplayUnitType.DUT_METERS_CENTIMETERS:
                            trueValue = value * METERS_IN_FEET;
                            break;
                        case DisplayUnitType.DUT_MILLIMETERS:
                            trueValue = value * METERS_IN_FEET * 1000;
                            break;
                    }
                }
                if (value != 0)
                {
                    track[index] = new TrackBar();
                    track[index].Name = fp.Definition.Name;
                    track[index].Text = fp.Definition.Name;
                    track[index].Size = new Size(180, 10);
                    track[index].Maximum = Convert.ToInt32(value * 100) * 2;
                    track[index].Minimum = 1;
                    track[index].Value = Convert.ToInt32(value * 100);
                    track[index].TickFrequency = Convert.ToInt32(track[index].Maximum * 0.05);
                    track[index].MouseUp += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
                    track[index].ValueChanged += new EventHandler(trackBar_ValueChanged);
                    track[index].Tag = index;
                    track[index].Padding = new Padding(3, 3, 3, 3);
                    labels[index] = new Label();
                    labels[index].AutoSize = true;
                    labels[index].MaximumSize = new Size(70, 0);
                    labels[index].Font = new Font("Arial", 8);
                    labels[index].Text = Truncate(fp.Definition.Name, 15);
                    labels[index].Name = fp.Definition.Name;
                    labels[index].Visible = true;
                    labels[index].Padding = new Padding(3, 4, 3, 3);
                    input[index] = new System.Windows.Forms.TextBox();
                    input[index].Size = new Size(34, 10);
                    input[index].KeyDown += new KeyEventHandler(textBox_KeyDown);
                    input[index].Name = fp.Definition.Name;
                    input[index].Text = Math.Round(trueValue, 2).ToString();
                    input[index].BackColor = System.Drawing.SystemColors.Control;
                    input[index].BorderStyle = BorderStyle.None;
                    input[index].Margin = new Padding(0, 5, 0, 0);
                    input[index].ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
                    this.flowLayoutPanel1.Controls.Add(track[index]);
                    this.flowLayoutPanel1.Controls.Add(input[index]);
                    this.flowLayoutPanel1.Controls.Add(labels[index]);
                    index++;
                }
                else
                {
                    track[index] = new TrackBar();
                    track[index].Name = fp.Definition.Name;
                    track[index].Text = fp.Definition.Name;
                    track[index].Size = new Size(180, 10);
                    track[index].Tag = index;
                    track[index].Enabled = false;
                    track[index].Padding = new Padding(3, 3, 3, 3);
                    labels[index] = new Label();
                    labels[index].AutoSize = true;
                    labels[index].MaximumSize = new Size(70, 0);
                    labels[index].Font = new Font("Arial", 8);
                    labels[index].ForeColor = System.Drawing.Color.LightGray;
                    labels[index].Text = Truncate(fp.Definition.Name, 15);
                    labels[index].Name = fp.Definition.Name;
                    labels[index].Visible = true;
                    labels[index].Padding = new Padding(3, 4, 3, 3);
                    this.flowLayoutPanel1.Controls.Add(track[index]);
                    this.flowLayoutPanel1.Controls.Add(labels[index]);
                    index++;
                }
            }
        }
        /// <summary>
        /// truncate string and add '..' at the end
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private string Truncate(string source, int length)
        {
            if (source.Length > length)
            {
                source = source.Substring(0, length);
                return source + "..";
            }
            else
            {
                return source;
            }

        }
        /// <summary>
        /// Helper method to assign label to an empty panel
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private System.Windows.Forms.Label WarningLabel(string p)
        {
            Label l = new Label();
            l.AutoSize = true;
            l.MaximumSize = new Size(90, 0);
            l.Location = new System.Drawing.Point(Convert.ToInt32(splitContainer1.Width * 0.5 - l.Size.Width * 0.5), Convert.ToInt32(splitContainer1.Height * 0.5 - l.Size.Height * 0.5));
            l.Font = new Font("Arial", 8, FontStyle.Italic);
            l.Text = p;
            return l;
        }
        /// <summary>
        /// On update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar_MouseUp(object sender, MouseEventArgs e)
        {
            TrackBar tbar = sender as TrackBar;

            if (user_done_updating)
            {
                user_done_updating = false;
                double first = convertValueTO((double)tbar.Value * 0.01);
                input[Array.IndexOf(track,tbar)].Text = Math.Round(first, 2).ToString();   //trackbar to textbox value
                MakeRequest(RequestId.SlideParam, new Tuple<string, double>(tbar.Text, (double)tbar.Value * 0.01));
            }
        }
        /// <summary>
        /// On update for checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_MouseUp(object sender, MouseEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;

            if (user_done_updating)
            {
                user_done_updating = false;
                MakeRequest(RequestId.SlideParam, new Tuple<string, double>(cbox.Name, (double)(cbox.Checked ? 1.0 : 0.0)));
            }
        }
        /// <summary>
        /// On update for textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            System.Windows.Forms.TextBox tbox = sender as System.Windows.Forms.TextBox;
            if (e.KeyCode == Keys.Enter)
            {
                int first = Convert.ToInt32(tbox.Text);
                int second = Convert.ToInt32(convertValueFROM(first * 100));
                TrackBar tbar = track[Array.IndexOf(input, tbox)];
                if (second < tbar.Maximum) tbar.Value = second;  //textbox to trackbar value
                else
                {
                    tbar.Maximum = second * 2;
                    tbar.Value = second;
                }
                MakeRequest(RequestId.SlideParam, new Tuple<string, double>(tbox.Name, (double)(convertValueFROM(Convert.ToInt16(tbox.Text)))));
            }
        }
        /// <summary>
        /// forward conversion of project to unit values
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private double convertValueTO(double p)
        {
            switch (dut)
            {
                case DisplayUnitType.DUT_METERS:
                    return p * METERS_IN_FEET;
                case DisplayUnitType.DUT_CENTIMETERS:
                    return p * METERS_IN_FEET * 100;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    return p;
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return p * 12;
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                    return p * METERS_IN_FEET;
                case DisplayUnitType.DUT_MILLIMETERS:
                    return p * METERS_IN_FEET * 1000;
            }
            return p;
        }
        /// <summary>
        /// reverse the unit transformation to project units
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private double convertValueFROM(int p)
        {
            switch (dut)
            {
                case DisplayUnitType.DUT_METERS:
                    return p / METERS_IN_FEET;                    
                case DisplayUnitType.DUT_CENTIMETERS:
                    return p / METERS_IN_FEET / 100;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    return p;
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return p / 12;
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                    return p / METERS_IN_FEET;
                case DisplayUnitType.DUT_MILLIMETERS:
                    return p / METERS_IN_FEET / 1000;
            }
            return p;
        }

        /// <summary>
        /// Form closed event handler
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            exEvent.Dispose();
            exEvent = null;
            handler = null;

            // do not forget to call the base class
            base.OnFormClosed(e);
        }
        /// <summary>
        ///   DozeOff -> disable all controls (but the Exit button)
        /// </summary>
        /// 
        private void DozeOff()
        {
            //MessageBox.Show("You are in the Control.Doze off event.");
            //EnableCommands(false);
        }
        /// <summary>
        ///   WakeUp -> enable all controls
        /// </summary>
        /// 
        public void WakeUp()
        {
            //MessageBox.Show("You are in the Control.Wake up event.");
            //EnableCommands(true);
        }

        /// <summary>
        ///   Control enabler / disabler 
        /// </summary>
        /// 
        private void EnableCommands(bool status)
        {
            foreach (System.Windows.Forms.Control ctrl in this.Controls)
            {
                ctrl.Enabled = status;
            }
            if (!status)
            {
                this.btnExit.Enabled = true;
            }
        }
        /// <summary>
        ///   A private helper method to make a request
        ///   and put the dialog to sleep at the same time.
        /// </summary>
        /// <remarks>
        ///   It is expected that the process which executes the request 
        ///   (the Idling helper in this particular case) will also
        ///   wake the dialog up after finishing the execution.
        /// </remarks>
        ///
        private void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(value);
            handler.Request.Make(request);
            exEvent.Raise();
            DozeOff();
        }
        /// <summary>
        ///  Exit - closing the dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Control1_GotFocus(Object sender, EventArgs e)
        {
            MessageBox.Show("You are in the Control.GotFocus event.");
        }

        Boolean user_done_updating = false;
        /// <summary>
        /// track slider changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            user_done_updating = true;
        }
        /// <summary>
        /// Document Changed triggered event
        /// </summary>
        internal void DocumentChanged()
        {
            //MessageBox.Show("You are in the Control.Document Changed event.");
            CollectData();
        }
        /// <summary>
        /// If a parameter has changed, detect if it is one of 'ours' first
        /// </summary>
        /// <param name="changedId"></param>
        internal void ElementChanged(ICollection<ElementId> changedId)
        {
            if (famParam != null)
            {
                List<String> elements = new List<string>();
                foreach (ElementId eId in changedId)
                {
                    elements.Add(doc.GetElement(eId).Name);
                }
                bool hasMatch = famParam.Select(x => x.Key)
                    .Intersect(elements)
                    .Any();
                if (hasMatch) CollectData();
            }
        }
        /// <summary>
        /// "Refresh Document" - Push update new document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Autodesk.Revit.DB.Document doc = uiapp.ActiveUIDocument.Document;

                if (!this.messageTriggered)
                {
                    this.Document(doc);
                    this.DocumentChanged();
                }
                else
                {
                    this.Document(doc);
                    this.FamilyDocumentChanged();
                }
            }
            catch (Exception ex)
            {
                this.InvalidDocument(ex);
            }
        }
        /// <summary>
        /// Trigers if no valid Family Document is available on Refresh Document after Message
        /// </summary>
        private void FamilyDocumentChanged()
        {
            this.splitContainer1.Panel1.Controls.Clear();
            this.splitContainer1.Panel1.Controls.Add(this.flowLayoutPanel1);
            this.splitContainer1.Panel1.Controls.Add(this.pictureBox1);
            if (!this.minimizedState) minimizeToggle();
            this.messageTriggered = false;
            this.DocumentChanged();
        }
        /// <summary>
        /// Trigers if no valid Family Document is available on Refresh Document
        /// </summary>
        private void InvalidDocument(Exception ex)
        {
            this.minimizedState = this.splitContainer1.Panel2Collapsed;
            if (!this.splitContainer1.Panel2Collapsed) minimizeToggle();
            this.splitContainer1.Panel1.Controls.Clear();
            this.splitContainer1.Panel1.Controls.Add(WarningLabel("Please, run in a Family Document"));
            this.messageTriggered = true;
        }
        /// <summary>
        /// minimize yes no panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            minimizeToggle();
        }
        /// <summary>
        /// minimizes second split panel
        /// </summary>
        private void minimizeToggle()
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            pictureBox1.Image = (splitContainer1.Panel2Collapsed) ? _imageMaxi : _imageMini;
        }
    }
}
