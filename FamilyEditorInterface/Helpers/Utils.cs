using Autodesk.Revit.DB;
using FamilyEditorInterface.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyEditorInterface
{
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
        //UI to Revit internal unit values
        public static double convertValueTO(double p)
        {
            return GetDutValueTo(DUT, p);
        }
        //Revit to UI units
        public static double convertValueFROM(double p)
        {
            return GetDutValueFrom(DUT, p);
        }
        //UI to Revit internal unit values
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
        //Revit to UI units
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
        /// <summary>
        /// Check if it's a Family Document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static Autodesk.Revit.DB.Document checkDoc(Autodesk.Revit.DB.Document document)
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

        #region Request Handling
        // Toggle Type to Instance and vica verse
        public static void MakeRequest(RequestId request, string name, string type)
        {
            Application.Control.handler.Request.TypeToInstance(new List<string>() { name });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }

        // Rename Parameter
        public static void MakeRequest(RequestId request, Tuple<string, string> renameValue)
        {
            Application.Control.handler.Request.RenameValue(new List<Tuple<string, string>>() { renameValue });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        // Change Parameter Value of Multiple
        public static void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            Application.Control.handler.Request.Value(values);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        // Delete Parameter
        public static void MakeRequest(RequestId request, string deleteValue)
        {
            Application.Control.handler.Request.DeleteValue(new List<string>() { deleteValue });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        // Change Parameter Value
        public static void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            Application.Control.handler.Request.Value(new List<Tuple<string, double>>() { value });
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        // Change all values
        public static void MakeRequest(RequestId request, List<Tuple<string, string, double>> value)
        {
            Application.Control.handler.Request.AllValues(value);
            Application.Control.handler.Request.Make(request);
            Application.Control.exEvent.Raise();
        }
        #endregion
    }
}
