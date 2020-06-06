using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyEditorInterface.Helpers
{
    public static class ValueConvertUtils
    {
        /// <summary>
        /// Converts a string from a double value
        /// </summary>
        /// <remarks>
        /// Depends on the DisplayUnitType and can be m, dm, cm, mm, ft, in, frac ft, frac in, angle
        /// </remarks>
        public static string StringFromDoubleConvert(DisplayUnitType displayUnitType, int precision, double value)
        {
            switch (displayUnitType)
            {
                case DisplayUnitType.DUT_MILLIMETERS:
                case DisplayUnitType.DUT_CENTIMETERS:
                case DisplayUnitType.DUT_DECIMETERS:
                case DisplayUnitType.DUT_METERS:
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                case DisplayUnitType.DUT_DECIMAL_FEET:
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return ConvertPrecision(precision, value);
                case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
                    return ConvertFeetInches(value);
                case DisplayUnitType.DUT_FRACTIONAL_INCHES:
                    return ConvertInches(value);
                case DisplayUnitType.DUT_DECIMAL_DEGREES:
                    return ConvertAngle(precision, value);
                default:
                    return "Not supported.";
            }
        }
        //Adds the degrees symbol to an angle value
        private static string ConvertAngle(int precision, double value)
        {
            return ConvertPrecision(precision, value) + "°";
        }
        //Converts double to string fractional feet and inches
        private static string ConvertFeetInches(double value)
        {
            return ToFractionalFeetAndInches(value);
        }
        //Converts double to string fractional inches
        private static string ConvertInches(double value)
        {
            return ToFractionInches(value) + "\"";
        }
        /// <summary>
        /// Convers a double from a string value
        /// </summary>
        /// <param name="storageType">The storage type that defines what type of value we originally have</param>
        /// <param name="displayUnitType">the Display Unit Type</param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        public static double DoubleFromStringConvert(StorageType storageType, DisplayUnitType displayUnitType, string value)
        {
            switch (storageType)
            {
                case (StorageType.Double):
                    return DoubleFromStringConvert(displayUnitType, value);
                case (StorageType.Integer):
                    return Double.Parse(value);
                default:
                    return 0.0;
            }
        }
        /// <summary>
        /// Converts a double from a string value
        /// </summary>
        /// <remarks>
        /// Depends on the DisplayUnitType and can be m, dm, cm, mm, ft, in, frac ft, frac in, angle
        /// </remarks>
        public static double DoubleFromStringConvert(DisplayUnitType displayUnitType, string value)
        {
            double result = 0.0;
            switch (displayUnitType)
            {
                case DisplayUnitType.DUT_MILLIMETERS:
                case DisplayUnitType.DUT_CENTIMETERS:
                case DisplayUnitType.DUT_DECIMETERS:
                case DisplayUnitType.DUT_METERS:
                case DisplayUnitType.DUT_METERS_CENTIMETERS:
                case DisplayUnitType.DUT_DECIMAL_FEET:
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    double.TryParse(value, out result);
                    return result;
                case DisplayUnitType.DUT_DECIMAL_DEGREES:
                    double.TryParse(value.Trim('°'), out result);
                    return result;
                case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
                    return ConvertFeetInches(value);
                case DisplayUnitType.DUT_FRACTIONAL_INCHES:
                    return ConvertInches(value);
                default:
                    return 0.0;
            }
        }
        //Convert string to double fractional inches
        private static double ConvertInches(string value)
        {
            FromFractionalInches(value, out var result);
            return result;    
        }
        //Converts string to double franction feet and inches
        private static double ConvertFeetInches(string value)
        {
            FromFractionalFeetAndInches(value, out var result);
            return result;
        }

        #region Double to String
        /// <summary>
        /// Creates a precision formatted string form a double
        /// </summary>
        /// <param name="precision">1 = 0.0, 2 = 0.00</param>
        /// <param name="value">The value that will be converted to String</param>
        /// <returns></returns>
        public static string ConvertPrecision(int precision, double value)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalDigits = precision;
            return value.ToString("N", nfi);
        }
        /// <summary>
        /// Converts decimal feet and inches to fractional feet and inches
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string ToFractionalFeetAndInches(double value)
        {
            var feet = (int)value; //Feet whole
            var decimalInches = (value - feet) * 12;    //Decimal inches

            if (decimalInches == 0) return $"{feet}'";

            var inches = (int)(decimalInches);  //Inches whole
            var remainder = ToFractionInches(decimalInches - inches);    //The remainder

            if (remainder == "0") return $"{feet}' {inches}\"";

            return $"{feet}' {inches} {remainder}\"";
        }
        /// <summary>
        /// Converts decimal inches to fractional inches
        /// </summary>
        /// <see cref="https://stackoverflow.com/questions/38891250/convert-to-fraction-inches"/>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string ToFractionInches(double value)
        {
            // denominator is fixed
            int denominator = 256;
            // integer part, can be signed: 1, 0, -3,...
            int integer = (int)value;
            // numerator: always unsigned (the sign belongs to the integer part)
            // + 0.5 - rounding, nearest one: 37.9 / 64 -> 38 / 64; 38.01 / 64 -> 38 / 64
            int numerator = (int)((Math.Abs(value) - Math.Abs(integer)) * denominator + 0.5);

            // some fractions, e.g. 24 / 64 can be simplified:
            // both numerator and denominator can be divided by the same number
            // since 64 = 2 ** 6 we can try 2 powers only 
            // 24/64 -> 12/32 -> 6/16 -> 3/8
            // In general case (arbitrary denominator) use gcd (Greatest Common Divisor):
            //   double factor = gcd(denominator, numerator);
            //   denominator /= factor;
            //   numerator /= factor;
            while ((numerator % 2 == 0) && (denominator % 2 == 0))
            {
                numerator /= 2;
                denominator /= 2;
            }

            // The longest part is formatting out
            // if we have an actual, not degenerated fraction (not, say, 4 0/1)
            if (denominator > 1)
                if (integer != 0) // all three: integer + numerator + denominator
                    return string.Format("{0} {1}/{2}", integer, numerator, denominator);
                else if (value < 0) // negative numerator/denominator, e.g. -1/4
                    return string.Format("-{0}/{1}", numerator, denominator);
                else // positive numerator/denominator, e.g. 3/8
                    return string.Format("{0}/{1}", numerator, denominator);
            else
                return integer.ToString(); // just an integer value, e.g. 0, -3, 12...  
        }
        #endregion

        #region String to Double
        //Returns decimal from fractional feet and inches
        private static double? FromFractionalInches(string value, out double success)
        {
            if (double.TryParse(value, out success))
            {
                return success; //The input could be decimal, check that first
            }
            else if (FractionalInches(value, out success))
            {
                return success; //The input contains ' and '', check that second
            }
            else
            {
                return null;    //The input was invalid
            }
        }
        //Returns the decimal representation of the fractional feet and inches
        private static bool FractionalInches(string value, out double success)
        {
            var trim = new Char[] { ' ', '"' };
            var split = value.Split(' ');   //Returns the 2 parts from inches fractional representation - 7 51/256"
            if (split.Length <= 0 || !Int32.TryParse(split[0].Trim(trim), out int inches))
            {
                success = 0.0;  //We failed to convert to inches
                return false;
            }
            if (split.Length <= 1)
            {
                success = inches;   //There was no fraction, we add the inches part
                return true;
            }
            var fraction = split[1].Trim(trim);
            var fractionParts = fraction.Split('/');
            if (fractionParts.Length > 1 && Int32.TryParse(fraction.Split('/')[0], out int remainder) && Int32.TryParse(fraction.Split('/')[1], out int denominator))
            {
                success = inches + FromRemainder(remainder, denominator);    //Finally convert from inches and fraction
                return true;
            }
            success = 0.0;
            return false;   //If we somehow failed, return false
        }
        //Returns decimal from fractional feet and inches
        private static double? FromFractionalFeetAndInches(string value, out double success)
        {
            if (double.TryParse(value, out success))
            {
                return success; //The input could be decimal, check that first
            }
            else if (FractionalFeetAndInches(value, out success))
            {
                return success; //The input contains ' and '', check that second
            }
            else
            {
                return null;    //The input was invalid
            }
        }
        //Returns the decimal representation of the fractional feet and inches
        private static bool FractionalFeetAndInches(string value, out double success)
        {
            var trim = new Char[] { ' ', '\'', '"' };
            var split = value.Split(' ');   //Returns the 3 parts from a feet and inches fractional representation - 7' 7 51/256"
            if (split.Length <= 0 || !Int32.TryParse(split[0].Trim(trim), out int feet))
            {
                success = 0.0;  //We failed to convert to feet
                return false;
            }
            if (split.Length <= 1 || !Int32.TryParse(split[1].Trim(trim), out int inches))
            {
                success = feet; //Only feet value was found
                return true;
            }
            if (split.Length <= 2)
            {
                success = feet + inches / 12.0;   //There was no fraction, we add the feet part with the inches part 
                return true;
            }
            var fraction = split[2].Trim(trim);
            var fractionParts = fraction.Split('/');
            if (fractionParts.Length > 1 && Int32.TryParse(fraction.Split('/')[0], out int remainder) && Int32.TryParse(fraction.Split('/')[1], out int denominator))
            {
                success = FromFractionalFeetAndInches(feet, inches, remainder, denominator);    //Finally convert from feet, inches and fraction
                return true;
            }
            success = 0.0;
            return false;   //If we somehow failed, return false
        }
        //Get decimal from fractional feet and inches
        private static double FromFractionalFeetAndInches(int feet, int inches, double remainder, double denominator)
        {
            //Add feet to the result of FromInches
            double result = (FromInches(inches, remainder, denominator) + feet);

            return result;
        }
        //Get decimal from fracitonal inches
        private static double FromInches(int inches, double remainder, double denominator)
        {
            //Add inches with the result of the FromRemainder, then divide by 12
            double result = (FromRemainder(remainder, denominator) + inches) / 12.0;

            return result;
        }
        //Get the remainder value (7' 7 51/256" - get the 51/256 into 0.2 form)
        private static double FromRemainder(double remainder, double denominator)
        {
            // divide remainder by denmoniator and round to 1 decimal point
            double result = Math.Round(remainder / (double)denominator, 4);

            return result;
        }
        #endregion
    }
}
