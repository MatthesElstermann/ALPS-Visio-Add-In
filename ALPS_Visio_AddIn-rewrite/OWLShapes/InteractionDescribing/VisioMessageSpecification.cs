using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using Visio = Microsoft.Office.Interop.Visio;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageSpecification : MessageSpecification, IVisioImportableWithShape
    {
        private const string shapeType = Constants.SIDMasters.Message;

        private readonly IShapeImport import;
        public VisioMessageSpecification(IModelLayer layer) : base(layer) { import = new PASSProcessModelElementImport(this); }
        protected VisioMessageSpecification() { import = new PASSProcessModelElementImport(this); }

        public void ImportToVisio(Visio.Page page)
        {
            if (this.GetShape() != null) return;

            import.Import(shapeType, page, VH.GetBounds(this));

            // TODO: containsPayloadDescription
        }

        /// <summary>
        /// Zeichnet die Nachricht fuer eine Message-Box auf <paramref name="page"/> und liefert das Shape.
        /// Eine Spezifikation kann in mehreren Boxen vorkommen (auch auf anderen Layer-Seiten); ein
        /// Shape gehoert aber nur zu einer Liste — InsertListMember verschob es frueher aus der ersten
        /// Box heraus. Ab der zweiten Verwendung wird deshalb ein eigenes Shape angelegt.
        /// </summary>
        public Visio.Shape ImportForMessageBox(Visio.Page page)
        {
            if (this.GetShape() == null)
            {
                ImportToVisio(page);
                return this.GetShape();
            }

            var copy = new PASSProcessModelElementImport(this);
            copy.Import(shapeType, page, VH.GetBounds(this));
            return copy.GetShape();
        }

        public bool PrepareDimensions() // TODO: prepare dimensions
        {
            return false;
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageSpecification();
        }

        public Visio.Shape GetShape()
        {
            return import.GetShape();
        }
    }
}