using Microsoft.Office.Interop.Visio;
using System;

namespace VisioAddIn
{
    public class ALPSGlobalFunctions
    {

        private const double AVG_DAYS_PER_MONTH_CONST = 30.4375;

        public static string convertFractionOfDayToHourFormat(double fractionOfDay)
        {
            TimeSpan span = TimeSpan.FromDays(fractionOfDay);

            string result = "";

            if (span.Days > 0)
            {
                result += span.Days + " days, ";
            }

            if (span.Hours > 0)
            {
                result += span.Hours + " hours, ";
            }

            if (span.Minutes > 0)
            {
                result += span.Minutes + " minutes, ";
            }

            if (span.Seconds > 0)
            {
                result += span.Seconds + " seconds, ";
            }

            if (span.Milliseconds > 0)
            {
                result += span.Milliseconds + " millis, ";
            }

            //Check if result is Empty
            if (result == "")
            {
                result = "0";
            }

            return result;
        }

        internal static string removeLineBreaks(string input)
        {
            return input.Replace(System.Environment.NewLine, "");
        }

        internal static string getSystemDecimalDelimiter()
        {
            return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }

        public static Page determineConnectedSbdPageForSubject(Shape inputSubject)
        {
            if (inputSubject.CellExistsU["Hyperlink." + ALPSConstants.alpsHyperlinkTypeLinkedSBD, 01] == 0)
                return null;
            
            string pageSubAddress = inputSubject.Hyperlinks.ItemU[ALPSConstants.alpsHyperlinkTypeLinkedSBD].SubAddress;

            return pageSubAddress != "" ? inputSubject.Document.Pages.ItemU[pageSubAddress] : null;
        }

        /// <summary>
        /// Decodes a xml time duration with the form P_Y_M_DT_H_M_S with _ being integer or floating point values.
        /// The decoded result is a double specifying the amount of the days the provided xml string encoded.
        /// </summary>
        public static double decodeXmlDayTimeDurationToFractionsOfDays(string inputString)
        {
            double totalDayFraction = 0;

            int posP = inputString.IndexOf("P", StringComparison.Ordinal) + 1;

            if ((inputString.Length <= 1) || (posP != 1)) return totalDayFraction;
            inputString = inputString.Substring(posP);
            totalDayFraction = 0;

            // Years ##################################### (should not be here)
            int posY = inputString.IndexOf("Y", StringComparison.Ordinal) + 1;
            if (posY > 1)
            {
                string yearElement = inputString.Substring(0, posY - 1);
                if (double.TryParse(yearElement, out double yearValue))
                {
                    totalDayFraction += yearValue * 365;
                }

                inputString = inputString.Substring(posY);
            }

            // Months ##################################### (should not be here)
            int posM = inputString.IndexOf("M", StringComparison.Ordinal) + 1;
            int posT = inputString.IndexOf("T", StringComparison.Ordinal) + 1;
            if ((posM > 1) && ((posM < posT) || (posT < 1)))
            {
                string monthElement = inputString.Substring(0, (posM - 1));
                if (double.TryParse(monthElement, out double monthValue))
                {
                    totalDayFraction += monthValue * AVG_DAYS_PER_MONTH_CONST;
                }

                inputString = inputString.Substring(posM);
            }

            // Days #####################################
            int posD = inputString.IndexOf("D", StringComparison.Ordinal) + 1;
            if (posD > 1)
            {
                string dayElement = inputString.Substring(0, (posD - 1));
                if (double.TryParse(dayElement, out double dayValue))
                {
                    totalDayFraction += dayValue;
                }

                inputString = inputString.Substring(posD);
            }

            //  YMD to HMS separator #####################################
            posT = inputString.IndexOf("T", StringComparison.Ordinal) + 1;
            if (posT > 0)
            {
                inputString = inputString.Substring(posT);
            }
            /*
                else if ((result.Length < 1))
                {
                    goto myErrorHandlingCode;
                }
                */

            // Hours #####################################
            int posH = inputString.IndexOf("H", StringComparison.Ordinal) + 1;
            if (posH > 1)
            {
                string hourElement = inputString.Substring(0, posH - 1);
                if (double.TryParse(hourElement, out double hourValue))
                {
                    totalDayFraction += hourValue / 24.0;
                }

                inputString = inputString.Substring(inputString.Length - (inputString.Length - posH));
            }

            // Minutes #####################################
            int posM2 = inputString.IndexOf("M", StringComparison.Ordinal) + 1;
            if (posM2 > 1)
            {
                string minuteElement = inputString.Substring(0, posM2 - 1);
                if (double.TryParse(minuteElement, out double minuteValue))
                {

                    totalDayFraction += minuteValue / (24.0 * 60);
                }

                inputString = inputString.Substring((inputString.Length - (inputString.Length - posM2)));
            }

            //  #####################################
            int posS = inputString.IndexOf("S", StringComparison.Ordinal) + 1;
            if (posS > 1)
            {
                string secondElement = inputString.Substring(0, posS - 1);
                if (double.TryParse(secondElement, out double secondValue))
                {

                    totalDayFraction +=  secondValue / (24 * 60 * 60);
                }

                inputString = inputString.Substring((inputString.Length - (inputString.Length - posS)));
            }

            return totalDayFraction;

            //myErrorHandlingCode:
        }

        internal static double convertPercentageFormulaToDouble(string formula)
        {
            return double.Parse(formula.Remove(formula.Length - 3)) / 100.0;
        }

        public static double convertFormulaToFractionsOfDay(string formula)
        {
            formula = formula.Replace("\"", "");
            formula = formula.Replace("vt.", "");
            double result = double.Parse(formula);
            return result;
        }

        internal static string makeStringUriCompatible(string idOfMessage)
        {
            return idOfMessage.Replace(":", " ")
                .Replace(";", " ")
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace(Environment.NewLine, " ")
                .TrimEnd(' ')
                .TrimStart(' ')
                .Replace(" ", "_");
        }

        internal static string removeQuotes(string subjectName)
        {
            return subjectName.Replace("\"", "");
        }


    }
}
