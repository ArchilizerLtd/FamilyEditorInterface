using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyEditorInterface
{
    internal class ProjectParameters
    {
        private Document doc;
        private SortedList<string, FamilyParameter> famParam;
        internal bool load = false;
        public bool goUnits { get; private set; }
             

        internal ProjectParameters(Document doc)
        {
            this.doc = doc;
            this.famParam = new SortedList<string, FamilyParameter>();
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
            else if (fp.IsDeterminedByFormula || fp.Formula != null)
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
        internal List<FamilyEditorItem> CollectData()
        {
            List<FamilyEditorItem> collectList = new List<FamilyEditorItem>();
            
            FamilyManager familyManager = doc.FamilyManager;
            FamilyType familyType = familyManager.CurrentType;
                     
            if (familyType == null)
            {
                familyType = CreateDefaultFamilyType(familyManager);
            }

            int indexChk = 0;
            double value = 0.0;
            
            famParam.Clear();

            foreach (FamilyParameter fp in familyManager.Parameters)
            {
                if (!famEdit(fp, familyType)) continue;
                else famParam.Add(fp.Definition.Name, fp);
            }           

            //add controlls
            List<ElementId> eId = new List<ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Dimension> dimList = collector
                .OfCategory(BuiltInCategory.OST_Dimensions)
                .WhereElementIsNotElementType()
                .Cast<Dimension>()
                .ToList();

            List<FamilyParameter> paramUsed = new List<FamilyParameter>();

            foreach(Dimension dim in dimList)
            {
                try
                {
                    if (dim.FamilyLabel != null) paramUsed.Add(dim.FamilyLabel);
                }
                catch(Exception)
                {

                }
            }

            foreach (FamilyParameter fp in famParam.Values)
            {
                bool associated = !fp.AssociatedParameters.IsEmpty || paramUsed.Any(x => x.Definition.Name.Equals(fp.Definition.Name));
                ///yes-no parameters
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                    
                    eId.Add(fp.Id);

                    FamilyEditorItem newItem = new FamilyEditorItem(); // collect data yes-no
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)
                    newItem.addCheckbox();         

                    collectList.Add(newItem);
                    
                    indexChk++;
                    continue;
                }
                ///slider parameters
                if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                eId.Add(fp.Id);
                              
                //DisplayUnitType dut = this.doc.GetUnits().GetDisplayUnitType();
                goUnits = Utils._goUnits();
                
                if (associated)
                {
                    FamilyEditorItem newItem = new FamilyEditorItem();  // collect data slider, value != 0
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)

                    newItem.addTrackbar();
                    newItem.addLabel();

                    //some units are not supported yet
                    //only if units are supported, add a text box
                    if (goUnits)
                    {
                        newItem.addTextbox();
                    }

                    collectList.Add(newItem);
                }
                else
                {
                    FamilyEditorItem newItem = new FamilyEditorItem(); // collect data slider, value == 0
                    newItem.Precision = Properties.Settings.Default.Precision;
                    newItem.Name = fp.Definition.Name;
                    newItem.Value = value;
                    newItem.Type = fp.Definition.ParameterType.ToString();
                    newItem.Associated = associated;
                    newItem.BuiltIn = fp.Id.IntegerValue < 0;
                    newItem.Shared = fp.IsShared;
                    newItem.Visible = !(newItem.BuiltIn && !Properties.Settings.Default.SystemParameters); // if it's a built-in parameter and built-in parameters are hidden from settings, hide (false)

                    newItem.addTrackbar();
                    newItem.addLabel();

                    collectList.Add(newItem);
                }
            }
            sort(collectList);
            return collectList;
        }
        /// <summary>
        /// Creates default family type.
        /// </summary>
        /// <param name="familyManager">The family manager.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Aborted by user.</exception>
        private FamilyType CreateDefaultFamilyType(FamilyManager familyManager)
        {
            FamilyType familyType = null;

            TaskDialog td = new TaskDialog("No Family Type");
            td.MainInstruction = "Create Default Family Type.";
            string s = "This might be a new Family with no existing Parameters or Family Types." + Environment.NewLine + Environment.NewLine +
            "In order to use this plugin, you can either create a new Parameter/Family Type from the Family Types Dialog" +
                " and restart the plugin or create a Default Family Type by accepting this message." + Environment.NewLine + Environment.NewLine +
                    "You can always delete the Default Family Parameter later.";
            td.MainContent = s;
            td.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

            TaskDialogResult tResult = td.Show();

            if (TaskDialogResult.Yes == tResult)
            {
                using (Transaction t = new Transaction(doc, "Create Family Type"))
                {
                    t.Start();
                    familyType = familyManager.NewType("Default");
                    t.Commit();
                }
            }
            else
            {
                throw new Exception("Aborted by user.");
            }

            return familyType;
        }
        /// <summary>
        /// Synchronizes the default values.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="backup">The backup.</param>
        internal void syncDefaults(List<FamilyEditorItem> result, List<FamilyEditorItem> backup)
        {
            foreach (FamilyEditorItem item in result)
            {
                if (!backup.Any(x => x.Name.Equals(item.Name))) backup.Add(item);
            }
            foreach (var item in backup)
            {
                if (!result.Any(x => x.Name.Equals(item.Name))) backup.Remove(item);
            }
            sort(backup);
        }
        /// <summary>
        /// Sort the list by names
        /// </summary>
        /// <param name="backup"></param>
        private void sort(List<FamilyEditorItem> list)
        {
            list = list.OrderBy(x => !x.Associated).ThenBy(x => x.Name).ToList();
        }
    }
}
