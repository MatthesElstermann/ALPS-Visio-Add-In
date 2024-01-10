using alps.net.api.ALPS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using VDS.RDF.Query.Expressions.Functions.Sparql.String;
using VisioAddIn.OwlShapes;
using Visio = Microsoft.Office.Interop.Visio;
using static Microsoft.Office.Interop.Visio.VisSectionIndices;
using static Microsoft.Office.Interop.Visio.VisRowTags;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Globalization;

namespace VisioAddIn
{
    public static class VisioHelper
    {

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


        public enum ShapeType
        {
            SBD, SID
        }

        private static IDictionary<int, IList<ISimple2DVisualizationPoint>> placedPointsOnPage = new Dictionary<int, IList<ISimple2DVisualizationPoint>>();

        private static void determinPlacingCoordinatesOnSID(Visio.Page page, out double posX, out double posY, 
            IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalModelElement = null)
        {
            
            ISimple2DVisualizationBounds bound = null;
            double defaultX = 2;//4.25;
            double defaultY = 10;//5.5;
            posX = defaultX;
            posY = defaultY;

            
            if (!(points is null) && (points.Count > 0))
            {
                foreach (ISimple2DVisualizationPoint point in points)
                {
                    if (!(point is ISimple2DVisualizationBounds))
                    {
                        posX = point.getRelative2DPosX();
                        posY = point.getRelative2DPosY();

                        
                        //Scale for real page if applicable
                        if (page.PageSheet.CellExistsU["User.OWLIMPORTINFORATIOMOD", 0] == -1)
                        {
                            Debug.WriteLine("Original posX/Y: (" + posX+","+posY+") , " +
                                " pagewidht: " + page.PageSheet.CellsU["PageWidth"].Result[""] + 
                                " pageHeight: " + page.PageSheet.CellsU["PageHeight"].Result[""]);
                            posX = posX * page.PageSheet.CellsU[ALPSConstants.pageCellPagePropertiesPageWidth].Result[""];
                            posY = posY * page.PageSheet.CellsU[ALPSConstants.pageCellPagePropertiesPageHeight].Result[""];
                            Debug.WriteLine("Simple sim positioning " + originalModelElement.getModelComponentID() + " at(x,y): " + posX + "," + posY + ")");

                        }
                    }
                    else if (point is ISimple2DVisualizationBounds boundObject)
                    {
                        bound = boundObject;
                    }
                }
            }
            /*
            else if (hasSimple2DVisCoordinates(originalModelElement)) //for simple 2D Shapes 
            {
                if (page.PageSheet.CellExistsU["User.OWLIMPORTINFORATIOMOD", 0] == 1)
                {
                    IHasSimple2DVisualizationBox mySimp2D = (IHasSimple2DVisualizationBox)originalModelElement;
                   
                    //originalModelElement is IHasSimple2DVisualizationBox
                    if (page.PageSheet.CellExistsU["User.OWLIMPORTINFORATIOMOD", 0] == 0)
                    {
                        posX = mySimp2D.getRelative2DPosX() * page.PageSheet.CellsU["PageWidth"].Result[""];
                        posY = mySimp2D.getRelative2DPosY() * page.PageSheet.CellsU["PageHeight"].Result[""];
                        Debug.WriteLine("Simple sim positioning " + originalModelElement.getModelComponentID() + " at(x,y): " + posX + "," + posY + ")");
                    }

                }
            }*/
            // Else automatic mode
            else if (placedPointsOnPage.TryGetValue(page.ID, out IList<ISimple2DVisualizationPoint> pointList))
            {
                ISimple2DVisualizationPoint lastPoint = pointList[pointList.Count - 1];
                posX = lastPoint.getRelative2DPosX() + 4;
                posY = lastPoint.getRelative2DPosY() + 4;
                if (posX > 10)
                {
                    posX = 2;
                    posY -= 4;
                }
            }
        }

        private static bool hasSimple2DVisCoordinates(IPASSProcessModelElement originalModelElement)
        {
            bool result = false;

            if (originalModelElement is IHasSimple2DVisualizationBox mySimp2D)
            {
                if(mySimp2D.get2DPageRatio() > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        public static Visio.Shape place(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null)
        {
            Visio.Document shapes = null;
            
            double placingPosX = 0, placingPosY = 0;
            switch (shapeType)
            {
                case ShapeType.SBD:
                    shapes = VisioHelper.openStencil(VisioStencils.SBD_STENCIL);
                    break;
                case ShapeType.SID:
                    shapes = VisioHelper.openStencil(VisioStencils.SID_STENCIL);
                    determinPlacingCoordinatesOnSID(page, out placingPosX, out placingPosY, points, originalElement);
                    break;
            }

            if (shapes != null)
            {
                Visio.Master sidMaster = shapes.Masters.get_ItemU(masterType);

                // Keep track of all the points shapes have been placed to
                ISimple2DVisualizationPoint tempPlacingPoint = new Simple2DVisualizationPoint();
                tempPlacingPoint.setRelative2DPosX(placingPosX);
                tempPlacingPoint.setRelative2DPosY(placingPosY);

                if (placedPointsOnPage.ContainsKey(page.ID))
                    placedPointsOnPage[page.ID].Add(tempPlacingPoint);
                else
                    placedPointsOnPage.Add(page.ID, new List<ISimple2DVisualizationPoint> { tempPlacingPoint });
                
                
                Visio.Shape droppedShape = page.Drop(sidMaster, placingPosX, placingPosY);

              

                Debug.WriteLine("Dropped Shape:  " + masterType + " on Page: " + page.ID + " at coordinates (X/Y): (" + placingPosX + "," + placingPosY + ")" +
                                                    " (name: " + page.NameU + 
                                                    ", width: " + (page.PageSheet.CellsU["PageWidth"].FormulaU) + 
                                                    ", height: "  + (page.PageSheet.CellsU["PageHeight"].FormulaU) + ")");

                return droppedShape;
            }
            return null;

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
            Debug.Print ("creating new SBD page");
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

        public static void toggleVBAListeners()
        {
            Debug.Print("Starting toggleVBAListeners");
            Visio.IVDocument myActiveDocument = Globals.ThisAddIn.Application.ActiveDocument;
            if (myActiveDocument.DocumentSheet.CellExistsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun, 0] == 0)
            {
                myActiveDocument.DocumentSheet.AddNamedRow((short)visSectionProp, ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun, (short)visTagDefault);
            }

            double status = 0.0;
            status = myActiveDocument.DocumentSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun].Result[""];

            Debug.Print("status before: " + status);

            
            if(status == 0.0) {
                switchVBAListenersON();
            }
            else
            {
                switchVBAListenersOFF();
            };

            status = myActiveDocument.DocumentSheet.CellsU["Prop." + ALPSConstants.alpsPropertieTypeInteropWithVSTOShouldListenersRun].Result[""];

            Debug.Print("status after: " + status);

        }

        public static void switchVBAListenersON() { setVBAListenersRunning(true); }
        public static void switchVBAListenersOFF() { setVBAListenersRunning(false); }
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
            };
        }

    }
}
