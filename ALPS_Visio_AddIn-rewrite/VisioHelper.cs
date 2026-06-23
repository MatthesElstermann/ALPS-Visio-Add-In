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

        /// <summary>
        /// Opens the latest stencil file from the configured Shapes folder.
        /// </summary>
        public static Visio.Document openStencil(VisioStencils stencil)
        {
            Visio.Documents visioDocs = Globals.ThisAddIn.Application.Documents;
            try
            {
                switch (stencil)
                {
                    case VisioStencils.SID_STENCIL:
                        return visioDocs.OpenEx(ShapeFinder.getSIDName(), (short)Visio.VisOpenSaveArgs.visOpenDocked);
                    case VisioStencils.SBD_STENCIL:
                        return visioDocs.OpenEx(ShapeFinder.getSBDName(), (short)Visio.VisOpenSaveArgs.visOpenDocked);
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                string name = stencil == VisioStencils.SID_STENCIL ? ShapeFinder.getSIDName() : ShapeFinder.getSBDName();
                string msg = "Failed to load SID Shapes. Expecting file \"" + name + "\" to exist in the \"My Shapes\" folder.\n"
                           + "My Shapes path (Application.MyShapesPath): " + Globals.ThisAddIn.Application.MyShapesPath + "\n"
                           + "Error: " + e.Message;
                System.Windows.Forms.MessageBox.Show(msg);
            }
            return null;
        }

        public static Visio.Shape Place(string shapeType, Visio.Page page)
        {
            Visio.Document stencil = openStencil(GetStencil(shapeType));
            Visio.Master sidMaster = stencil.Masters.get_ItemU(shapeType);
            return page.Drop(sidMaster, 0, 0);
        }

        public static VisioStencils GetStencil(string shapeType)
        {
            var field = typeof(VisioAddIn.ALPSConstants).GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.FlattenHierarchy)
                .FirstOrDefault(f => f.IsLiteral && !f.IsInitOnly &&
                                     (f.Name.Contains("SID") || f.Name.Contains("SBD")) &&
                                     f.GetRawConstantValue().ToString() == shapeType);

            if (field.Name.Contains("SID")) return VisioStencils.SID_STENCIL;
            if (field.Name.Contains("SBD")) return VisioStencils.SBD_STENCIL;
            throw new ArgumentException();
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
