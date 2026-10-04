using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Prueft, dass die Master-Namen, mit denen der Import Shapes ablegt, in den installierten
    /// ALPS-Schablonen tatsaechlich existieren. Drei SBD-Konstanten ("TimeTransition",
    /// "SendingFailedTransition", "FlowRestrictor") gab es dort nie — der Import dieser
    /// Transitionen schlug fehl. Die Schablonen (.vssm = ZIP/OPC) werden ohne Visio gelesen;
    /// sind sie nicht installiert, wird der Test uebersprungen.
    /// </summary>
    [TestFixture]
    public class StencilMasterNameTests
    {
        private static readonly string ShapesDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Meine Shapes");

        private static ISet<string> MasterNamesOf(string prefix)
        {
            string file = Directory.Exists(ShapesDir)
                ? Directory.GetFiles(ShapesDir, prefix + " v*.vssm").OrderByDescending(f => f).FirstOrDefault()
                : null;
            if (file == null)
                Assert.Ignore("Schablone \"" + prefix + "\" nicht installiert (" + ShapesDir + ").");

            using (ZipArchive zip = ZipFile.OpenRead(file))
            using (Stream stream = zip.GetEntry("visio/masters/masters.xml").Open())
            {
                return new HashSet<string>(XDocument.Load(stream).Root.Elements()
                    .Select(master => (string)master.Attribute("NameU")));
            }
        }

        [TestCase(Constants.SBDMasters.DoState)]
        [TestCase(Constants.SBDMasters.ReceiveState)]
        [TestCase(Constants.SBDMasters.SendState)]
        [TestCase(Constants.SBDMasters.GenericReturnToOriginReference)]
        [TestCase(Constants.SBDMasters.StandardTransition)]
        [TestCase(Constants.SBDMasters.ReceiveTransition)]
        [TestCase(Constants.SBDMasters.SendTransition)]
        [TestCase(Constants.SBDMasters.SendingFailedTransition)]
        [TestCase(Constants.SBDMasters.TimeTransition)]
        [TestCase(Constants.SBDMasters.UserCancelTransition)]
        [TestCase(Constants.SBDMasters.FlowRestrictor)]
        [TestCase(Constants.SBDMasters.StateExtension)]
        public void SBD_Master_existiert_in_der_Schablone(string masterName)
        {
            Assert.That(MasterNamesOf("Abstract PASS SBD Visio Shapes"), Does.Contain(masterName));
        }

        [TestCase(Constants.SIDMasters.StandardActor)]
        [TestCase(Constants.SIDMasters.InterfaceActor)]
        [TestCase(Constants.SIDMasters.CommunicationRestriction)]
        [TestCase(Constants.SIDMasters.StandardMessageConnector)]
        [TestCase(Constants.SIDMasters.Message)]
        [TestCase(Constants.SIDMasters.StandAloneMacro)]
        [TestCase(Constants.SIDMasters.ActorExtension)]
        [TestCase(Constants.SIDMasters.GuardExtension)]
        [TestCase(Constants.SIDMasters.MacroExtension)]
        [TestCase(Constants.SIDMasters.SubjectGroup)]
        [TestCase(Constants.SIDMasters.AbstractCommunicationChannel)]
        [TestCase(Constants.SIDMasters.SystemInterfaceSubject)]
        public void SID_Master_existiert_in_der_Schablone(string masterName)
        {
            Assert.That(MasterNamesOf("Abstract PASS SID Visio Shapes"), Does.Contain(masterName));
        }
    }
}
