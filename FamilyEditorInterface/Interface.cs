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
        private List<FamilyEditorItem> items;
        private float scale_x, scale_y;
        private System.Drawing.Color transparent = System.Drawing.Color.FromArgb(0, 120, 120, 120);
        private int precision;

        // change label text fields
        Label labelBeingEdited = new Label();
        System.Windows.Forms.TextBox editWindow = new System.Windows.Forms.TextBox();
        bool editWindowActive = false;
        string storeOldLabelValue;
        // change text box field
        System.Windows.Forms.TextBox textBoxBeingEdited = new System.Windows.Forms.TextBox();
        bool editTextBoxActive = false;

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
            this.Location = new System.Drawing.Point(Screen.PrimaryScreen.WorkingArea.Width - 600, Convert.ToInt32(Screen.PrimaryScreen.WorkingArea.Height * 0.5 - this.Height * 0.5));
            this.exEvent = exEvent;
            this.handler = handler;
            this.uiapp = uiapp;
            
            this.scale_x = this.CreateGraphics().DpiX / 100;
            this.scale_y = this.CreateGraphics().DpiY / 100;

            this.doc = ThisDocumentCollect();
            if(this.doc != null)
            {
                DisplayData();
            }
        }
        internal Autodesk.Revit.DB.Document ThisDocumentCollect()
        {
            Autodesk.Revit.DB.Document checkDoc = null;
            this.uidoc = uiapp.ActiveUIDocument;
            checkDoc = Utils.checkDoc(uidoc.Document);
            if (checkDoc == null)
            {
                this.InvalidDocument();
                return checkDoc;
            }
            Utils.Init(checkDoc);
            this.projectParameters = new ProjectParameters(checkDoc);
            this.Text = title + String.Format(" - {0}", Utils.Truncate(checkDoc.Title, 10));
            this.items = this.projectParameters.CollectData();
            return checkDoc;
        }
        internal void ThisDocumentChanged()
        {
            ThisDocumentCollect();
            DisplayData();
        }        
        /// <summary>
        /// Save Default Parameter Values
        /// </summary>
        private void SaveDefaults()
        {
            //items = projectParameters.CollectData();
            items.ForEach(x => x.SaveDefaults());
        }
        private void LoadDefaults()
        {
            MakeRequest(RequestId.RestoreAll, items.Select(x => x.Restore).ToList());
            items.ForEach(x => x.RestoreDefaults());
            DisplayData();
        }
        #region Form Settup
        private void DisplayData()
        {
            this.mainPanel.Controls.Clear();
            this.offPanel.Controls.Clear();

            if (this.items.Count == 0)
            {
                this.mainContainer.Panel1.Controls.Add(WarningLabel("No active parameters"));
            }

            bool check = false;

            List<System.Windows.Forms.Control> items = new List<System.Windows.Forms.Control>();
            List<System.Windows.Forms.Control> checks = new List<System.Windows.Forms.Control>();

            this.items = this.items.OrderBy(x => !x.Associated).ThenBy(x => x.Name).ToList();

            foreach (FamilyEditorItem item in this.items)
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
            error.Padding = new Padding(Convert.ToInt32(scale_x * 55), 25, 0, 0); //check if it works
            error.ForeColor = System.Drawing.Color.DarkGray;

            return error;
        }
        private System.Windows.Forms.TextBox createTextbox(FamilyEditorItem item)
        {
            System.Windows.Forms.TextBox txt = new System.Windows.Forms.TextBox();
            
            string s = item.getTextbox().Item1;
            string d = item.getTextbox().Item2;
            
            txt.Size = new Size(Convert.ToInt32(scale_x * 34), 10);
            txt.KeyDown += new KeyEventHandler(textBox_KeyDown);
            txt.LostFocus += textBoxLostFocus;
            txt.TextChanged += textBoxEdited;
            txt.Name = s;
            txt.Text = d;
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

            // Inactive parameter
            if (!item.Associated)
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
            
            trk.Name = s;
            trk.Text = s;
            trk.Size = new Size(Convert.ToInt32(scale_x * 180), 10);
            trk.Padding = new Padding(3, 3, 3, 3);
            trk.Tag = item;

            if (item.Associated)
            {
                trk.Maximum = item.BarValue == 0 ? 100 : item.BarValue * 2;
                trk.Minimum = 0;
                trk.Value = item.BarValue;
                trk.TickFrequency = Convert.ToInt32(trk.Maximum * 0.05);
                trk.MouseUp += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
                trk.MouseWheel += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
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

            // Inactive parameter
            if (!item.Associated)
            {
                chk.Enabled = false;
                chk.ForeColor = System.Drawing.Color.LightGray;
            }

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

        #region Form Action

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
        /// On Parameter Name Change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void paramNameChanged(object sender, EventArgs e)
        {
            Label lbl = sender as Label;
            FamilyEditorItem item = lbl.Tag as FamilyEditorItem;
            if (!Utils.UnallowedChacarcters(lbl.Name))
            {
                lbl.Name = storeOldLabelValue;
                return;
            }
            string newName = lbl.Name;            
            // First update the Revit Parameter name 
            // Second update the item
            // Third update all other controls connected with this paramter/familyeditoritem
            MakeRequest(RequestId.ChangeParamName, new Tuple<string, string>(item.Name, newName));
            item.Name = newName;
            FamilyItemsToFormElements(item);
        }
        /// <summary>
        /// On update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar_MouseUp(object sender, MouseEventArgs e)
        {            
            if (user_done_updating)
            {
                user_done_updating = false;

                TrackBar tbar = sender as TrackBar;
                FamilyEditorItem item = tbar.Tag as FamilyEditorItem;

                item.BarValue = tbar.Value;

                if (projectParameters.goUnits)
                {
                    System.Windows.Forms.TextBox box = mainPanel.Controls.OfType<System.Windows.Forms.TextBox>().Where(x => x.Tag.Equals(tbar.Tag)).Single();

                    box.Text = item.BoxValue;   //trackbar to textbox value
                }
                MakeRequest(RequestId.SlideParam, item.Request);
            }
        }
        /// <summary>
        /// On update for checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (user_done_updating)
            {
                user_done_updating = false;

                CheckBox cbox = sender as CheckBox;
                FamilyEditorItem item = cbox.Tag as FamilyEditorItem;

                item.CheckValue = cbox.Checked;

                MakeRequest(RequestId.SlideParam, item.Request);
            }
        }
        private void textBoxEdited(object sender, EventArgs e)
        {
            if (!editTextBoxActive)
            {
                textBoxBeingEdited = sender as System.Windows.Forms.TextBox;
                editTextBoxActive = true;
            }
        }

        private void textBoxLostFocus(object sender, EventArgs e)
        {
            if (editTextBoxActive)
            {
                editTextBoxActive = false;
                FinalizeTextBoxEdit();
            }
        }

        private void FinalizeTextBoxEdit()
        {
            System.Windows.Forms.TextBox tbox = textBoxBeingEdited;
            FamilyEditorItem item = tbox.Tag as FamilyEditorItem;

            item.BoxValue = tbox.Text;
            try
            {
                TrackBar tbar = mainPanel.Controls.OfType<TrackBar>().Where(x => x.Tag.Equals(tbox.Tag)).Single();

                if (item.BarValue < tbar.Maximum) tbar.Value = item.BarValue;  //textbox to trackbar value
                else
                {
                    tbar.Maximum = item.BarValue * 2;
                    tbar.Value = item.BarValue;
                }
                MakeRequest(RequestId.SlideParam, item.Request);
            }
            catch(Exception)
            {

            }
        }
        /// <summary>
        /// On update for textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if(editTextBoxActive)
                {
                    editTextBoxActive = false;
                    FinalizeTextBoxEdit();
                }
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
        private void MakeRequest(RequestId request, Tuple<string, string> renameValue)
        {
            //MessageBox.Show("You are in the Control.Request event.");
            handler.Request.RenameValue(new List<Tuple<string, string>>() { renameValue });
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
                    this.DisplayData();
                }
                else
                {
                    this.ThisDocumentChanged();
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
        private void FamilyItemsToFormElements(FamilyEditorItem item)
        {
            System.Windows.Forms.TextBox tbox = mainPanel.Controls.OfType<System.Windows.Forms.TextBox>().Where(x => x.Tag.Equals(item)).SingleOrDefault();
            if (tbox != null)
            {
                tbox.Name = item.Name;
            }
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
                DisplayData();
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
            FamilyEditorItem item = label.Tag as FamilyEditorItem;
            if (item.BuiltIn)
            {
                DialogResult dialogResult = MessageBox.Show(String.Format("Can't edit or delete BuiltInParameter"), "Error.", MessageBoxButtons.OK);
                return;
            }
            if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
            {
                DeleteItem(label);
            }
            else
            {
                if (editWindowActive)
                {
                    FinalizeEdit();
                    return;
                }
                else
                {
                    PlaceEditWindowOverLabel(label);
                    AssociateEditorWithLabel(label);
                    DisableControls();
                }
            }
        }
        private void DeleteItem(Label lbl)
        {
            DialogResult dialogResult = MessageBox.Show(String.Format("Are you sure you want to delete {0} parameter?", lbl.Text), "Delete confirmation.", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                FamilyEditorItem item = lbl.Tag as FamilyEditorItem;
                MakeRequest(RequestId.DeleteId, item.Name);
                items.Remove(item);
            }
            DocumentRefresh();
        }
        private void AssociateEditorWithLabel(Label label)
        {
            storeOldLabelValue = label.Name;
            editWindow.Text = label.Name;
            label.ForeColor = transparent;
            labelBeingEdited = label;
        }
        private void PlaceEditWindowOverLabel(Label label)
        {
            editWindow.Location = new System.Drawing.Point(label.Location.X + mainContainer.Location.X + 15, label.Location.Y + mainContainer.Location.Y + 17);
            editWindow.Size = label.Size;
            if (!Controls.Contains(editWindow)) Controls.Add(editWindow);
            editWindow.Visible = true;
            editWindow.BringToFront();
            editWindow.Focus();
            editWindowActive = true;
            editWindow.KeyUp += TextBoxKeyUp;
            editWindow.Leave += TextBoxLeave;
            editWindow.LostFocus += TextBoxLeave;
        }
        private void TextBoxLeave(object sender, EventArgs e)
        {
            if (editWindowActive) FinalizeEdit();
        }
        private void FinalizeEdit()
        {
            editWindowActive = false;
            labelBeingEdited.Name = items.Any(x => x.Name.Equals(editWindow.Text)) ? storeOldLabelValue : editWindow.Text;
            labelBeingEdited.Text = Utils.Truncate(labelBeingEdited.Name, 15);
            labelBeingEdited.Focus();
            labelBeingEdited.ForeColor = SystemColors.ControlText;
            
            editWindow.SendToBack();
            EnableControls();
            this.DisplayData();
        }
        /// <summary>
        /// Disable the main canvas while renaming parameter
        /// </summary>
        private void DisableControls()
        {
            mainContainer.Enabled = false;
        }
        /// <summary>
        /// Enable the main canvas after finishing with the renaming of the parameter
        /// </summary>
        private void EnableControls()
        {
            mainContainer.Enabled = true;
        }
        /// <summary>
        /// Opens the Settings Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setButton_Click(object sender, EventArgs e)
        {
            using (Settings.Settings set = new Settings.Settings())
            {
                set.Precision = this.precision;
                var dialogResult = set.ShowDialog();
                if(dialogResult == DialogResult.OK)
                {
                    ChangePrecision(set.Precision);
                    this.precision = set.Precision;
                }
            }
        }
        /// <summary>
        /// Sets the precision for each parameter value
        /// </summary>
        /// <param name="precision"></param>
        private void ChangePrecision(int precision)
        {
            foreach(FamilyEditorItem item in this.items)
            {
                item.Precision = precision;
            }
        }
        private void TextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (editWindowActive) FinalizeEdit();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Escape)
            {
                editWindow.Text = storeOldLabelValue;
                if (editWindowActive) FinalizeEdit();
                e.Handled = true;
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DocumentRefresh();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (!validDocument()) return;
            if (sameDocument())
            {
                LoadDefaults();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!validDocument()) return;
            if (sameDocument())
            {
                SaveDefaults();
                DisplayData();
            }
        }

        // Escape button - doesn't work here?!
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion
    }
}
