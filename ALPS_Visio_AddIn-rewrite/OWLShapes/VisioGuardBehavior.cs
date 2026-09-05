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
    /// The constructor must be PUBLIC: alps.net.api's ReflectiveEnumerator.createInstance calls
    /// <c>type.GetConstructors()[0]</c> (public constructors only) to build the candidate list. With
    /// only a protected constructor the class is silently dropped and the plain <c>GuardBehavior</c>
    /// is used, so the GBD is never drawn. A parameterless public constructor is enough and avoids
    /// mirroring the (longer, drift-prone) base signature.
    /// </remarks>
    public class VisioGuardBehavior : GuardBehavior, IVisioImportable
    {
        public VisioGuardBehavior() { }

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
