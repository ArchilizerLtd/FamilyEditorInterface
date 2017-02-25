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
using System.Text.RegularExpressions;

namespace FamilyEditorInterface
{
    public partial class Interface : System.Windows.Forms.Form
    {
        private readonly string title = "Family Parameters";
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
        private List<FamilyEditorItem> result, backup;
        private float scale_x, scale_y;

        // change label text fields
        Label labelBeingEdited = new Label();
        System.Windows.Forms.TextBox editWindow = new System.Windows.Forms.TextBox();
        bool editWindowActive = false;
        string storeOldLabelValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exEvent"></param>
        /// <param name="handler"></param>
        public Interface(UIApplication uiapp, Autodesk.Revit.UI.ExternalEvent exEvent, RequestHandler handler)
        {
            InitializeComponent();
            ImageLoad();

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(Screen.PrimaryScreen.WorkingArea.Width - 400, Convert.ToInt32(Screen.PrimaryScreen.WorkingArea.Height * 0.5 - this.Height * 0.5));
            this.exEvent = exEvent;
            this.handler = handler;
            this.uiapp = uiapp;
            this.uidoc = uiapp.ActiveUIDocument;
            this.doc = checkDoc(uidoc.Document);
            if (doc == null)
            {
                this.InvalidDocument();
                return;
            }
            Utils.Init(this.doc);
            this.projectParameters = new ProjectParameters(doc);
            this.Text = title + String.Format(" - {0}", Utils.Truncate(doc.Title, 10));
            this.result = this.projectParameters.CollectData();
            this.backup = this.projectParameters.CollectData();
            this.scale_x = this.CreateGraphics().DpiX / 100;
            this.scale_y = this.CreateGraphics().DpiY / 100;

            DisplayData();
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
                if (item.getCheckbox() != null)
                {
                    CheckBox chk = createCheckBox(item);
                    checks.Add(chk);
                    check = true;
                }
                if (item.getTrackbar() != null)
                {
                    TrackBar tkr = createTrackBar(item);
                    items.Add(tkr);
                }
                if (item.getLabel() != null)
                {
                    Label lbl = createLabel(item);
                    items.Add(lbl);
                }
                if (item.getTextbox() != null)
                {
                    System.Windows.Forms.TextBox txt = createTextbox(item);
                    items.Add(txt);
                }
                if (items.Count > 0)
                {
                    this.mainPanel.Controls.AddRange(items.ToArray());
                    this.mainPanel.SetFlowBreak(items.ToArray()[items.Count - 1], true);
                }
                if (checks.Count > 0)
                {
                    this.offPanel.Controls.AddRange(checks.ToArray());
                    this.offPanel.SetFlowBreak(checks.ToArray()[0], true);
                }

                items.Clear();
                checks.Clear();
            }


            if (!check)
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
            error.Padding = new Padding(Convert.ToInt32(scale_x * 55), 35, 0, 0); //check if it works
            error.ForeColor = System.Drawing.Color.DarkGray;

            return error;
        }
        private System.Windows.Forms.TextBox createTextbox(FamilyEditorItem item)
        {
            System.Windows.Forms.TextBox txt = new System.Windows.Forms.TextBox();
            
            string s = item.getTextbox().Item1;
            double d = item.getTextbox().Item2;

            txt = new System.Windows.Forms.TextBox();
            txt.Size = new Size(Convert.ToInt32(scale_x * 34), 10);
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
            
            string s = item.getLabel().Item1;
            double d = item.getLabel().Item2;

            lbl = new Label();
            lbl.AutoSize = true;
            lbl.MaximumSize = new Size(Convert.ToInt32(scale_x * 70), 0);
            lbl.Font = new Font("Arial", 8);
            lbl.Text = Utils.Truncate(s, 15);
            lbl.Name = s;
            lbl.Visible = true;
            lbl.Padding = new Padding(3, 4, 3, 3);
            lbl.Tag = item;
            lbl.Click += labelChange;
            lbl.TextChanged += paramNameChanged;

            if (d == 0)
            {
                lbl.ForeColor = System.Drawing.Color.LightGray;
            }

            return lbl;
        }
        
        private TrackBar createTrackBar(FamilyEditorItem item)
        {
            TrackBar trk = new TrackBar();
            
            string s = item.getTrackbar().Item1;
            double d = item.getTrackbar().Item2;

            trk = new TrackBar();
            trk.Name = s;
            trk.Text = s;
            trk.Size = new Size(Convert.ToInt32(scale_x * 180), 10);
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
            
            string s = item.getCheckbox().Item1;
            double d = item.getCheckbox().Item2;

            chk.Name = s;
            chk.Text = Utils.Truncate(s, 18);

            chk.Checked = Convert.ToInt32(d) == 1 ? true : false;
            chk.MouseUp += new System.Windows.Forms.MouseEventHandler(checkBox_MouseUp);
            chk.Click += new EventHandler(trackBar_ValueChanged);
            chk.Padding = new Padding(3, 3, 3, 3);
            chk.Margin = new Padding(0, 0, Convert.ToInt32(scale_x * 50), Convert.ToInt32(scale_y * 5));
            chk.Tag = item;

            return chk;
        }

        private void paramNameChanged(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            string newName = lbl.Text;
            MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(lbl.Name, newName));
            FamilyEditorItem item = result.First(x => x.Name.Equals(lbl.Name));
            item.Name = newName;
            FamilyItemsToFormElements(item);
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

        #region Form Action

        private Autodesk.Revit.DB.Document checkDoc(Autodesk.Revit.DB.Document document)
        {
            if (document.IsFamilyDocument)
            {
                return document;
            }
            else
            {
                return null;
            }
        }

        private void RecollectData()
        {
            result = projectParameters.CollectData();
            projectParameters.syncDefaults(result, backup);
            DisplayData();
        }
        internal void ThisDocumentChanged()
        {
            this.doc = uiapp.ActiveUIDocument.Document;
            Utils.Init(this.doc);
            this.projectParameters = new ProjectParameters(doc);
            this.Text = title + String.Format(" - {0}", Utils.Truncate(doc.Title, 10));
            this.result = this.projectParameters.CollectData();
            this.backup = this.projectParameters.CollectData();

            RecollectData();
        }
        /// <summary>
        /// Document Changed triggered event
        /// </summary>
        internal void DocumentChanged()
        {
            RecollectData();
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
            l.TextAlign = ContentAlignment.MiddleCenter;
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
                MakeRequest(RequestId.SlideParam, new Tuple<string, double>(tbar.Name, (double)tbar.Value * 0.01));
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
        /// Change Parameter Name
        /// </summary>
        /// <param name="request"></param>
        /// <param name="value"></param>
        private void MakeRequest(RequestId request, Tuple<string, string> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(new List<Tuple<string, string>>() { value });
            handler.Request.Make(request);
            exEvent.Raise();
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
        private void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.Value(new List<Tuple<string, double>>() { value });
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
        /// "Refresh Document" - Push update new document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshButton_Click(object sender, EventArgs e)
        {
            DocumentRefresh();
        }
        private void DocumentRefresh()
        {
            if (!validDocument()) return;
            try
            {
                if (sameDocument())
                {
                    this.DocumentChanged();
                }
            }
            catch (Exception)
            {
                this.InvalidDocument();
            }
        }
        private bool validDocument()
        {
            Autodesk.Revit.DB.Document doc = uiapp.ActiveUIDocument.Document;

            if (!doc.IsFamilyDocument)
            {
                this.InvalidDocument();
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// Check if we are still in the same document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        private bool sameDocument()
        {
            Autodesk.Revit.DB.Document doc = uiapp.ActiveUIDocument.Document;

            if (this.doc == null || !doc.Title.Equals(this.doc.Title))
            {
                this.ThisDocumentChanged();
                return false;
            }
            else return true;
        }
        /// <summary>
        /// Trigers if no valid Family Document is available on Refresh Document
        /// </summary>
        private void InvalidDocument()
        {
            if (!this.mainContainer.Panel2Collapsed) minimizeToggle();
            this.mainPanel.Controls.Clear();
            this.mainPanel.Controls.Add(error("Please, run in a Family Document"));
            this.doc = null;
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
            if (!validDocument()) return;
            if (sameDocument())
            {
                List<Tuple<string, double>> value = projectParameters.GetValues(backup);
                RestoreValues(backup);
                MakeRequest(RequestId.SlideParam, value);
                Reset();
            }
        }

        private void RestoreValues(List<FamilyEditorItem> backup)
        {
            foreach(var item in backup)
            {
                FamilyItemsToFormElements((FamilyEditorItem)item);
            }
        }
        
        private void FamilyItemsToFormElements(FamilyEditorItem item)
        {
            System.Windows.Forms.TextBox tbox = mainPanel.Controls.OfType<System.Windows.Forms.TextBox>().Where(x => x.Tag.Equals(item)).SingleOrDefault();
            if(tbox != null) tbox.Name = item.Name;
            System.Windows.Forms.TrackBar tbar = mainPanel.Controls.OfType<System.Windows.Forms.TrackBar>().Where(x => x.Tag.Equals(item)).SingleOrDefault();
            if(tbar != null)
            {
                tbar.Name = item.Name;
                tbar.Text = item.Name;
            }
            System.Windows.Forms.Label lbl = mainPanel.Controls.OfType<System.Windows.Forms.Label>().Where(x => x.Tag.Equals(item)).SingleOrDefault();
            if (lbl != null)
            {
                lbl.Name = item.Name;
                lbl.Text = Utils.Truncate(item.Name, 15);
            }
            System.Windows.Forms.CheckBox chk = mainPanel.Controls.OfType<System.Windows.Forms.CheckBox>().Where(x => x.Tag.Equals(item)).SingleOrDefault();
            if (chk != null)
            {
                chk.Name = item.Name;
                chk.Text = item.Name;
            }
        }
        /// <summary>
        /// Save Defaults
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (!validDocument()) return;
            if (sameDocument())
            {
                SaveDefaults();
            }
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
        #endregion

        #region Text Edit
        private void labelChange(object sender, EventArgs e)
        {
            Label label = sender as Label;
            if (editWindowActive) FinalizeEdit();
            PlaceEditWindowOverLabel(label);
            AssociateEditorWithLabel(label);
        }

        private void AssociateEditorWithLabel(Label label)
        {
            storeOldLabelValue = label.Text;
            editWindow.Text = label.Text;
            label.ForeColor = SystemColors.Control;
            labelBeingEdited = label;
        }
        private void PlaceEditWindowOverLabel(Label label)
        {
            editWindow.Location = new System.Drawing.Point(label.Location.X + mainContainer.Location.X + 5, label.Location.Y + mainContainer.Location.Y + 4);
            editWindow.Size = label.Size ;
            if (!Controls.Contains(editWindow)) Controls.Add(editWindow);
            editWindow.Visible = true;
            editWindow.BringToFront();
            editWindow.Focus();
            editWindowActive = true;
            editWindow.KeyUp += TextBoxKeyUp;
            editWindow.Leave += TextBoxLeave;
        }
        private void TextBoxLeave(object sender, EventArgs e)
        {
            FinalizeEdit();
        }
        private void FinalizeEdit()
        {
            labelBeingEdited.Text = editWindow.Text;
            labelBeingEdited.Focus();
            labelBeingEdited.ForeColor = SystemColors.ControlText;

            editWindow.Visible = false;
            editWindow.SendToBack();
            editWindowActive = false;
        }
        private void TextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FinalizeEdit();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Escape)
            {
                editWindow.Text = storeOldLabelValue;
                FinalizeEdit();
                e.Handled = true;
            }
        }
        #endregion
    }
}
