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
    /// Only the protected parameterless constructor is provided — the parser instantiates parsed
    /// elements via <see cref="getParsedInstance"/>, so mirroring the base constructor is unnecessary.
    /// </remarks>
    public class VisioExtensionBehavior : ExtensionBehavior, IVisioImportable
    {
        protected VisioExtensionBehavior() { }

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
