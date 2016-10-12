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
            /*
        else if (!ft.HasValue(fp))
        {
            return false;
        }
        else if (ft.AsDouble(fp) == null && ft.AsInteger(fp) == null)
        {
            return false;
        }
             * */
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
        internal List<FamilyEditorItem> CollectData()
        {
            List<FamilyEditorItem> result = new List<FamilyEditorItem>();

            if (!doc.IsFamilyDocument)
            {
                //Command.global_message =
                //  "Please run this command in a family document.";

                //TaskDialog.Show("Message", Command.global_message);
            }

            // TO DO: fiugre out how to get family parameter value without existing family type

            FamilyManager familyManager = doc.FamilyManager;
            FamilyType familyType = familyManager.CurrentType;
                     
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
            
            foreach (FamilyParameter fp in famParam.Values)
            {
                ///yes-no parameters
                if (fp.Definition.ParameterType.Equals(ParameterType.YesNo))
                {
                    if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                    
                    eId.Add(fp.Id);

                    FamilyEditorItem newItem = new FamilyEditorItem();
                    newItem.addCheckbox(fp.Definition.Name, value);

                    result.Add(newItem);

                    
                    indexChk++;
                    continue;
                }
                try
                {
                    ///slider parameters
                    if (fp.StorageType == StorageType.Double) value = Convert.ToDouble(familyType.AsDouble(fp));
                    else if (fp.StorageType == StorageType.Integer) value = Convert.ToDouble(familyType.AsInteger(fp));
                    eId.Add(fp.Id);
                }
                catch
                {
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
                        if (familyType == null)
                        {

                            using (Transaction t = new Transaction(doc, "Create Family Type"))
                            {
                                t.Start();
                                familyManager.NewType("Default");
                                t.Commit();
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Aborted by user.");
                    }
                }
                

                //DisplayUnitType dut = this.doc.GetUnits().GetDisplayUnitType();
                goUnits = Utils._goUnits();

                if (fp.Definition.ParameterType == ParameterType.Length)
                {
                    
                }
                if (value != 0)
                {
                    FamilyEditorItem newItem = new FamilyEditorItem();
                    newItem.addTrackbar(fp.Definition.Name, value);
                    newItem.addLabel(fp.Definition.Name, value);

                    //some units are not supported yet
                    if (goUnits)
                    {
                        newItem.addTextbox(fp.Definition.Name, value);
                    }

                    result.Add(newItem);
                }
                else
                {
                    FamilyEditorItem newItem = new FamilyEditorItem();
                    newItem.addTrackbar(fp.Definition.Name, value);
                    newItem.addLabel(fp.Definition.Name, value);

                    result.Add(newItem);
                }
            }
            sort(result);
            return result;
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
                if (!backup.Any(x => x.Name().Equals(item.Name()))) backup.Add(item);
            }
            foreach (var item in backup)
            {
                if (!result.Any(x => x.Name().Equals(item.Name()))) backup.Remove(item);
            }
            sort(backup);
        }
        /// <summary>
        /// Sort the list by names
        /// </summary>
        /// <param name="backup"></param>
        private void sort(List<FamilyEditorItem> list)
        {
            list = list.OrderBy(x => x.Name()).ToList();
        }
        /// <summary>
        /// Transforms list of items to list of tuplets
        /// Used for returning default values to the project parameters
        /// </summary>
        /// <param name="backup"></param>
        /// <returns></returns>
        internal List<Tuple<string, double>> GetValues(List<FamilyEditorItem> backup)
        {
            List<Tuple<string, double>> value = new List<Tuple<string, double>>();
            foreach(var item in backup)
            {
                value.Add(new Tuple<string, double>(item.Name(), item.Value()));
            }
            return value;
        }
    }
}
