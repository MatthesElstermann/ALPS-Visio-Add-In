using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using System.Collections.Generic;
using System.Linq;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioMessageExchangeList : MessageExchangeList, IVisioImportable
    {
        public VisioMessageExchangeList(IModelLayer layer) : base(layer) { }
        protected VisioMessageExchangeList() { }

        public void ImportToVisio(Visio.Page page)
        {
            if (this.getMessageExchanges().Values.FirstOrDefault() is IVisioImportableWithShape messageExchangeWithConnector)
            {
                // store previous shapes
                List<Visio.Shape> previousShapes = new List<Visio.Shape>();
                foreach (Visio.Shape shape in page.Shapes) previousShapes.Add(shape);

                messageExchangeWithConnector.ImportToVisio(page);

                // find message box
                Visio.Shape messageBox = null;
                foreach (Visio.Shape shape in page.Shapes)
                    if (shape.CellExistsU["User.idOnPage", 0] != 0 &&
                        shape.CellsU["User.idOnPage"].Result[""] == messageExchangeWithConnector.GetShape().CellsU["User.idOfCorrespondingShape"].Result[""])
                    {
                        messageBox = shape;
                        break;
                    }

                // Die Message-Box legt die STENCIL-VBA per EventDrop an, wenn der
                // Connector gedroppt wird. Ist der VBA-Zustand kaputt (typisch: vorher
                // ein VBA-Laufzeitfehler, z. B. nach Modell-Erstellen + -Loeschen in
                // derselben Sitzung), entsteht keine Box -- dann hier mit klarer
                // Handlungsanweisung abbrechen statt mit NullReferenceException.
                if (messageBox == null)
                    throw new System.InvalidOperationException(
                        "Die Message-Box wurde beim Import nicht erzeugt. Das passiert, wenn die " +
                        "VBA-Makros des SID-Stencils nicht (mehr) laufen — typischerweise nach einem " +
                        "vorherigen VBA-Fehler in dieser Visio-Sitzung. Bitte Visio neu starten und " +
                        "den Import wiederholen.");

                // delete wrong shapes
                // alternative idea: delete messages with default label (or label == id)
                foreach (Visio.Shape shape in page.Shapes)
                    if (!previousShapes.Contains(shape) && shape != messageExchangeWithConnector.GetShape() && shape != messageBox) shape.Delete();

                // center message box
                messageBox.CellsU["Actions.Center.Action"].Trigger();

                // aggregate list
                foreach (IMessageExchange messageExchange in this.getMessageExchanges().Values)
                {
                    if (messageExchange.getMessageType() is IVisioImportableWithShape importable)
                    {
                        importable.ImportToVisio(page);

                        messageBox.ContainerProperties.InsertListMember(importable.GetShape(), 0);
                        importable.GetShape().BringToFront();
                    }
                }
            }
        }

        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioMessageExchangeList();
        }
    }
}