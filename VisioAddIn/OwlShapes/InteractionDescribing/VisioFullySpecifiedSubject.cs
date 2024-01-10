
using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioFullySpecifiedSubject : FullySpecifiedSubject, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandardActor;
        private Simple2DPosParser parser;
        private readonly IExportFunctionality export;

        protected VisioFullySpecifiedSubject() { export = new SubjectExport(this); }

        public VisioFullySpecifiedSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null,
            ISubjectBaseBehavior subjectBaseBehavior = null, ISet<ISubjectBehavior> subjectBehaviors = null,
            ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null,
            ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(layer, labelForID, incomingMessageExchange, subjectBaseBehavior, subjectBehaviors, outgoingMessageExchange, maxSubjectInstanceRestriction,
                  subjectDataDefinition, inputPoolConstraints, comment, additionalLabel, additionalAttribute)
        { export = new SubjectExport(this); }

        public void exportToVisio(Visio.Page pageToExportTo, ISimple2DVisualizationBounds bounds = null)
        {

            // Place a standard actor onto the SID page
            List<ISimple2DVisualizationPoint> myTempList = new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>());

            Debug.WriteLine("Subject export: " + this.getModelComponentID() + " Point List count:  " + myTempList.Count);
            
            export.export(VisioHelper.ShapeType.SID, pageToExportTo, type, myTempList, this); 


            //shape = VisioHelper.place(VisioHelper.ShapeType.SID, pageToExportTo, ALPSConstants.alpsSIDMasterStandardActor,
            //new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()));


            // TODO multiple behaviors etc  


            //This gets the Page which the makros create themself and uses it to drop all of the SBD elements there.
            Visio.Pages allPages = VisioHelper.getCurrentPages();
            Visio.Page currentSBDPage = null;
            foreach (Visio.Page page in allPages)
            {
                if (page.NameU.EndsWith(getModelComponentID()))
                {
                    currentSBDPage = page;
                    break;
                }

            }

            //if there was no SBD page created (because no active listeners where there
            //then create you own
            if (currentSBDPage == null)
            {
                //(Visio.Page sidPage, string name, string nameU, Visio.Shape subjectShape)
                currentSBDPage = VisioHelper.CreateSBDPage(pageToExportTo, ("SBD: " + getModelComponentID()), (""+getModelComponentID()), this.getShape());
            }

            // TODO 
            // SBDPages.Add(currentSBDPage);

            //Now place all of the SBD shapes. It is easier to just interrupt the SID placement and do this now
            if (getSubjectBaseBehavior() is IVisioExportable exportable && !(currentSBDPage is null))
                exportable.exportToVisio(currentSBDPage);
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioFullySpecifiedSubject();
        }

        public Visio.Shape getShape()
        {
            return export.getShape();
        }

        public void setShape(Visio.Shape shape)
        {
            export.setShape(shape);
        }

        protected override bool parseAttribute(string predicate, string objectContent, string lang, string dataType, IParseablePASSProcessModelElement element)
        {
            if (parser is null) parser = new Simple2DPosParser(this);
            if (!parser.parseAttribute(predicate, objectContent, lang, dataType, element))
            {
                return base.parseAttribute(predicate, objectContent, lang, dataType, element);
            }
            return true;
        }
    }
}
