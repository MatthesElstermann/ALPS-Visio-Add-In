using alps.net.api.ALPS;
using System;
using System.Collections.Generic;
using Visio = Microsoft.Office.Interop.Visio;
using static VisioAddIn.VisioHelper;
using alps.net.api.StandardPASS;

namespace VisioAddIn.OwlShapes
{
    /// <summary>
    /// An interface that allows to extract export functionality that is similar for multiple elements.
    /// 
    /// Example: A VisioDoTransition and a VisioReceiveTransition are very similar to export
    /// (both need ID, comment, labels, sending state and receiving state), but they each extend the twin classes in the alps.net.api,
    /// the DoTransition and the ReceiveTransition. They do not extend a class like VisioTransition,
    /// which would contain the shared export functionality, since multiple inheritance is not possible in C#.
    /// The shared functionality is therefor extracted into a TransitionExport class, which defines the shared functionality to export transitions.
    /// It extends the PASSProcessModelElementExport class, which defines the shared functionality to export any element (it sets ID, comments, type...).
    /// This design is chosen to not define code redundandly in each class.
    /// </summary>
    public interface IExportFunctionality
    {
        void export(ShapeType shapeType, Visio.Page page, string masterType, IList<ISimple2DVisualizationPoint> points = null, IPASSProcessModelElement originalElement = null);

        Visio.Shape getShape();

        void setShape(Visio.Shape shape);
    }
}
