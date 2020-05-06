using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FamilyEditorInterface.Resources.WPF.ViewModel
{
    public static class LabelsAndArrays
    {
        /// <summary>
        /// A method that determins if a FamilyParameter is used as a driver for any Dimension
        /// </summary>
        /// <param name="doc">The current Family Document</param>
        /// <param name="paramLabel">Reference to the List of Family Parameters ot populate</param>
        internal static void GetDims(Document doc, ref List<FamilyParameter> paramLabel)
        {
            if (!doc.IsFamilyDocument) return;  //Expecting a Family Document
            List<Dimension> dimList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Dimensions)
                .WhereElementIsNotElementType()
                .Cast<Dimension>()
                .ToList();

            foreach (Dimension dim in dimList)
            {
                try
                {
                    if (dim.FamilyLabel != null) paramLabel.Add(dim.FamilyLabel);
                }
                catch (Exception)
                {

                }
            }
        }
        /// <summary>
        /// A method that determins if a FamilyParameter is used as a driver for an Array
        /// </summary>
        /// <param name="doc">The current Family Document</param>
        /// <param name="paramLabel">Reference to the List of Family Parameters ot populate</param>
        internal static void GetArrays(Document doc, ref List<FamilyParameter> paramLabel)
        {
            if (!doc.IsFamilyDocument) return;  //Expecting a Family Document
            List<BaseArray> arrays = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_IOSArrays)
                .WhereElementIsNotElementType()
                .Cast<BaseArray>()
                .ToList();

            foreach (BaseArray arr in arrays)
            {
                try
                {
                    if (arr.Label != null) paramLabel.Add(arr.Label);
                }
                catch (Exception)
                {

                }
            }
        }
        /// <summary>
        /// A method that populates a list of FamilyPrameters that are used inside Formulas
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="paramFormula"></param>
        internal static void GetFormulas(Document doc, ref List<FamilyParameter> paramFormula)
        {
            List<FamilyParameter> famParam = new List<FamilyParameter>();
            List<FamilyParameter> outList = new List<FamilyParameter>();

            foreach (FamilyParameter fp in doc.FamilyManager.Parameters)
            {
                famParam.Add(fp);   //Populate a list of FamilyParameters first
            }

            foreach (FamilyParameter fp in doc.FamilyManager.Parameters)
            {
                if (fp.Formula != null)
                {
                    //If the formula contains the name of the FamilyParameter, add it to the list
                    string formula = fp.Formula;
                    famParam.ForEach(x => { if (formula.Contains(x.Definition.Name)) outList.Add(x); });
                }
            }

            if(outList.Any())
            {
                paramFormula = outList; //If the list contains any elements, return it to the main 
            }
        }
    }
}
