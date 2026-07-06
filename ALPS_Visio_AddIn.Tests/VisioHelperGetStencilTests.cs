using NUnit.Framework;

namespace ALPS_Visio_AddIn_rewrite.Tests
{
    /// <summary>
    /// Friert das Stencil-Routing von <see cref="VisioHelper.GetStencil"/> ein. Die Stelle hat
    /// reale Bug-Historie: Fehlt ein SID-Master im internen Set, faellt GetStencil auf das
    /// SBD-Stencil zurueck und Place() wirft beim Import "Objektname nicht gefunden" (so geschehen
    /// bei den ALPS-SID-Elementen wie ActorExtension). Laeuft komplett ohne Visio — GetStencil
    /// arbeitet nur auf Konstanten.
    /// </summary>
    [TestFixture]
    public class VisioHelperGetStencilTests
    {
        [TestCase(Constants.SIDMasters.StandardActor)]
        [TestCase(Constants.SIDMasters.InterfaceActor)]
        [TestCase(Constants.SIDMasters.CommunicationRestriction)]
        [TestCase(Constants.SIDMasters.StandardMessageConnector)]
        [TestCase(Constants.SIDMasters.Message)]
        [TestCase(Constants.SIDMasters.StandAloneMacro)]
        // ALPS-SID-Elemente — genau die Master, deren Fehlen im Set frueher den
        // Import von Guard-/Subjekt-Extensions crashen liess.
        [TestCase(Constants.SIDMasters.ActorExtension)]
        [TestCase(Constants.SIDMasters.SubjectGroup)]
        [TestCase(Constants.SIDMasters.AbstractCommunicationChannel)]
        [TestCase(Constants.SIDMasters.SystemInterfaceSubject)]
        public void SID_Master_routet_auf_das_SID_Stencil(string masterName)
        {
            Assert.That(VisioHelper.GetStencil(masterName),
                Is.EqualTo(VisioHelper.VisioStencils.SID_STENCIL),
                masterName + " muss aus dem SID-Stencil gezogen werden.");
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
        public void SBD_Master_routet_auf_das_SBD_Stencil(string masterName)
        {
            Assert.That(VisioHelper.GetStencil(masterName),
                Is.EqualTo(VisioHelper.VisioStencils.SBD_STENCIL),
                masterName + " muss aus dem SBD-Stencil gezogen werden.");
        }

        /// <summary>
        /// Dokumentiert den bewussten Fallback: Unbekannte Master landen auf dem SBD-Stencil.
        /// (Wer einen neuen SID-Master ergaenzt, muss ihn in _sidShapeTypes eintragen —
        /// sonst schlaegt dieser Kontrakt hier bewusst NICHT fehl, aber der Import wirft.)
        /// </summary>
        [Test]
        public void Unbekannter_Master_faellt_auf_das_SBD_Stencil_zurueck()
        {
            Assert.That(VisioHelper.GetStencil("GibtEsNicht"),
                Is.EqualTo(VisioHelper.VisioStencils.SBD_STENCIL));
        }
    }
}
