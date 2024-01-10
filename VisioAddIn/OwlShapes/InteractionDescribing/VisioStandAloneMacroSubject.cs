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
    public class VisioStandAloneMacroSubject : StandaloneMacroSubject, IVisioExportableWithShape
    {
        private const string type = ALPSConstants.alpsSIDMasterStandAloneMacro;
        private Simple2DPosParser parser;
        private readonly IExportFunctionality export;

        protected VisioStandAloneMacroSubject()
        {
            //Debug.WriteLine("Constructor start: VisioStandAloneMacroSubject()");
            export = new SubjectExport(this);
            //Debug.WriteLine("Constructor end: VisioStandAloneMacroSubject()");
        }

        public VisioStandAloneMacroSubject(IModelLayer layer, string labelForID = null, ISet<IMessageExchange> incomingMessageExchange = null,
            IMacroBehavior subjectMacroBehavior = null, ISet<IMessageExchange> outgoingMessageExchange = null, int maxSubjectInstanceRestriction = 1, ISubjectDataDefinition subjectDataDefinition = null,
            ISet<IInputPoolConstraint> inputPoolConstraints = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null)
            : base(layer, labelForID, incomingMessageExchange, subjectMacroBehavior, 
                  outgoingMessageExchange, maxSubjectInstanceRestriction,
                  comment, additionalLabel, additionalAttribute)
        {
            //Debug.WriteLine("Constructor start: VisioStandAloneMacroSubject(...long...) lable: " + labelForID);
            export = new SubjectExport(this);
           // Debug.WriteLine("Constructor start: VisioStandAloneMacroSubject()");
        }
            


        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioStandAloneMacroSubject();
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
            //Debug.WriteLine("parsing Attribut in Stand Alone Macro Subject: " + predicate + " content: " + objectContent);
            if (parser is null)
            {
                //Debug.WriteLine("  --- new Simple2DPosParser");
                parser = new Simple2DPosParser(this);
            }
            if (!parser.parseAttribute(predicate, objectContent, lang, dataType, element))
            {
                //Debug.WriteLine("  --- going to base.");
                return base.parseAttribute(predicate, objectContent, lang, dataType, element);
            }

            //Debug.WriteLine("  --- parsing Attribut  Done");

            return true;
           
        }

        public void exportToVisio(Visio.Page pageToExportTo, ISimple2DVisualizationBounds bounds = null)
        {
            // Place a standard actor onto the SID page
            export.export(VisioHelper.ShapeType.SID, pageToExportTo, type,
                                new List<ISimple2DVisualizationPoint>(getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>()), this);


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
                currentSBDPage = VisioHelper.CreateSBDPage(pageToExportTo, ("MBD: " + getModelComponentID()), ("" + getModelComponentID()), this.getShape());
            }

            //Debug.WriteLine("new SBD page created: " + currentSBDPage.NameU);

            IMacroBehavior myBehavior = this.getBehavior();
            //Debug.WriteLine("Behavior exits: " + (myBehavior != null) + " is exportable: " + (myBehavior is IVisioExportable));


            //Now place all of the SBD shapes. It is easier to just interrupt the SID placement and do this now
            if (getBehavior() is IVisioExportable exportable && !(currentSBDPage is null))
            {
                exportable.exportToVisio(currentSBDPage);
            }
            
        }


    }
}
