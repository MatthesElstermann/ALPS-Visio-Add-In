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
    public static class VisioHelper // TODO: docs, rework
    {
        public static void setVBAListenersRunning(Boolean newStatus)
        {
            Visio.IVDocument myActiveDocument = Globals.ThisAddIn.Application.ActiveDocument;

            if (myActiveDocument.DocumentSheet.CellExistsU["Prop." + Constants.Properties.InteropWithVSTOShouldListenersRun, 0] == 0)
            {
                myActiveDocument.DocumentSheet.AddNamedRow((short)visSectionProp, Constants.Properties.InteropWithVSTOShouldListenersRun, (short)visTagDefault);
            }

            if (newStatus)
            {
                myActiveDocument.DocumentSheet.CellsU["Prop." + Constants.Properties.InteropWithVSTOShouldListenersRun].Formula = "-1";
            }
            else
            {
                myActiveDocument.DocumentSheet.CellsU["Prop." + Constants.Properties.InteropWithVSTOShouldListenersRun].Formula = "0";
            }
            ;
        }

        public enum VisioStencils
        {
            SID_STENCIL,
            SBD_STENCIL
        }

        /// <summary>
        /// Opens the latest SID-Stencil file from specified shape-folder
        /// </summary>
        /// <returns>The specified stencil file or null</returns>
        public static Visio.Document openStencil(VisioStencils stencil)
        {
            Visio.Documents visioDocs = Globals.ThisAddIn.Application.Documents;
            try
            {
                switch (stencil)
                {
                    case VisioStencils.SID_STENCIL:
                        Visio.Document sidShapes = visioDocs.OpenEx(ShapeFinder.getSIDName(),
                            (short)Visio.VisOpenSaveArgs.visOpenDocked);
                        return sidShapes;
                    case VisioStencils.SBD_STENCIL:
                        Visio.Document sbdShapes = visioDocs.OpenEx(ShapeFinder.getSBDName(),
                            (short)Visio.VisOpenSaveArgs.visOpenDocked);
                        return sbdShapes;
                }

            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                string msg = "Failed to load SID Shapes. Expecting file \"";
                switch (stencil)
                {
                    case VisioStencils.SID_STENCIL:
                        msg += ShapeFinder.getSIDName();
                        break;
                    case VisioStencils.SBD_STENCIL:
                        msg += ShapeFinder.getSBDName();
                        break;
                }
                msg += "\" to exist in \"my Shapes\" folder.\n";
                msg += "Error: " + e.Message;
                System.Windows.Forms.MessageBox.Show(msg);
            }
            return null;
        }

        public enum ShapeType
        {
            SBD, SID
        }

        public static Visio.Shape Place(string shapeType, Visio.Page page)
        {
            Visio.Document stencil = VisioHelper.openStencil(VisioHelper.GetStencil(shapeType));

            Visio.Master sidMaster = stencil.Masters.get_ItemU(shapeType);

            Visio.Shape droppedShape = page.Drop(sidMaster, 0, 0);

            return droppedShape;
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

            if (field.Name.Contains("SID"))
                return VisioStencils.SID_STENCIL;

            if (field.Name.Contains("SBD"))
                return VisioStencils.SBD_STENCIL;

            throw new ArgumentException();
        }

        public static void SetHyperlink(Visio.Shape shape, string property, string value)
        {
            if (shape.CellExistsU["Hyperlink." + property, 0] == 0)
                shape.AddNamedRow((short)visSectionHyperlink, property, (short)visTagDefault);
            //shape.Hyperlinks.ItemU["Hyperlink." + property].Address = "\"" + value + "\""; // shape.Hyperlinks.ItemU does not exist idk
        }
        public static void SetProperty(Visio.Shape shape, string property, string value)
        {
            if (shape.CellExistsU["Prop." + property, 0] == 0) shape.AddNamedRow((short)visSectionProp, property, (short)visTagDefault);

            shape.CellsU["Prop." + property].Formula = "\"" + value + "\"";
        }
        public static void SetBool(Visio.Shape shape, string property, bool value)
        {
            if (shape.CellExistsU["Prop." + property, 0] == 0) shape.AddNamedRow((short)visSectionProp, property, (short)visTagDefault);

            shape.CellsU["Prop." + property].Formula = value ? "=TRUE" : "=FALSE";
        }
        public static void SetSize(Visio.Shape shape, string cell, double value)
        {
            if (shape.CellExistsU[cell, 0] == 0) shape.AddNamedRow((short)visSectionNone, cell, (short)visTagDefault);

            shape.CellsU[cell].Formula = value.ToString(CultureInfo.InvariantCulture);
        }
        public static double GetSize(Visio.Shape shape, string cell)
        {
            return shape.CellsU[cell].Result[""];
        }

        public static void SetPropertyU(Visio.Shape shape, string property, object value)
        {
            SetCell(shape, visSectionProp, property, CellFormulaMode.U, CellValueType.Normal, value);
        }
        public static void SetPropertyULiteral(Visio.Shape shape, string property, object value)
        {
            SetCell(shape, visSectionProp, property, CellFormulaMode.U, CellValueType.Literal, value);
        }
        public static void SetPropertyFormulaU(Visio.Shape shape, string property, object value)
        {
            SetCell(shape, visSectionProp, property, CellFormulaMode.U, CellValueType.Formula, value);
        }
        public static void SetUser(Visio.Shape shape, string user, object value)
        {
            SetCell(shape, visSectionUser, user, CellFormulaMode.Normal, CellValueType.Literal, value);
        }
        public static void SetSizeMM(Visio.Shape shape, string cell, object value)
        {
            SetCell(shape, null, cell, CellFormulaMode.U, CellValueType.Size, value);
        }
        private enum CellFormulaMode
        {
            Normal,
            U,
            Force,
            ForceU
        }
        private enum CellValueType
        {
            Literal,
            Formula,
            Size,
            Normal
        }
        private static void SetCell(Visio.Shape shape, Visio.VisSectionIndices? section, string rowName, CellFormulaMode formulaMode, CellValueType valueType, object value)
        {
            string sectionName = "";
            switch (section)
            {
                case visSectionProp: sectionName = "Prop."; break;
                case visSectionUser: sectionName = "User."; break;
            }

            // Ensure row exists
            if (shape.CellExistsU[sectionName + rowName, 0] == 0)
            {
                shape.AddNamedRow((short)section, rowName, (short)visTagDefault);
            }

            // Get the cell
            Visio.Cell cell = shape.CellsU[sectionName + rowName];

            // Convert value properly
            if (value is IFormattable formattable) value = formattable.ToString(null, CultureInfo.InvariantCulture);

            // Build the value string
            string valueString = "";
            switch (valueType)
            {
                case CellValueType.Formula: valueString = "=" + value; break;
                case CellValueType.Size: valueString = value + " mm"; break;
                case CellValueType.Literal: valueString = "\"" + value + "\""; break;
                case CellValueType.Normal: valueString = "" + value; break;
            }

            // Apply according to formula mode
            switch (formulaMode)
            {
                case CellFormulaMode.Normal: cell.Formula = valueString; break;
                case CellFormulaMode.U: cell.FormulaU = valueString; break;
                case CellFormulaMode.Force: cell.FormulaForce = valueString; break;
                case CellFormulaMode.ForceU: cell.FormulaForceU = valueString; break;
            }
        }

        /// <summary>
        /// creates a new diagram page in visio
        /// and turns it into a sid page by setting all given parameters.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nameU"></param>
        /// <param name="modelURI"></param>
        /// <param name="extends"></param>
        /// <param name="implements"></param>
        /// <param name="priority"></param>
        /// <returns>created visio page</returns>
        public static Visio.Page CreateSIDPage(string name, string nameU, string modelURI, string extends, string implements, string priority)
        {
            Visio.Application addin = Globals.ThisAddIn.Application;
            if (addin.Documents.Count < 1)
            {
                addin.Documents.Add("");
            }
            Visio.Page page = Globals.ThisAddIn.Application.ActiveDocument.Pages.Add();

            // TODO: check if name already exists; if so, then change it in a meaningful way
            page.Name = name;
            page.NameU = nameU;

            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);

            if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageType, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageType].FormulaU = "\"" + Constants.Properties.SIDPage + "\"";

                //add and set "Model Name"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageModelURI, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageModelURI].FormulaU = "\"" + modelURI + "\"";

                //add and set "layer"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageLayer, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].FormulaU = "\"" + nameU + "\"";


                //add and set "extends"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.Transition.Extends, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Extends].FormulaU = "\"" + extends + "\"";

                //add and set "implements"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.Transition.Implements, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.Transition.Implements].FormulaU = "\"" + implements + "\"";

                //add and set "execution priority"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PriorityOrderNumber, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PriorityOrderNumber].FormulaU = "\"" + priority + "\"";

                if (page.Document.DocumentSheet.CellExistsU["Prop." + Constants.Properties.DocumentType, 0] == 0)
                {
                    page.Document.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.DocumentType, 0);
                }
            }
            return page;
        }

        /// <summary>
        /// precondition: document and matching sid page already exist.
        /// </summary>
        /// <param name="sidPage"></param>
        /// <param name="name"></param>
        /// <param name="subjectShape">the subject the page belongs to</param>
        public static Visio.Page CreateSBDPage(Visio.Page sidPage, string name, string nameU, Visio.Shape subjectShape)
        {
            Debug.Print("creating new SBD page");
            Visio.Page page = Globals.ThisAddIn.Application.ActiveDocument.Pages.Add();
            page.Name = name;
            page.NameU = nameU;
            //hyperlinks
            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionHyperlink, Constants.Properties.LinkedSIDPage, 0);
            page.PageSheet.Hyperlinks.ItemU[Constants.Properties.LinkedSIDPage].SubAddress = sidPage.NameU;
            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);
            //page layer props
            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageLayer, 0);
            page.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].FormulaU =
                "\"" + sidPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].ResultStr[""] + "\"";

            if (page.PageSheet.CellExistsU["Prop." + Constants.Properties.PageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.PageType, 0);
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, Constants.Properties.SBDLinkedSubjectID, 0);
                page.PageSheet.CellsU["Prop." + Constants.Properties.PageType].FormulaU =
                    "\"" + Constants.Properties.SBDPage + "\"";
                page.PageSheet.CellsU["Prop." + Constants.Properties.SBDLinkedSubjectID].FormulaU = subjectShape.ID.ToString();
            }

            //remove comment when it is assured that shapes are only valid s-bpm elements.
            subjectShape.Hyperlinks.ItemU[Constants.Properties.LinkedSBD].SubAddress = "" + page.NameU + "";

            return page;
        }

        public static List<ISimple2DVisualizationPoint> GetBounds(PASSProcessModelElement element)
        {
            return new List<ISimple2DVisualizationPoint>(element.getElementsWithUnspecifiedRelation().Values.OfType<ISimple2DVisualizationPoint>());
        }
    }
}