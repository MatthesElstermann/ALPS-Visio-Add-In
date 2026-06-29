using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio-importable guard behaviour: the behaviour of a guard (the content of a Guard Behaviour
    /// Diagram). Drawn like any other subject behaviour (states + transitions) via
    /// <see cref="BehaviorImporter"/>; the page it is drawn onto becomes the GBD.
    /// </summary>
    /// <remarks>
    /// Only the protected parameterless constructor is provided — the parser instantiates parsed
    /// elements via <see cref="getParsedInstance"/>, so mirroring the (longer) base constructor is
    /// unnecessary and would only invite signature drift.
    /// </remarks>
    public class VisioGuardBehavior : GuardBehavior, IVisioImportable
    {
        protected VisioGuardBehavior() { }

        public void ImportToVisio(Visio.Page page)
        {
            BehaviorImporter.Draw(this.getBehaviorDescribingComponents(), page);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioGuardBehavior();
        }
    }
}
