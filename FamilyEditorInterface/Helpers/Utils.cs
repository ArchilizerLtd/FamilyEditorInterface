using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Dialog.Alerts;
using Dialog.Service;
using FamilyEditorInterface.Dialog.Alerts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FamilyEditorInterface
{

    #region Random
    public class SingleRandom : Random
    {
        static SingleRandom _Instance;
        public static SingleRandom Instance
        {
            get
            {
                if (_Instance == null) _Instance = new SingleRandom();
                return _Instance;
            }
        }

        private SingleRandom() { }
    }
    #endregion

    public static class Utils
    {
        private static DisplayUnitType DUT;
        public const double METERS_IN_FEET = 0.3048;

        public static void InitializeUnits(Document doc)
        {
            DUT = doc.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits;
            if(!_goUnits())
            {
                NotSupported(DUT);
                DUT = DisplayUnitType.DUT_DECIMAL_FEET;
            }
        }
        /// <summary>
        /// UI to Revit internal unit values
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double convertValueTO(double p)
        {
            return GetDutValueTo(DUT, p);
        }
        /// <summary>
        /// Revit to UI units
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double convertValueFROM(double p)
        {
            return GetDutValueFrom(DUT, p);
        }
        /// <summary>
        /// Checks if the document is a Family Document and issues a Warning if it isn't
        /// </summary>
        /// <param name="doc"></param>
        internal static bool CheckFamilyDocument(Document doc)
        {
            if (doc.IsFamilyDocument) return true;
            else
            {
                DialogUtils.Alert("Warning", "Not a Family Document.");
                return false;
            }
        }
        /// <summary>
        /// Checks if a parameter with the same name eixsts in a family
        /// </summary>
        /// <param name="famParam">FamilyParameter to check</param>
        /// <param name="familyInstance">A family instance to check against</param>
        /// <returns></returns>
        internal static bool ParameterExist(FamilyParameter famParam, FamilyInstance familyInstance)
        {
            foreach(Parameter param in familyInstance.Parameters)
            {
                if (famParam.Definition.Name.Equals(param.Definition.Name)) return true;
            }
            foreach (Parameter param in familyInstance.Symbol.Parameters)
            {
                if (famParam.Definition.Name.Equals(param.Definition.Name)) return true;
            }
            return false;
        }       
        /// <summary>
        /// Checks if a parameter with the same name eixsts in a family
        /// </summary>
        /// <param name="famParam">FamilyParameter to check</param>
        /// <param name="familyInstance">The parameterset containing all family parameters</param>
        /// <returns></returns>
        internal static bool ParameterExist(FamilyParameter famParam, FamilyParameterSet parameters)
        {
            foreach(FamilyParameter param in parameters)
            {
                if (famParam.Definition.Name.Equals(param.Definition.Name)) return true;
            }
            return false;
        }
        /// <summary>
        /// Executes Revit Selection.PickObject and catches user cancel exception
        /// </summary>
        /// <param name="uidoc">Current UI Document</param>
        /// <param name="message">The message for PickObject</param>
        /// <returns></returns>
        internal static Reference PickObject(UIDocument uidoc, string message)
        {
            try
            {
               return uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, message);
            }
            catch(Exception) { return null; }
        }
        /// <summary>
        /// Executes Revit Selection.PickObjects and catches user cancel exception
        /// </summary>
        /// <param name="uidoc"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static IList<Reference> PickObjects(UIDocument uidoc, string message)
        {
            try
            {
               return uidoc.Selection.PickObjects(ObjectType.Element, message);
            }
            catch (Exception) { return null; }
        }
        /// <summary>
        /// UI to Revit internal unit values
        /// </summary>
        /// <param name="dut"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetDutValueTo(DisplayUnitType dut, double p)
        {
            switch (dut)
            {
                case DisplayUnitType.DUT_METERS:
                    return p * METERS_IN_FEET;
                case DisplayUnitType.DUT_CENTIMETERS:
                    return p * METERS_IN_FEET * 100;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    return p;
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return p * 12;
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                    return p * METERS_IN_FEET;
                case DisplayUnitType.DUT_MILLIMETERS:
                    return p * METERS_IN_FEET * 1000;
                case DisplayUnitType.DUT_DECIMETERS:
                    return p * METERS_IN_FEET * 10;
                case DisplayUnitType.DUT_DECIMAL_DEGREES:
                    return ToDegrees(p);
            }
            return p;
        }
        /// <summary>
        /// Retuns human-readable ParameterGroup name
        /// </summary>
        /// <param name="parameterGroup"></param>
        /// <returns></returns>
        internal static string GetReadableGroupName(BuiltInParameterGroup parameterGroup)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            string value = parameterGroup.ToString().Replace("PG_", "");

            if (value.Equals("INVALID")) return "Other";    //Special case

            value = value.Replace("_", " ");
            value = textInfo.ToTitleCase(value.ToLower());

            return value;
        }

        /// <summary>
        /// Revit to UI units
        /// </summary>
        /// <param name="dut"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetDutValueFrom(DisplayUnitType dut, double p)
        {
            switch (dut)
            {
                case DisplayUnitType.DUT_METERS:
                    return p / METERS_IN_FEET;
                case DisplayUnitType.DUT_CENTIMETERS:
                    return p / METERS_IN_FEET / 100;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    return p;
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return p / 12;
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                    return p / METERS_IN_FEET;
                case DisplayUnitType.DUT_MILLIMETERS:
                    return p / METERS_IN_FEET / 1000;
                case DisplayUnitType.DUT_DECIMETERS:
                    return p / METERS_IN_FEET / 10;
                case DisplayUnitType.DUT_DECIMAL_DEGREES:
                    return ToRadians(p);
            }
            return p;
        }        
        /// <summary>
        /// Throw exception on unsupported dut type
        /// </summary>
        /// <param name="dut"></param>
        private static void NotSupported(DisplayUnitType dut)
        {
            //System.Windows.MessageBox.Show("Unit Type not supported.", "Family Editor Interface");
        }
        /// <summary>
        /// check if we support user units
        /// </summary>
        /// <returns></returns>
        public static Boolean _goUnits()
        {
            if (DUT.Equals(DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES)) return false;
            if (DUT.Equals(DisplayUnitType.DUT_FRACTIONAL_INCHES)) return false;
            return true;
        }
        /// <summary>
        /// Returns 1 or 0 instead of 1 or -1
        /// </summary>
        /// <param name="val">Expects 1 or -1</param>
        /// <returns></returns>
        public static double GetYesNoValue(double val)
        {
            return val == 1 ? 1 : 0;
        }
        /// <summary>
        /// Convert to Radians.
        /// </summary>
        /// <param name="val">The value to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToRadians(double val)
        {
            return (Math.PI / 180) * val;
        }
        /// <summary>
        /// Convert to Degrees.
        /// </summary>
        /// <param name="val">The value to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToDegrees(double val)
        {
            return (180 * val) / Math.PI;
        }
        /// <summary>
        /// truncate string and add '..' at the end
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string Truncate(string source, int length)
        {
            if (source.Length > length)
            {
                source = source.Substring(0, length);
                return source + "..";
            }
            else
            {
                return source;
            }
        }
        /// <summary>
        /// Check if a string contains unallowed characters
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool UnallowedChacarcters(string text)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 _!-]+$");
            bool result = regexItem.IsMatch(text);
            return(result ?  true :  false);
        }
    }
}
