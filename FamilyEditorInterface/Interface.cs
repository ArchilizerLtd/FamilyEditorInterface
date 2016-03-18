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
        private FamilyManager familyManager;
        private int vOffset = 42;
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
            this.familyManager = doc.FamilyManager;
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
            return this.doc;
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
            if (fp.UserModifiable)
            {
                return false;
            }
            if (fp.IsDeterminedByFormula)
            {
                return false;
            }
            if (!ft.HasValue(fp))
            {
                return false;
            }
            if (ft.AsDouble(fp) == 0 || ft.AsDouble(fp) == null)
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
                Command.global_message =
                  "Please run this command in a family document.";

                TaskDialog.Show("Message", Command.global_message);
            }
            
            FamilyType familyType = familyManager.CurrentType;

            this.familyManager = doc.FamilyManager;
            this.panel1.Controls.Clear();

            int index = 0;
            double value = 0.0;

            TrackBar[] track = new TrackBar[familyManager.Parameters.Size];
            Label[] label = new Label[familyManager.Parameters.Size];

            famParam.Clear();

            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                if (!famEdit(fp, familyType)) continue;

                famParam.Add(fp.Definition.Name, fp);
            }
            //sort by name

            //add controlls
            List<ElementId> eId = new List<ElementId>();

            foreach(FamilyParameter fp in famParam.Values)
            {
                //if (fp.StorageType == StorageType.Double) value = UnitUtils.ConvertFromInternalUnits(Convert.ToDouble(familyType.AsDouble(fp)), DisplayUnitType.DUT_MILLIMETERS);
                //else if (fp.StorageType == StorageType.Integer) value = UnitUtils.ConvertFromInternalUnits(Convert.ToDouble(familyType.AsInteger(fp)), DisplayUnitType.DUT_MILLIMETERS);
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);

                track[index] = new TrackBar();
                track[index].Name = fp.Definition.Name;
                track[index].Text = fp.Definition.Name;
                track[index].Location = new System.Drawing.Point(5, 15 + index * vOffset);
                track[index].Size = new Size(180, 8);
                track[index].Maximum = Convert.ToInt32(value * 100) * 2;
                track[index].Minimum = 1;
                track[index].Value = Convert.ToInt32(value*100);
                track[index].TickFrequency = Convert.ToInt32(track[index].Maximum * 0.05);
                track[index].MouseUp += new System.Windows.Forms.MouseEventHandler(trackBar_MouseUp);
                track[index].ValueChanged += new EventHandler(trackBar_ValueChanged);
                track[index].Tag = index;
                label[index] = new Label();
                label[index].Size = new Size(100, 30);
                label[index].Font = new Font("Arial", 8);
                label[index].Location = new System.Drawing.Point(200, 15 + index * vOffset);
                //label[index].Text =  String.Format("{0}: 0 to {1}", fp.Definition.Name, track[index].Maximum);
                label[index].Text = fp.Definition.Name;
                label[index].Name = fp.Definition.Name;
                //label[index].Text = fp.StorageType.ToString();
                //label[index].Name = fp.Definition.ParameterType.ToString();
                label[index].Visible = true;

                index++;
            }

            this.panel1.Controls.AddRange(track);
            this.panel1.Controls.AddRange(label);
        }
        
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
        /// Push update new document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            Autodesk.Revit.DB.Document doc = uiapp.ActiveUIDocument.Document;
            if (!doc.Equals(this.Document()))
            {
                this.Document(doc);
                this.DocumentChanged();
            }
            DocumentChanged();
        }
    }
}
