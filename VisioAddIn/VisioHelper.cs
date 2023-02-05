using alps.net.api.ALPS;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using VisioAddIn.OwlShapes;
using Visio = Microsoft.Office.Interop.Visio;

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

        

        ///// <summary>
        ///// places a shape on the given visio page
        ///// </summary>
        ///// <param name="page">visio page</param>
        ///// <param name="shapeName">master name of the shape</param>
        //public static Visio.Shape PlaceSidShape(Visio.Page page, string shapeName, double xPos = 4.25, double yPos = 5.5)
        //{
        //    Visio.Document sidShapes = VisioHelper.openStencil(VisioStencils.SID_STENCIL);
        //    Visio.Master sidMaster = sidShapes.Masters.get_ItemU(shapeName);
        //    Visio.Shape shape = page.Drop(sidMaster, xPos, yPos);
        //    return shape;
        //}

        ///// <summary>
        ///// places a shape on the given visio page
        ///// </summary>
        ///// <param name="page">visio page</param>
        ///// <param name="shapeName">master name of the shape</param>
        //public static Visio.Shape PlaceSbdShape(Visio.Page page, string shapeName, double xPos = 4.25, double yPos = 5.5)
        //{
        //    Visio.Document sbdShapes = VisioHelper.openStencil(SBD);
        //    Visio.Master sbdMaster = sbdShapes.Masters.get_ItemU(shapeName);
        //    Visio.Shape shape = page.Drop(sbdMaster, xPos, yPos);
        //    return shape;
        //}

        public enum ShapeType
        {
            SBD, SID
        }

        private static IDictionary<int, IList<ISimple2DVisualizationPoint>> placedPoints = new Dictionary<int, IList<ISimple2DVisualizationPoint>>();

        private static void placeSIDShape(Visio.Page page, out double simpleXPos, out double simpleYPos, IList<ISimple2DVisualizationPoint> points = null)
        {
            ISimple2DVisualizationBounds bound = null;
            double defaultX = 2;//4.25;
            double defaultY = 10;//5.5;
            simpleXPos = defaultX;
            simpleYPos = defaultY;

            if (!(points is null) && (points.Count > 0))
                foreach (ISimple2DVisualizationPoint point in points)
                {
                    if (!(point is ISimple2DVisualizationBounds))
                    {
                        simpleXPos = point.getRelative2D_PosX();
                        simpleYPos = point.getRelative2D_PosX();
                    }
                    else if (point is ISimple2DVisualizationBounds boundObject)
                    {
                        bound = boundObject;
                    }
                }
            else if (placedPoints.TryGetValue(page.ID, out IList<ISimple2DVisualizationPoint> pointList))
            {
                ISimple2DVisualizationPoint lastPoint = pointList[pointList.Count - 1];
                simpleXPos = lastPoint.getRelative2D_PosX() + 4;
                simpleYPos = lastPoint.getRelative2D_PosY();
                if (simpleXPos > 10)
                {
                    simpleXPos = 2;
                    simpleYPos -= 4;
                }
            }
        }

        //private static void placeSBDShape(Visio.Page page, out double simpleXPos, out double simpleYPos, IList<ISimple2DVisualizationPoint> points = null)
        //{
        //    ISimple2DVisualizationBounds bound = null;
        //    double defaultX = 2;//4.25;
        //    double defaultY = 10;//5.5;
        //    simpleXPos = defaultX;
        //    simpleYPos = defaultY;

        //    if (!(points is null) && (points.Count > 0))
        //        foreach (ISimple2DVisualizationPoint point in points)
        //        {
        //            if (!(point is ISimple2DVisualizationBounds))
        //            {
        //                simpleXPos = point.getRelative2D_PosX();
        //                simpleYPos = point.getRelative2D_PosX();
        //            }
        //            else if (point is ISimple2DVisualizationBounds boundObject)
        //            {
        //                bound = boundObject;
        //            }
        //        }
        //    else if (placedPoints.TryGetValue(page.ID, out IList<ISimple2DVisualizationPoint> pointList))
        //    {
        //        ISimple2DVisualizationPoint lastPoint = pointList[pointList.Count - 1];
        //        simpleXPos = lastPoint.getRelative2D_PosX() + 4;
        //        simpleYPos = lastPoint.getRelative2D_PosY();
        //        if (simpleXPos > 10)
        //        {
        //            simpleXPos = 2;
        //            simpleYPos -= 4;
        //        }
        //    }
        //}

        public static Visio.Shape place(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null)
        {
            Visio.Document shapes = null;
            double simpleXPos = 0, simpleYPos = 0;
            switch (shapeType)
            {
                case ShapeType.SBD:
                    shapes = VisioHelper.openStencil(VisioStencils.SBD_STENCIL);
                    break;
                case ShapeType.SID:
                    shapes = VisioHelper.openStencil(VisioStencils.SID_STENCIL);
                    placeSIDShape(page, out simpleXPos, out simpleYPos, points);
                    break;
            }
            if (shapes != null)
            {
                Visio.Master sidMaster = shapes.Masters.get_ItemU(masterType);

                // Keep track of all the points shapes have been placed to
                ISimple2DVisualizationPoint placedPoint = new Simple2DVisualizationPoint();
                placedPoint.setRelative2D_PosX(simpleXPos);
                placedPoint.setRelative2D_PosY(simpleYPos);
                if (placedPoints.ContainsKey(page.ID))
                    placedPoints[page.ID].Add(placedPoint);
                else
                    placedPoints.Add(page.ID, new List<ISimple2DVisualizationPoint> { placedPoint });
                Visio.Shape droppedShape = page.Drop(sidMaster, simpleXPos, simpleYPos);
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

        //public static void refreshDocument()
        //{
        //    Visio.Application addin = Globals.ThisAddIn.Application;
        //    Visio.Document document = addin.ActiveDocument;
        //    Visio.Page page = Globals.ThisAddIn.Application.ActivePage;



        //}

        public static Visio.Pages getCurrentPages()
        {
            return ThisAddIn.getInstance().Application.ActiveDocument.Pages;
        }

    }
}
