﻿using Autodesk.Revit.DB;
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
            newItem.SuppressUpdate();   //When setting the first time, suppress the back-update coming from the UI because of difference in precision
            newItem.Name = fp.Definition.Name;  //The name of the Parameter
            newItem.StorageType = fp.StorageType; 
            newItem.ParamType = GetParameterType(fp);
            newItem.DisplayUnitType = GetDisplayUnitType(fp);   //Set the DisplayUnitType for this parameter
            newItem.Precision = Utils.GetPrecision(newItem.DisplayUnitType);   //Properties.Settings.Default.Precision;  //The precision set by the User in the Settings
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
            newItem.IsUsed = (newItem.Associated || newItem.Label || newItem.UsedInFormula);    //Used if any of the three is true
            newItem.Editable = newItem.IsUsed && !newItem.Formula && !newItem.Reporting; //If the Parameter is used or if the parameter is not defined by Formula, allow the user to edit
            newItem.Visible = Properties.Settings.Default.ToggleVisibility; //Set the visibility in the UI (user defined Tag property for ALL Tags, regardless of their specific conditions)
            newItem.TagVisible = Properties.Settings.Default.ToggleTagsVisibility; //Set the Tags visibility 
            newItem.UIValue = Math.Round(Utils.GetDutValueTo(newItem.StorageType, newItem.DisplayUnitType, GetParameterValue(ft, fp)),newItem.Precision);  //The Value of the parameter (can be yes/no, double, integer, string, ...)

            return newItem;
        }
        //Get the Display Unit Type
        private static DisplayUnitType GetDisplayUnitType(FamilyParameter fp)
        {
            try
            {
                var dut = fp.DisplayUnitType;
                return dut;
            }
            catch (Exception)
            {
                return DisplayUnitType.DUT_MILLIMETERS;
            }
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
                case (StorageType.String):
                    value = 0.0;
                    break;
                case (StorageType.ElementId):
                    value = 0.0;
                    break;
                default:
                    break;
            }

            return value;
        }
        //Get the ParameterType
        private static ParamType GetParameterType(FamilyParameter fp)
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
                case (Autodesk.Revit.DB.ParameterType.Length):
                    type = ParamType.Length;
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
                if (fpm.StorageType != StorageType.Double && fpm.StorageType != StorageType.Integer) continue;  //Skip anything that isn't numerical
                if (fpm.ParamType == ParamType.YesNo) l3.Add(fpm);  //Yes/No    
                else if (fpm.BuiltIn == true) l2.Add(fpm);  //Built-In
                else l1.Add(fpm);   //Value
            }

            Tuple<List<FamilyParameterModel>, List<FamilyParameterModel>, List<FamilyParameterModel>> lists = new Tuple<List<FamilyParameterModel>, List<FamilyParameterModel>, List<FamilyParameterModel>>(l1, l2, l3);

            return lists;
        }
    }
}
