using Microsoft.Office.Interop.Visio;
using System.Windows;
using VisioAddIn.Snapping;

namespace VisioAddIn
{
    /// <summary>
    /// Interaktionslogik für SnapConfirmation2.xaml
    /// </summary>
    public partial class WindowSnapConfirmation : System.Windows.Window
    {

        private static WindowSnapConfirmation inst;

        private SnapHandler snapHandler;

        private Shape snappingShape;
        private Shape referenceBackgroundShape;

        /// <summary>
        /// constructor.
        /// </summary>
        /// <param name="snapHandler">callback</param>
        /// <param name="snappingShape">name of shape</param>
        /// <param name="referenceBackgroundShape">name of shape should be snapping to</param>
        public WindowSnapConfirmation(SnapHandler snapHandler, Shape snappingShape, Shape referenceBackgroundShape)
        {
            InitializeComponent();

            this.snapHandler = snapHandler;
            this.snappingShape = snappingShape;
            this.referenceBackgroundShape = referenceBackgroundShape;

            // Initialize the GUI dynamically with text (Chooses the right language)

            this.Title = VisioAddIn.Resources.strings.WindowSnapConfirmationTitle;
            btnDialogYes.Content = VisioAddIn.Resources.strings.Yes;
            btnDialogNo.Content = VisioAddIn.Resources.strings.No;

            // Fills the two labels with the name of the shapes
            labelShapeSnapName.Content = string.Format(VisioAddIn.Resources.strings.WindowSnapConfirmationShouldShapeSnap, "\"" + snappingShape.Name + "\"");
            labelShapeSnapToName.Content = string.Format(VisioAddIn.Resources.strings.WindowSnapConfirmationShouldSnapTo, "\"" + referenceBackgroundShape.Name + "\"");

        }


        /// <summary>
        /// Called when the Yes-Button is pressed.
        /// Delivers the button-press to the callback (snap handler)
        /// </summary>
        /// <param name="sender">clicked Button</param>
        /// <param name="e">the event that happened</param>
        private void btnDialogYes_Click(object sender, RoutedEventArgs e)
        {
            snapHandler.performSnap(snappingShape, referenceBackgroundShape);
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
            snapHandler.unsnap(snappingShape);
            this.Close();
        }

    }




}
