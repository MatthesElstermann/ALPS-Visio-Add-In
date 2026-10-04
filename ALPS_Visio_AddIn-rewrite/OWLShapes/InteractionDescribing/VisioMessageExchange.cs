using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchange : MessageExchange, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.StandardMessageConnector;

        private readonly IShapeImport import;
        public VisioMessageExchange(IModelLayer layer) : base(layer) { import = new PASSProcessModelElementImport(this); }
        protected VisioMessageExchange() { import = new PASSProcessModelElementImport(this); }
        public void ImportToVisio(Visio.Page page)
        {
            // Vor dem Zeichnen pruefen: ein Verbinder kann nur an Shapes derselben Seite kleben.
            // Liegen Sender und Empfaenger auf verschiedenen Ebenen (= Seiten), entstand sonst ein
            // verwaister Verbinder neben der Seite und die Meldung "Ungueltiges Zielobjekt".
            EnsureOnPage(this.getSender(), page, "Sender");
            EnsureOnPage(this.getReceiver(), page, "Empfänger");

            import.Import(shapeType, page, VH.GetBounds(this));

            // set path (auto arrange)
            if (this.getSender() is IVisioImportableWithShape importableSender)
                this.GetShape().CellsU["BeginX"].GlueToPos(importableSender.GetShape(), 1, 0.5);
            if (this.getReceiver() is IVisioImportableWithShape importableReceiver)
                this.GetShape().CellsU["EndY"].GlueToPos(importableReceiver.GetShape(), 0, 0.5);

            // TODO: AbstractMessageExchange
            // TODO: FinalizedMessageExchange -> alps.net.api
        }

        private static void EnsureOnPage(ISubject subject, Visio.Page page, string role)
        {
            if (!(subject is IVisioImportableWithShape importable)) return;
            Visio.Shape shape = importable.GetShape();
            string name = subject.getModelComponentLabelsAsStrings().Count > 0
                ? subject.getModelComponentLabelsAsStrings()[0] : subject.getModelComponentID();
            if (shape == null)
                throw new System.InvalidOperationException(role + " „" + name + "“ wurde nicht gezeichnet — Nachricht übersprungen.");
            if (shape.ContainingPage.ID != page.ID)
                throw new System.InvalidOperationException(role + " „" + name + "“ liegt auf einer anderen Ebene (Seite „"
                    + shape.ContainingPage.Name + "“). Nachrichten zwischen Ebenen kann Visio nicht als Verbinder zeichnen — Nachricht übersprungen.");
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false; // routing follow points
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchange();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}