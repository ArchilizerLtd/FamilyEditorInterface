using Autodesk.Revit.DB;
using FamilyEditorInterface.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Resources.WPF.Model
{
    public static class ItemRetriever
    {
        //Returns a single FamilyParameterModel
        internal static FamilyParameterModel GetFamilyParameterModel(FamilyType ft, FamilyParameter fp, List<FamilyParameter> paramLabel, List<FamilyParameter> paramFormula)
        {
            FamilyParameterModel fpmodel = FamilyParameterItem(ft, fp, paramLabel, paramFormula);
            return fpmodel;
        }
        //Create a checkbox FamilyParameterModel item
        internal static FamilyParameterModel FamilyParameterItem(FamilyType ft, FamilyParameter fp, List<FamilyParameter> paramLabel, List<FamilyParameter> paramFormula)
        {
            //Collect data depending on the type of paramter
            FamilyParameterModel newItem = new FamilyParameterModel(); //Create new FamilyParamterModel
            newItem.Precision = Properties.Settings.Default.Precision;  //The precision set by the User in the Settings
            newItem.Name = fp.Definition.Name;  //The name of the Parameter
            newItem.StorageType = fp.StorageType;
            newItem.ParamType = ParameterType(fp);
            newItem.Value = GetParameterValue(ft, fp) ;  //The Value of the parameter (can be yes/no, double, integer, string, ...
            newItem.Type = fp.Definition.ParameterType.ToString();  //The parameter type
            newItem.Associated = !fp.AssociatedParameters.IsEmpty;    //If the parameter is being associated
            newItem.BuiltIn = fp.Id.IntegerValue < 0;
            newItem.Modifiable = fp.UserModifiable;
            newItem.Formula = fp.IsDeterminedByFormula;
            newItem.Label = paramLabel.Any(f => f.Id == fp.Id);
            newItem.Reporting = fp.IsReporting;
            newItem.UsedInFormula = paramFormula.Any(f => f.Id == fp.Id);
            newItem.ReadOnly = fp.IsReadOnly;
            newItem.Shared = fp.IsShared;
            newItem.TypeOrInstance = fp.IsInstance ? "Instance" : "Type";

            /*
            try
            {
                newItem.DisplayUnitType = fp.DisplayUnitType;
            }
            catch (Exception)
            {

            }
            */
            return newItem;
        }

        //Get the parameter value as Double
        private static double GetParameterValue(FamilyType ft, FamilyParameter fp)
        {
            double value = 0.0;

            switch (fp.StorageType)
            {
                case (StorageType.Double):
                    value = Convert.ToDouble(ft.AsDouble(fp));
                    break;
                case (StorageType.Integer):
                    value = Convert.ToDouble(ft.AsInteger(fp));
                    break;
                default:
                    break;
            }

            return value;
        }
        //Get the ParameterType
        private static ParamType ParameterType(FamilyParameter fp)
        {
            ParamType type;
            switch (fp.Definition.ParameterType)
            {
                case (Autodesk.Revit.DB.ParameterType.YesNo):
                    type = ParamType.YesNo;
                    break;
                case (Autodesk.Revit.DB.ParameterType.Angle):
                    type = ParamType.Angle;
                    break;
                case (Autodesk.Revit.DB.ParameterType.Area):
                    type = ParamType.Area;
                    break;
                case (Autodesk.Revit.DB.ParameterType.Material):
                    type = ParamType.Material;
                    break;
                default:
                    type = ParamType.Supported;
                    break;
            }

            return type;
        }
        /// <summary>
        /// 1: Value
        /// 2: Built-In
        /// 3: Yes/No
        /// </summary>
        /// <param name="famParamModels">The list of all family parameter models</param>
        /// <returns></returns>
        internal static Tuple<List<FamilyParameterModel>, List<FamilyParameterModel>, List<FamilyParameterModel>> GetParameters(IList<FamilyParameterModel> famParamModels)
        {
            List<FamilyParameterModel> l1 = new List<FamilyParameterModel>();
            List<FamilyParameterModel> l2 = new List<FamilyParameterModel>();
            List<FamilyParameterModel> l3 = new List<FamilyParameterModel>();

            foreach (var fpm in famParamModels)
            {
                if (fpm.ParamType == ParamType.YesNo) l3.Add(fpm);  //Yes/No    
                else if (fpm.BuiltIn == true) l2.Add(fpm);  //Built-In
                else l1.Add(fpm);   //Value
            }

            Tuple<List<FamilyParameterModel>, List<FamilyParameterModel>, List<FamilyParameterModel>> lists = new Tuple<List<FamilyParameterModel>, List<FamilyParameterModel>, List<FamilyParameterModel>>(l1, l2, l3);

            return lists;
        }
    }
}
