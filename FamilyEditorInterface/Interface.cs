using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private int vOffset = 50;
        private SortedList<string, FamilyParameter> famParam;


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
            if (!fp.StorageType.ToString().Equals("Double") && !fp.StorageType.ToString().Equals("Integer"))
            {
                return false;
            }
            else if (fp.UserModifiable)
            {
                return false;
            }
            else if (fp.IsDeterminedByFormula)
            {
                return false;
            }
            else if (!ft.HasValue(fp))
            {
                return false;
            }
            else if (ft.AsDouble(fp) == null)
            {
                return false;
            }
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
            
            this.panel1.Controls.Clear();

            int index = 0;
            double value = 0.0;

            TrackBar[] track = new TrackBar[familyManager.Parameters.Size];
            Label[] label = new Label[familyManager.Parameters.Size];

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
                panel1.Controls.Add(WarningLabel("No active parameters"));
            }

            foreach(FamilyParameter fp in famParam.Values)
            {
                //if (fp.StorageType == StorageType.Double) value = UnitUtils.ConvertFromInternalUnits(Convert.ToDouble(familyType.AsDouble(fp)), DisplayUnitType.DUT_MILLIMETERS);
                //else if (fp.StorageType == StorageType.Integer) value = UnitUtils.ConvertFromInternalUnits(Convert.ToDouble(familyType.AsInteger(fp)), DisplayUnitType.DUT_MILLIMETERS);
                
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);
                if (value != 0)
                {
                    track[index] = new TrackBar();
                    track[index].Name = fp.Definition.Name;
                    track[index].Text = fp.Definition.Name;
                    track[index].Location = new System.Drawing.Point(5, 15 + index * vOffset);
                    track[index].Size = new Size(180, 10);
                    track[index].Maximum = Convert.ToInt32(value * 100) * 2;
                    track[index].Minimum = 1;
                    track[index].Value = Convert.ToInt32(value * 100);
                    track[index].TickFrequency = Convert.ToInt32(track[index].Maximum * 0.05);
                    track[index].MouseUp += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
                    track[index].ValueChanged += new EventHandler(trackBar_ValueChanged);
                    track[index].Tag = index;
                    label[index] = new Label();
                    label[index].AutoSize = true;
                    label[index].MaximumSize = new Size(100, 0);
                    label[index].Font = new Font("Arial", 8);
                    label[index].Location = new System.Drawing.Point(200, 15 + index * vOffset);
                    //label[index].Text =  String.Format("{0}: 0 to {1}", fp.Definition.Name, track[index].Maximum);
                    label[index].Text = fp.Definition.Name;
                    label[index].Name = fp.Definition.Name;
                    //label[index].ForeColor = System.Drawing.Color.LightGray;
                    label[index].Visible = true;
                    index++;
                }
                else
                {
                    track[index] = new TrackBar();
                    track[index].Name = fp.Definition.Name;
                    track[index].Text = fp.Definition.Name;
                    track[index].Location = new System.Drawing.Point(5, 15 + index * vOffset);
                    track[index].Size = new Size(180, 10);
                    track[index].Tag = index;
                    track[index].Enabled = false;
                    label[index] = new Label();
                    label[index].AutoSize = true;
                    label[index].MaximumSize = new Size(100, 0);
                    label[index].Font = new Font("Arial", 8);
                    label[index].ForeColor = System.Drawing.Color.LightGray;
                    label[index].Location = new System.Drawing.Point(200, 15 + index * vOffset);
                    label[index].Text = fp.Definition.Name;
                    label[index].Name = fp.Definition.Name;
                    label[index].Visible = true;

                    index++;
                }
            }

            if (track.Length > 0) this.panel1.Controls.AddRange(track);
            if (label.Length > 0) this.panel1.Controls.AddRange(label);
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
            l.Location = new System.Drawing.Point(Convert.ToInt32(panel1.Width * 0.5 - l.Size.Width * 0.5), Convert.ToInt32(panel1.Height * 0.5 - l.Size.Height * 0.5));
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
                MakeRequest(RequestId.SlideParam, new Tuple<string, double> (tbar.Text, (double)tbar.Value));
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
                foreach(ElementId eId in changedId)
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

                if (this.Document() != null && !doc.Equals(this.Document()))
                {
                    this.Document(doc);
                    this.DocumentChanged();
                }
                else if (this.Document() == null && doc.IsValidObject && doc.IsFamilyDocument)
                {
                    this.Document(doc);
                    this.DocumentChanged();
                }
                else
                {
                    this.DocumentChanged();
                }
            }
            catch (Exception ex)
            {
                this.InvalidDocument(ex);
            }
        }
        /// <summary>
        /// Trigers if no valid Family Document is available on Refresh Document
        /// </summary>
        private void InvalidDocument(Exception ex)
        {
            this.panel1.Controls.Clear();
            this.panel1.Controls.Add(WarningLabel("Please, run in a Family Document"));
        }
    }
}
