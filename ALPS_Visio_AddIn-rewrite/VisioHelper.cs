using alps.net.api.ALPS;
using alps.net.api.StandardPASS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using static Microsoft.Office.Interop.Visio.VisRowTags;
using static Microsoft.Office.Interop.Visio.VisSectionIndices;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite
{
    public static class VisioHelper
    {
        public static void setVBAListenersRunning(Boolean newStatus)
        {
            Visio.IVDocument myActiveDocument = Globals.ThisAddIn.Application.ActiveDocument;

            if (myActiveDocument.DocumentSheet.CellExistsU["Prop." + Constants.Properties.InteropWithVSTOShouldListenersRun, 0] == 0)
            {
                myActiveDocument.DocumentSheet.AddNamedRow((short)visSectionProp, Constants.Properties.InteropWithVSTOShouldListenersRun, (short)visTagDefault);
            }

            myActiveDocument.DocumentSheet.CellsU["Prop." + Constants.Properties.InteropWithVSTOShouldListenersRun].Formula =
                newStatus ? "-1" : "0";
        }

        public enum VisioStencils
        {
            SID_STENCIL,
            SBD_STENCIL
        }

        // Documents.OpenEx und Masters.ItemU sind teure COM-Aufrufe und fielen frueher pro
        // platziertem Shape an (Place -> openStencil -> OpenEx). Die Caches vermeiden das;
        // zeigt ein Eintrag auf ein inzwischen geschlossenes Stencil (COMException), wird
        // er invalidiert und einmal frisch aufgeloest.
        private static readonly Dictionary<VisioStencils, Visio.Document> _stencilCache =
            new Dictionary<VisioStencils, Visio.Document>();
        private static readonly Dictionary<string, Visio.Master> _masterCache =
            new Dictionary<string, Visio.Master>();

        /// <summary>
        /// Opens the latest stencil file from the configured Shapes folder. The opened
        /// document is cached; a closed stencil is detected and reopened.
        /// </summary>
        public static Visio.Document openStencil(VisioStencils stencil)
        {
            if (_stencilCache.TryGetValue(stencil, out Visio.Document cached))
            {
                if (IsAlive(cached)) return cached;
                _stencilCache.Remove(stencil);
            }

            Visio.Documents visioDocs = Globals.ThisAddIn.Application.Documents;
            try
            {
                string fileName = stencil == VisioStencils.SID_STENCIL ? ShapeFinder.getSIDName() : ShapeFinder.getSBDName();
                Visio.Document doc = visioDocs.OpenEx(fileName, (short)Visio.VisOpenSaveArgs.visOpenDocked);
                _stencilCache[stencil] = doc;
                return doc;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                string name = stencil == VisioStencils.SID_STENCIL ? ShapeFinder.getSIDName() : ShapeFinder.getSBDName();
                string kind = stencil == VisioStencils.SID_STENCIL ? "SID" : "SBD";
                UI.ResultDialog.ShowError(kind + "-Schablone nicht gefunden",
                    "Die " + kind + "-Shapes konnten nicht geladen werden.",
                    "Erwartete Datei \"" + name + "\" im Ordner \"Meine Shapes\".\n"
                    + "Pfad (Application.MyShapesPath): " + Globals.ThisAddIn.Application.MyShapesPath + "\n\n"
                    + "Fehler: " + e.Message);
            }
            return null;
        }

        /// <summary>Checks whether a cached COM document is still open (RCW still valid).</summary>
        private static bool IsAlive(Visio.Document doc)
        {
            try
            {
                int _ = doc.ID;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Uebertraegt die benutzerdefinierten Muster-Master (Linien-/Linienenden-/
        /// Fuellmuster) beider ALPS-Stencils in das Zieldokument, sofern dort noch
        /// nicht vorhanden. Die Verbinder-Master zeichnen ihre Pfeilspitzen ueber
        /// USE("…")-Linienmuster (z. B. LinePattern = USE("NewConnectorPattern"));
        /// beim programmatischen <c>page.Drop</c> kopiert Visio solche referenzierten
        /// Muster nicht mit (das Stencil-VBA, das dabei sonst greift, ist waehrend
        /// des Imports deaktiviert). Ohne die Muster im Dokument laufen die
        /// USE-Formeln ins Leere und Visio rendert die Verbinder ohne Pfeilspitzen.
        /// </summary>
        public static void CopyPatternMasters(Visio.Document targetDocument)
        {
            if (targetDocument == null) return;

            foreach (VisioStencils stencilKind in new[] { VisioStencils.SID_STENCIL, VisioStencils.SBD_STENCIL })
            {
                Visio.Document stencil = openStencil(stencilKind);
                if (stencil == null) continue; // openStencil hat den Fehler bereits gemeldet

                foreach (Visio.Master master in stencil.Masters)
                {
                    // Normale Shape-Master kommen regulaer per Drop ins Dokument;
                    // hier interessieren nur die Muster-Typen (Fill/Line/LineEnd).
                    if (master.Type == (short)Visio.VisMasterTypes.visTypeMaster)
                        continue;

                    if (!HasMaster(targetDocument, master.NameU))
                        targetDocument.Masters.Drop(master, 0, 0);
                }
            }
        }

        /// <summary>True, wenn das Dokument bereits einen Master mit diesem Universal-Namen hat.</summary>
        private static bool HasMaster(Visio.Document document, string nameU)
        {
            try
            {
                Visio.Master _ = document.Masters.get_ItemU(nameU);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return false;
            }
        }

        public static Visio.Shape Place(string shapeType, Visio.Page page)
        {
            try
            {
                return page.Drop(GetMaster(shapeType), 0, 0);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Der Master-Cache kann auf ein inzwischen geschlossenes Stencil zeigen —
                // Eintraege invalidieren und genau einmal frisch aufloesen.
                _masterCache.Remove(shapeType);
                _stencilCache.Remove(GetStencil(shapeType));
                return page.Drop(GetMaster(shapeType), 0, 0);
            }
        }

        private static Visio.Master GetMaster(string shapeType)
        {
            if (_masterCache.TryGetValue(shapeType, out Visio.Master cached)) return cached;

            Visio.Document stencil = openStencil(GetStencil(shapeType));
            // openStencil zeigt bei Fehlern bereits eine MessageBox und liefert null — hier
            // mit klarer Ursache abbrechen statt spaeter mit NullReferenceException.
            if (stencil == null)
                throw new InvalidOperationException(
                    "Stencil fuer Master \"" + shapeType + "\" konnte nicht geoeffnet werden — Import abgebrochen.");

            Visio.Master master = stencil.Masters.get_ItemU(shapeType);
            _masterCache[shapeType] = master;
            return master;
        }

        private static readonly HashSet<string> _sidShapeTypes = new HashSet<string>
        {
            Constants.SIDMasters.StandardActor,
            Constants.SIDMasters.InterfaceActor,
            Constants.SIDMasters.CommunicationRestriction,
            Constants.SIDMasters.StandardMessageConnector,
            Constants.SIDMasters.Message,
            Constants.SIDMasters.StandAloneMacro,
            // ALPS SID elements — these are SID masters too; without them GetStencil falls back to
            // the SBD stencil and Place() throws "Objektname nicht gefunden" (e.g. drawing a guard
            // extension during import).
            Constants.SIDMasters.ActorExtension,
            Constants.SIDMasters.SubjectGroup,
            Constants.SIDMasters.AbstractCommunicationChannel,
            Constants.SIDMasters.SystemInterfaceSubject,
        };

        public static VisioStencils GetStencil(string shapeType)
        {
            if (_sidShapeTypes.Contains(shapeType)) return VisioStencils.SID_STENCIL;
            return VisioStencils.SBD_STENCIL;
        }

        // -------------------------------------------------------------------------
        // ShapeSheet property setters
        // -------------------------------------------------------------------------

        /// <summary>
        /// Wraps a value as a Visio ShapeSheet string literal, escaping embedded
        /// double quotes by doubling them (<c>"</c> → <c>""</c>).
        /// </summary>
        public static string QuoteLiteral(object value)
        {
            return "\"" + (value?.ToString() ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Sets <c>Prop.<paramref name="property"/></c> to a quoted string literal.
        /// Embedded double-quotes are escaped automatically.
        /// </summary>
        public static void SetProp(Visio.Shape shape, string property, string value)
        {
            if (shape.CellExistsU["Prop." + property, 0] == 0)
                shape.AddNamedRow((short)visSectionProp, property, (short)visTagDefault);
            shape.CellsU["Prop." + property].FormulaU = QuoteLiteral(value);
        }

        /// <summary>Sets <c>Prop.<paramref name="property"/></c> to <c>TRUE</c> or <c>FALSE</c>.</summary>
        public static void SetPropBool(Visio.Shape shape, string property, bool value)
        {
            if (shape.CellExistsU["Prop." + property, 0] == 0)
                shape.AddNamedRow((short)visSectionProp, property, (short)visTagDefault);
            shape.CellsU["Prop." + property].FormulaU = value ? "TRUE" : "FALSE";
        }

        /// <summary>
        /// Sets <c>Prop.<paramref name="property"/></c> to a raw Visio formula.
        /// The formula is assigned verbatim — no escaping and no <c>=</c> prefix added.
        /// </summary>
        public static void SetPropFormula(Visio.Shape shape, string property, string formula)
        {
            if (shape.CellExistsU["Prop." + property, 0] == 0)
                shape.AddNamedRow((short)visSectionProp, property, (short)visTagDefault);
            shape.CellsU["Prop." + property].FormulaU = formula;
        }

        /// <summary>Sets <c>User.<paramref name="user"/></c> to a quoted string literal.</summary>
        public static void SetUser(Visio.Shape shape, string user, string value)
        {
            if (shape.CellExistsU["User." + user, 0] == 0)
                shape.AddNamedRow((short)visSectionUser, user, (short)visTagDefault);
            shape.CellsU["User." + user].FormulaU = QuoteLiteral(value);
        }

        /// <summary>
        /// Sets <c>Hyperlink.<paramref name="property"/>.Address</c> to a quoted string literal.
        /// Creates the hyperlink row if it does not exist.
        /// </summary>
        public static void SetHyperlink(Visio.Shape shape, string property, string value)
        {
            if (shape.CellExistsU["Hyperlink." + property + ".Address", 0] == 0)
                shape.AddNamedRow((short)visSectionHyperlink, property, (short)visTagDefault);
            shape.CellsU["Hyperlink." + property + ".Address"].FormulaU = QuoteLiteral(value);
        }

        /// <summary>
        /// Sets <c>Hyperlink.<paramref name="property"/>.SubAddress</c> to a quoted string literal.
        /// Creates the hyperlink row if it does not exist. Used for the snapping links
        /// (e.g. <c>extendedSubject</c>), which are read from the SubAddress sub-cell.
        /// </summary>
        public static void SetHyperlinkSubAddress(Visio.Shape shape, string property, string value)
        {
            // Mirror the proven CreateSBDPage pattern: add the hyperlink row if missing, then set
            // SubAddress via the object model (raw value, no formula quoting).
            if (shape.CellExistsU["Hyperlink." + property + ".SubAddress", 0] == 0)
                shape.AddNamedRow((short)Visio.VisSectionIndices.visSectionHyperlink, property, 0);
            shape.Hyperlinks.ItemU[property].SubAddress = value;
        }

        // -------------------------------------------------------------------------
        // Geometry / page cell helpers
        // -------------------------------------------------------------------------

        /// <summary>Sets a geometry or page cell (e.g. PinX, Width, PageWidth) to a numeric value.</summary>
        public static void SetCell(Visio.Shape shape, string cell, double value)
        {
            shape.CellsU[cell].FormulaU = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Sets a geometry or page cell to a value expressed in millimetres.</summary>
        public static void SetCellMM(Visio.Shape shape, string cell, double value)
        {
            shape.CellsU[cell].FormulaU = value.ToString(CultureInfo.InvariantCulture) + " mm";
        }

        /// <summary>Returns the numeric result of a geometry or page cell.</summary>
        public static double GetCell(Visio.Shape shape, string cell)
        {
            return shape.CellsU[cell].Result[""];
        }

        // -------------------------------------------------------------------------
        // Page creation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a page name not yet used by any other page in the document. Visio rejects
        /// duplicate page names; if the desired name is taken, a numeric suffix is appended.
        /// </summary>
        private static string GetUniquePageName(Visio.Page newPage, string desiredName)
        {
            HashSet<string> taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Visio.Page other in newPage.Document.Pages)
            {
                if (other.ID == newPage.ID) continue;
                taken.Add(other.Name);
                taken.Add(other.NameU);
            }

            if (!taken.Contains(desiredName)) return desiredName;
            for (int i = 2; ; i++)
            {
                string candidate = desiredName + "_" + i;
                if (!taken.Contains(candidate)) return candidate;
            }
        }

        /// <summary>
        /// Creates a new SID diagram page and sets all standard PASS properties on it.
        /// </summary>
        public static Visio.Page CreateSIDPage(string name, string nameU, string modelURI, string extends, string implements, string priority)
        {
            Visio.Application addin = Globals.ThisAddIn.Application;
            if (addin.Documents.Count < 1)
                addin.Documents.Add("");

            Visio.Page page = Globals.ThisAddIn.Application.ActiveDocument.Pages.Add();

            // Visio rejects duplicate page names — derive a unique variant before assigning.
            // NameU must mirror Name: a whitespace NameU is invalid and makes Visio fall back
            // to its default ("Zeichenblatt-N"). Reusing the unique Name fixes this and also
            // keeps the SBD→SID hyperlink (which targets NameU) correct.
            page.Name = GetUniquePageName(page, name);
            page.NameU = page.Name;

            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);

            if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageType, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageType].FormulaU = QuoteLiteral(Constants.Properties.SIDPage);

                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageModelURI, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI].FormulaU = QuoteLiteral(modelURI);

                // Required by ModelController.isSid() — existence check only, value is not read
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageModelVersion, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageModelVersion].FormulaU = QuoteLiteral(" ");

                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageLayer, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].FormulaU = QuoteLiteral(nameU);

                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.Transition.Extends, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].FormulaU = QuoteLiteral(extends);

                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.Transition.Implements, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Implements].FormulaU = QuoteLiteral(implements);

                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PriorityOrderNumber, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].FormulaU = QuoteLiteral(priority);

                if (page.Document.DocumentSheet.CellExistsU["Prop." + Constants.Properties.DocumentType, 0] == 0)
                {
                    page.Document.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.DocumentType, 0);
                }
            }
            return page;
        }

        /// <summary>
        /// Creates a new SBD diagram page linked to the given SID page and subject shape.
        /// Precondition: the SID page and document must already exist.
        /// </summary>
        public static Visio.Page CreateSBDPage(Visio.Page sidPage, string name, string nameU, Visio.Shape subjectShape)
        {
            Debug.Print("creating new SBD page");
            Visio.Page page = Globals.ThisAddIn.Application.ActiveDocument.Pages.Add();
            page.Name = GetUniquePageName(page, name);
            page.NameU = GetUniquePageName(page, nameU);

            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionHyperlink, Constants.Properties.LinkedSIDPage, 0);
            page.PageSheet.Hyperlinks.ItemU[Constants.Properties.LinkedSIDPage].SubAddress = sidPage.NameU;

            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);

            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageLayer, 0);
            page.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].FormulaU =
                QuoteLiteral(sidPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].ResultStr[""]);

            if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageType, 0);
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.SBDLinkedSubjectID, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageType].FormulaU = QuoteLiteral(Constants.Properties.SBDPage);
                page.PageSheet.CellsU["Prop." + Constants.Properties.SBDLinkedSubjectID].FormulaU = subjectShape.ID.ToString();
            }

            // Link the subject shape to its new behaviour page. The StandardActor master ships with
            // a linkedSBD hyperlink row, but the ActorExtension master (subject/guard/macro extensions)
            // does not — add the row first so creating a GBD for an extension does not throw
            // "Objektname nicht gefunden".
            if (subjectShape.CellExistsU["Hyperlink." + Constants.Properties.LinkedSBD + ".SubAddress", 0] == 0)
                subjectShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionHyperlink, Constants.Properties.LinkedSBD, 0);
            subjectShape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress = "" + page.NameU;

            return page;
        }

        // -------------------------------------------------------------------------
        // Model element helpers
        // -------------------------------------------------------------------------

        public static List<ISimple2DVisualizationPoint> GetBounds(PASSProcessModelElement element)
        {
            return new List<ISimple2DVisualizationPoint>(element.getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>());
        }
    }
}
