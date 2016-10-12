using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface
{
    public static class Utils
    {
        private static DisplayUnitType dut;
        public const double METERS_IN_FEET = 0.3048;

        public static void Init(Document doc)
        {
            dut = doc.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits;
        }
        /// <summary>
        /// forward conversion of project to unit values
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double convertValueTO(double p)
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
        /// <summary>
        /// reverse the unit transformation to project units
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double convertValueFROM(double p)
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
        /// check if we support user units
        /// </summary>
        /// <returns></returns>
        public static Boolean _goUnits()
        {
            if (dut.Equals(DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES)) return false;
            if (dut.Equals(DisplayUnitType.DUT_FRACTIONAL_INCHES)) return false;
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

        public static double trueValue(double value)
        {
            double trueValue = 0.0;

            switch (dut)
            {
                case DisplayUnitType.DUT_METERS:
                    trueValue = value * METERS_IN_FEET;
                    break;
                case DisplayUnitType.DUT_CENTIMETERS:
                    trueValue = value * METERS_IN_FEET * 100;
                    break;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    trueValue = value;
                    break;
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    trueValue = value * 12;
                    break;
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                    trueValue = value * METERS_IN_FEET;
                    break;
                case DisplayUnitType.DUT_MILLIMETERS:
                    trueValue = value * METERS_IN_FEET * 1000;
                    break;
            }

            return trueValue;
        }
    }
}
