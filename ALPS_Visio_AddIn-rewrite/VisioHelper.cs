using System;
using System.Collections;
using VisioAddIn;
using Visio = Microsoft.Office.Interop.Visio;
using static Microsoft.Office.Interop.Visio.VisSectionIndices;
using static Microsoft.Office.Interop.Visio.VisRowTags;
using System.Diagnostics;

namespace ALPS_Visio_AddIn_rewrite
{
    public static class VisioHelper // TODO: place
    {
        public static void setVBAListenersRunning(Boolean newStatus)
        {
            Visio.IVDocument myActiveDocument = Globals.ThisAddIn.Application.ActiveDocument;

            if (myActiveDocument.DocumentSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun, 0] == 0)
            {
                myActiveDocument.DocumentSheet.AddNamedRow((short)visSectionProp, ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun, (short)visTagDefault);
            }

            if (newStatus)
            {
                myActiveDocument.DocumentSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun].Formula = "-1";
            }
            else
            {
                myActiveDocument.DocumentSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun].Formula = "0";
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


            page.Name = name;
            page.NameU = nameU;

            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);

            if (page.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePageType, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageType].FormulaU = "\"" + ALPSConstants.alpsPropertieValueSIDPage + "\"";

                //add and set "Model Name"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePageModelURI, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageModelURI].FormulaU = "\"" + modelURI + "\"";

                //add and set "layer"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePageLayer, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageLayer].FormulaU = "\"" + nameU + "\"";


                //add and set "extends"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypeExtends, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeExtends].FormulaU = "\"" + extends + "\"";

                //add and set "implements"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertyTypeImplements, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertyTypeImplements].FormulaU = "\"" + implements + "\"";

                //add and set "execution priority"
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePriorityOrderNumber, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePriorityOrderNumber].FormulaU = "\"" + priority + "\"";

                if (page.Document.DocumentSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypeDocumentType, 0] == 0)
                {
                    page.Document.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypeDocumentType, 0);
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
            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionHyperlink, ALPSConstants.alpsHyperlinksLinkedSIDPage, 0);
            page.PageSheet.Hyperlinks.ItemU[ALPSConstants.alpsHyperlinksLinkedSIDPage].SubAddress = sidPage.NameU;
            page.PageSheet.AddSection((short)Visio.VisSectionIndices.visSectionProp);
            //page layer props
            page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePageLayer, 0);
            page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageLayer].FormulaU =
                "\"" + sidPage.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageLayer].ResultStr[""] + "\"";

            if (page.PageSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypePageType, 0] == 0)
            {
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypePageType, 0);
                page.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionProp, ALPSConstants.alpsPropertieTypeSBDLinkedSubjectID, 0);
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypePageType].FormulaU =
                    "\"" + ALPSConstants.alpsPropertieValueSBDPage + "\"";
                page.PageSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeSBDLinkedSubjectID].FormulaU = subjectShape.ID.ToString();
            }

            //remove comment when it is assured that shapes are only valid s-bpm elements.
            subjectShape.Hyperlinks.ItemU[ALPSConstants.alpsHyperlinkTypeLinkedSBD].SubAddress = "" + page.NameU + "";

            return page;
        }

        public static Visio.Pages getCurrentPages()
        {
            return ThisAddIn.getInstance().Application.ActiveDocument.Pages;
        }

        public static Visio.Page getPageInPages(Visio.Pages pages, int index)
        {
            if (index >= pages.Count || index < 0) return null;
            IEnumerator enumerator = pages.GetEnumerator();
            for (int x = 0; x < pages.Count; x++)
            {
                enumerator.MoveNext();
                if (index == x)
                    return (Visio.Page)enumerator.Current;
            }
            return null;
        }
    }
}