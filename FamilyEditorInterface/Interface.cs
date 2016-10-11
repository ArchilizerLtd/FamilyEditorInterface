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
        private ProjectParameters projectParameters;
        private Assembly _assembly;
        private Stream _imageStream;
        private Bitmap _imageMini;
        private Bitmap _imageMaxi;
        private bool messageTriggered;
        private bool minimizedState;
        private List<System.Windows.Forms.Control> DefaultValues;
        private List<FamilyEditorItem> result, backup;

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
            this.projectParameters = new ProjectParameters(doc);
            this.DefaultValues = new List<System.Windows.Forms.Control>();
            InitializeComponent();
            Utils.Init(doc);
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(Screen.PrimaryScreen.WorkingArea.Width - 400, Convert.ToInt32(Screen.PrimaryScreen.WorkingArea.Height * 0.5 - this.Height * 0.5));
            this.result = this.projectParameters.CollectData();
            this.backup = this.projectParameters.CollectData(); ;
            ImageLoad();
            DisplayData();
        }

        private void RecollectData()
        {
            result = projectParameters.CollectData();
            projectParameters.syncDefaults(result, backup);
            DisplayData();
        }

        private void Reset()
        {
            result = backup;
            DisplayData();
        }

        private void SaveDefaults()
        {
            result = projectParameters.CollectData();
            backup = result;
        }
        #region Form Settup
        private void DisplayData()
        {
            this.mainPanel.Controls.Clear();
            this.offPanel.Controls.Clear();
            
            if (result.Count == 0)
            {
                this.mainContainer.Panel1.Controls.Add(WarningLabel("No active parameters"));
            }

            bool check = false;

            List<System.Windows.Forms.Control> items = new List<System.Windows.Forms.Control>();
            List<System.Windows.Forms.Control> checks = new List<System.Windows.Forms.Control>();

            foreach (FamilyEditorItem item in result)
            {
                if (item.getCheckbox().Count > 0)
                {
                    CheckBox chk = createCheckBox(item);
                    checks.Add(chk);
                    check = true;
                }
                if (item.getTrackbar().Count > 0)
                {
                    TrackBar tkr = createTrackBar(item);
                    items.Add(tkr);
                }
                if (item.getLabel().Count > 0)
                {
                    Label lbl = createLabel(item);
                    items.Add(lbl);
                }
                if (item.getTextbox().Count > 0)
                {
                    System.Windows.Forms.TextBox txt = createTextbox(item);
                    items.Add(txt);
                }

                this.mainPanel.Controls.AddRange(items.ToArray());
                this.offPanel.Controls.AddRange(checks.ToArray());

                items.Clear();
                checks.Clear();
            }


            if(!check)
            {
                this.offPanel.Controls.Add(error("This family has no Yes/No parameters."));
            }
        }

        private Label error(string message)
        {
            Label error = new Label();
            error.Text = message;
            error.TextAlign = ContentAlignment.MiddleCenter;
            error.AutoSize = true;
            error.Padding = new Padding(55, 35, 0, 0);
            error.ForeColor = System.Drawing.Color.DarkGray;

            return error;
        }
        private System.Windows.Forms.TextBox createTextbox(FamilyEditorItem item)
        {
            System.Windows.Forms.TextBox txt = new System.Windows.Forms.TextBox();
            
            string s = item.getTextbox().Keys.First();
            double d = item.getTextbox().Values.First();

            txt = new System.Windows.Forms.TextBox();
            txt.Size = new Size(34, 10);
            txt.KeyDown += new KeyEventHandler(textBox_KeyDown);
            txt.Name = s;
            txt.Text = Math.Round(Utils.trueValue(d), 2).ToString();
            txt.BackColor = System.Drawing.SystemColors.Control;
            txt.BorderStyle = BorderStyle.None;
            txt.Margin = new Padding(0, 5, 0, 0);
            txt.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            txt.Tag = item;

            return txt;
        }


        private Label createLabel(FamilyEditorItem item)
        {
            Label lbl = new Label();

            string s = item.getLabel().Keys.First();
            double d = item.getLabel().Values.First();

            lbl = new Label();
            lbl.AutoSize = true;
            lbl.MaximumSize = new Size(70, 0);
            lbl.Font = new Font("Arial", 8);
            lbl.Text = Utils.Truncate(s, 15);
            lbl.Name = s;
            lbl.Visible = true;
            lbl.Padding = new Padding(3, 4, 3, 3);
            lbl.Tag = item;

            if (d == 0)
            {
                lbl.ForeColor = System.Drawing.Color.LightGray;
            }

            return lbl;
        }

        private TrackBar createTrackBar(FamilyEditorItem item)
        {
            TrackBar trk = new TrackBar();

            string s = item.getTrackbar().Keys.First();
            double d = item.getTrackbar().Values.First();

            trk = new TrackBar();
            trk.Name = s;
            trk.Text = s;
            trk.Size = new Size(180, 10);
            trk.Padding = new Padding(3, 3, 3, 3);
            trk.Tag = item;

            if (d != 0)
            {
                trk.Maximum = Convert.ToInt32(d * 100) * 2;
                trk.Minimum = 1;
                trk.Value = Convert.ToInt32(d * 100);
                trk.TickFrequency = Convert.ToInt32(trk.Maximum * 0.05);
                trk.MouseUp += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
                trk.ValueChanged += new EventHandler(trackBar_ValueChanged);
            }
            else
            {
                trk.Enabled = false;
            }

            return trk;
        }


        private CheckBox createCheckBox(FamilyEditorItem item)
        {
            CheckBox chk = new CheckBox();

            string s = item.getCheckbox().Keys.First();
            double d = item.getCheckbox().Values.First();

            chk.Name = s;
            chk.Text = Utils.Truncate(s, 10);

            chk.Checked = Convert.ToInt32(d) == 1 ? true : false;
            chk.MouseUp += new System.Windows.Forms.MouseEventHandler(checkBox_MouseUp);
            chk.Click += new EventHandler(trackBar_ValueChanged);            
            chk.Padding = new Padding(3, 3, 3, 3);
            chk.Margin = new Padding(0, 0, 50, 5);
            chk.Tag = item;

            return chk;
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
        #endregion


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
        /// Helper method to assign label to an empty panel
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private System.Windows.Forms.Label WarningLabel(string p)
        {
            Label l = new Label();
            l.AutoSize = true;
            l.MaximumSize = new Size(90, 0);
            l.Location = new System.Drawing.Point(Convert.ToInt32(mainContainer.Width * 0.5 - l.Size.Width * 0.5), Convert.ToInt32(mainContainer.Height * 0.5 - l.Size.Height * 0.5));
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
                double first = Utils.convertValueTO((double)tbar.Value * 0.01);
                if (projectParameters.goUnits)
                {                    
                    System.Windows.Forms.TextBox box = mainPanel.Controls.OfType<System.Windows.Forms.TextBox>().Where(x => x.Tag.Equals(tbar.Tag)).Single();

                    box.Text = Math.Round(first, 2).ToString();   //trackbar to textbox value
                }
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
                double first = Convert.ToDouble(tbox.Text);
                int second = Convert.ToInt32(Utils.convertValueFROM(first * 100));

                TrackBar tbar = mainPanel.Controls.OfType<TrackBar>().Where(x => x.Tag.Equals(tbox.Tag)).Single();
                
                if (second < tbar.Maximum) tbar.Value = second;  //textbox to trackbar value
                else
                {
                    tbar.Maximum = second * 2;
                    tbar.Value = second;
                }
                MakeRequest(RequestId.SlideParam, new Tuple<string, double>(tbox.Name, (double)(Utils.convertValueFROM(Convert.ToDouble(tbox.Text)))));
            }
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
            handler.Request.Value(new List<Tuple<string, double>>() {value});
            handler.Request.Make(request);
            exEvent.Raise();
        }
        /// <summary>
        /// Similar to the MakeRequest method taking a single tuplet, 
        /// this one provides the whole List
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="value">The value.</param>
        private void MakeRequest(RequestId request, List<Tuple<string, double>> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(value);
            handler.Request.Make(request);
            exEvent.Raise();
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
            RecollectData();
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
            this.mainContainer.Panel1.Controls.Clear();
            this.mainContainer.Panel1.Controls.Add(this.mainPanel);
            this.mainContainer.Panel1.Controls.Add(this.maximizeIcon);
            if (!this.minimizedState) minimizeToggle();
            this.messageTriggered = false;
            this.DocumentChanged();
        }
        /// <summary>
        /// Trigers if no valid Family Document is available on Refresh Document
        /// </summary>
        private void InvalidDocument(Exception ex)
        {
            this.minimizedState = this.mainContainer.Panel2Collapsed;
            if (!this.mainContainer.Panel2Collapsed) minimizeToggle();
            this.mainContainer.Panel1.Controls.Clear();
            this.mainContainer.Panel1.Controls.Add(WarningLabel("Please, run in a Family Document"));
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
            mainContainer.Panel2Collapsed = !mainContainer.Panel2Collapsed;
            maximizeIcon.Image = (mainContainer.Panel2Collapsed) ? _imageMaxi : _imageMini;
        }
        /// <summary>
        /// Loads Defaults
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void defaultButton_Click(object sender, EventArgs e)
        {            
            if ((System.Windows.Forms.Control.ModifierKeys & Keys.Shift) != 0)
            {
                SaveDefaults();
            }
            else
            {
                List<Tuple<string, double>> value = projectParameters.GetValues(backup);
                MakeRequest(RequestId.SlideParam, value);
                Reset();
            }
        }
        private void defaultButton_KeyDown(object sender, KeyEventArgs e)
        {
            if ((System.Windows.Forms.Control.ModifierKeys & Keys.Shift) != 0)
            {
                defaultButton.ForeColor = System.Drawing.Color.DarkGray;
                defaultButton.Text = "Save";
            }
            else
            {
                defaultButton.ForeColor = System.Drawing.Color.Black;
                defaultButton.Text = "Default";
            }
        }
        private void defaultButton_KeyUp(object sender, KeyEventArgs e)
        {
            defaultButton.ForeColor = System.Drawing.Color.Black;
            defaultButton.Text = "Default";
        }
        /// <summary>
        ///   WakeUp -> enable all controls
        /// </summary>
        /// 
        internal void WakeUp()
        {
            //MessageBox.Show("You are in the Control.Wake up event.");
            //EnableCommands(true);
        }
        private void Interface_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys.Shift) != 0)
            {
                MessageBox.Show("Shift is pressed");
            }
        }
    }
}
