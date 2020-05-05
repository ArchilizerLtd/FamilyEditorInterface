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
        //forward conversion of project to unit values
        public static double convertValueTO(double p)
        {
            return GetDutValueTo(DUT, p);
        }
        //reverse the unit transformation to project units
        public static double convertValueFROM(double p)
        {
            return GetDutValueFrom(DUT, p);
        }
        //forward conversion of project to unit values
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
            }
            return p;
        }
        //reverse the unit transformation to project units
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
            Application.handler.Request.TypeToInstance(new List<string>() { name });
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }

        // Rename Parameter
        public static void MakeRequest(RequestId request, Tuple<string, string> renameValue)
        {
            Application.handler.Request.RenameValue(new List<Tuple<string, string>>() { renameValue });
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        // Change Parameter Value of Multiple
        public static void MakeRequest(RequestId request, List<Tuple<string, double>> values)
        {
            Application.handler.Request.Value(values);
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        // Delete Parameter
        public static void MakeRequest(RequestId request, string deleteValue)
        {
            Application.handler.Request.DeleteValue(new List<string>() { deleteValue });
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        // Change Parameter Value
        public static void MakeRequest(RequestId request, Tuple<string, double> value)
        {
            Application.handler.Request.Value(new List<Tuple<string, double>>() { value });
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        // Change all values
        public static void MakeRequest(RequestId request, List<Tuple<string, string, double>> value)
        {
            Application.handler.Request.AllValues(value);
            Application.handler.Request.Make(request);
            Application.exEvent.Raise();
        }
        #endregion
    }
}
