using alps.net.api.ALPS;
using alps.net.api.parsing;
using System.Collections.Generic;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioMacroExtension : MacroExtension, IVisioExportableWithShape
    {
        // TODO change type etc.
        private const string type = ALPSConstants.alpsSIDMasterActorExtension;
        private readonly IExportFunctionality export;

        protected VisioMacroExtension() { export = new SubjectExport(this); }
        

        public VisioMacroExtension(IModelLayer layer) : base(layer) { export = new SubjectExport(this); }

        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            // Place the shape onto the SID page
            export.export(VisioHelper.ShapeType.SID, currentPage, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));


        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMacroExtension();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }

    }
}
