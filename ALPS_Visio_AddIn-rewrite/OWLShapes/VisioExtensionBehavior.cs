using alps.net.api.ALPS;
using alps.net.api.parsing;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    /// <summary>
    /// Visio-importable extension behaviour: the behaviour an <see cref="ISubjectExtension"/> adds on
    /// top of a base subject. Drawn like any other subject behaviour (states + transitions) via
    /// <see cref="BehaviorImporter"/>; the page it is drawn onto becomes the extension's GBD.
    /// </summary>
    /// <remarks>
    /// The constructor must be PUBLIC: alps.net.api's ReflectiveEnumerator.createInstance calls
    /// <c>type.GetConstructors()[0]</c> (public constructors only) to build the candidate list. With
    /// only a protected constructor the class is silently dropped and the plain
    /// <c>ExtensionBehavior</c> is used, so the extension behaviour is never drawn.
    /// </remarks>
    public class VisioExtensionBehavior : ExtensionBehavior, IVisioImportable
    {
        public VisioExtensionBehavior() { }

        public void ImportToVisio(Visio.Page page)
        {
            BehaviorImporter.Draw(this.getBehaviorDescribingComponents(), page);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioExtensionBehavior();
        }
    }
}
