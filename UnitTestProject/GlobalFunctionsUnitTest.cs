using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisioAddIn;

namespace UnitTestProject
{
    [TestClass]
    public class GlobalFunctionsUnitTest
    {
        [TestMethod]
        public void decodeXmlDayTimeDurationToFractionsOfDays()
        {
            string validXmlDuration = "P1Y2M3DT5H20M30.123S";
            double result = ALPSGlobalFunctions.decodeXmlDayTimeDurationToFractionsOfDays(validXmlDuration);
        }
    }
}
