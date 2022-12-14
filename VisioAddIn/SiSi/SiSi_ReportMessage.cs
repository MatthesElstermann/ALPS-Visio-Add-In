using System;

namespace VisioAddIn.SiSi
{
    class SiSi_ReportMessage
    {

        public enum ReportType
        {
            Warning,
            Error
        };

        private ReportType reportType;
        private string subject;
        private string shapeName;
        private string message;
        private string devCode;
        private DateTime timestamp;

        /// <summary>
        /// Creates an ReportMessageObject which contains all the Data nessecary
        /// </summary>
        /// <param name="reportType">Type of the Report</param>
        /// <param name="shapeType"> Type of the Shape</param>
        /// <param name="shapeName"> Name of the Shape</param>
        /// <param name="message"> Message to display</param>
        /// <param name="devCode"> Indicator of the place of the reportthrow for better bugfixing</param>
        /// <param name="timestamp"> Timestamp for sorting in order of occurence</param>
        public SiSi_ReportMessage(ReportType reportType, string subject, String shapeName, String message, String devCode, DateTime timestamp)
        {
            this.reportType = reportType;
            this.subject = subject;
            this.shapeName = shapeName;
            this.message = message;
            this.devCode = devCode;
            this.timestamp = timestamp;
        }

        /// <summary>
        /// Constructor with automated adding of current time. See Constructor above.
        /// </summary>
        /// <param name="reportType">Type of the Report</param>
        /// <param name="shapeType"> Type of the Shape</param>
        /// <param name="shapeName"> Name of the Shape</param>
        /// <param name="message"> Message to display</param>
        /// <param name="devCode"> Indicator of the place of the reportthrow for better bugfixing</param>
        public SiSi_ReportMessage(ReportType reportType, string subject, String shapeName, String message, String devCode) : this(reportType, subject, shapeName, message, devCode, DateTime.Now)
        {

        }

        /// <summary>
        /// Returns the values of this Reportmessage with the given parameters
        /// </summary>
        /// <param name="reportType">Type of the Report</param>
        /// <param name="shapeType"> Type of the Shape</param>
        /// <param name="shapeName"> Name of the Shape</param>
        /// <param name="message"> Message to display</param>
        /// <param name="devCode"> Indicator of the place of the reportthrow for better bugfixing</param>
        /// <param name="timestamp"> Timestamp for sorting in order of occurence</param>
        public void giveAllInfos(out ReportType reportType, out string subject, out String shapeName, out String message, out String devCode, out DateTime timestamp)
        {
            reportType = this.reportType;
            subject = this.subject;
            shapeName = this.shapeName;
            message = this.message;
            devCode = this.devCode;
            timestamp = this.timestamp;
        }

        /// <summary>
        /// Returns the values of this Reportmessage with the given parameters
        /// </summary>
        /// <param name="reportType">Type of the Report</param>
        /// <param name="shapeType"> Type of the Shape</param>
        /// <param name="shapeName"> Name of the Shape</param>
        /// <param name="message"> Message to display</param>
        /// <param name="devCode"> Indicator of the place of the reportthrow for better bugfixing</param>
        /// <param name="timestamp"> Timestamp for sorting in order of occurence</param>
        public void giveAllInfos(out string reportType, out string subject, out String shapeName, out String message, out String devCode, out DateTime timestamp)
        {
            reportType = this.reportType.ToString("G");
            subject = this.subject;
            shapeName = this.shapeName;
            message = this.message;
            devCode = this.devCode;
            timestamp = this.timestamp;
        }
    }
}
