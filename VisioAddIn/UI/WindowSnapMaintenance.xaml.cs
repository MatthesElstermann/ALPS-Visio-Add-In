using Microsoft.Office.Interop.Visio;
using System.Windows;
using VisioAddIn.Snapping;

namespace VisioAddIn
{
    /// <summary>
    /// interaction logic for WindowSnapMaintenance.xaml
    /// </summary>
    public partial class WindowSnapMaintenance : System.Windows.Window
    {
        public WindowSnapMaintenance()
        {
            InitializeComponent();
        }

        private SbdSnapHandler SnapHandler;

        private Shape Shape;
        private Shape SnapToShape;

        /// <summary>
        /// constructor.
        /// </summary>
        /// <param name="snapHandler">callback</param>
        /// <param name="shape">name of shape</param>
        /// <param name="snapToShape">name of shape should be snapping to</param>
        public WindowSnapMaintenance(SbdSnapHandler snapHandler, Shape shape, Shape snapToShape)
        {
            InitializeComponent();

            this.SnapHandler = snapHandler;
            this.Shape = shape;
            this.SnapToShape = snapToShape;

            Title = VisioAddIn.Resources.strings.WindowSnapConfirmationTitle;
            btnDialogNo.Content = VisioAddIn.Resources.strings.No;
            btnDialogYes.Content = VisioAddIn.Resources.strings.Yes;

            // Fills the two labels with the name of the shapes
            labelShapeSnapName.Content = string.Format(VisioAddIn.Resources.strings.WindowSnapConfirmationShouldShapeSnap, "\"" + shape.Name + "\"");
            labelShapeSnapToName.Content = string.Format(VisioAddIn.Resources.strings.WindowSnapMaintenanceShouldStaySnapped, "\"" + snapToShape.Name + "\"");

        }


        /// <summary>
        /// Called when the Yes-Button is pressed.
        /// Delivers the button-press to the callback (snap handler)
        /// </summary>
        /// <param name="sender">clicked Button</param>
        /// <param name="e">the event that happened</param>
        private void btnDialogYes_Click(object sender, RoutedEventArgs e)
        {
            SnapHandler.maintainSnap(Shape, SnapToShape);
            this.Close();
        }

        /// <summary>
        /// Called when the No-Button is pressed.
        /// Delivers the button-press to the callback (snap handler)
        /// </summary>
        /// <param name="sender">clicked Button</param>
        /// <param name="e">the event that happened</param>
        private void btnDialogNo_Click(object sender, RoutedEventArgs e)
        {
            SnapHandler.unsnap(Shape);
            this.Close();
        }
    }
}
