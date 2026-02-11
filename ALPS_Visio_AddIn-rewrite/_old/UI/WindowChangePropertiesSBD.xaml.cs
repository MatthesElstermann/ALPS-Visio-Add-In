
using System.Windows;
using VisioAddIn.Snapping;

namespace VisioAddIn
{
    /// <summary>
    /// Interaktionslogik für WindowChangePropertiesSBD.xaml
    /// </summary>
    public partial class WindowChangePropertiesSbd : Window
    {

        private ALPS_Visio_AddIn_rewrite.ThisAddIn addIn;
        private readonly ModelController modelController;
        private readonly SBDPage sbdPage;
        private readonly IDialogCallback callback;

        public WindowChangePropertiesSbd(IDialogCallback callback, ALPS_Visio_AddIn_rewrite.ThisAddIn addIn, ModelController modelController, SBDPage sbdPage)
        {
            InitializeComponent();

            this.callback = callback;
            this.addIn = addIn;
            this.modelController = modelController;
            this.sbdPage = sbdPage;


            if (this.sbdPage.getExtends() != null)
            {
                chooseSep.Content = string.Format(ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesChooseSepExtended, "\"" + this.sbdPage.getExtends().getNameU() + "\""); ;
            }
            else
            {
                chooseSep.Content = "";
                btnDialogOK.Visibility = Visibility.Collapsed;
                btnFullSeparation.Visibility = Visibility.Collapsed;
                btnNormalSeparation.Visibility = Visibility.Collapsed;
                btnNoSeparation.Visibility = Visibility.Collapsed;
            }
            Title = string.Format(ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesTitle, sbdPage.getNameU());
            btnDialogOK.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.OK;
            btnFullSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationFullSep;
            btnNormalSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationStandardSep;
            btnNoSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationNoSep;

            btnFullSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationFullSepTooltip;
            btnNormalSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationStandardSepTooltip;
            btnNoSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationNoSepTooltip;

        }

        private void ButtonNoSep_Click(object sender, RoutedEventArgs e)
        {
            SBDPageController sbdPageC = modelController.getSbdPageController(sbdPage);
            sbdPageC.setSeparationStyle(DiagramPageController.SeparationStyle.NO_SEP);
        }

        private void ButtonFullSep_Click(object sender, RoutedEventArgs e)
        {
            SBDPageController sbdPageC = modelController.getSbdPageController(sbdPage);
            sbdPageC.setSeparationStyle(DiagramPageController.SeparationStyle.FULL_SEP);
        }

        private void ButtonStandardSep_Click(object sender, RoutedEventArgs e)
        {
            SBDPageController sbdPageC = modelController.getSbdPageController(sbdPage);
            sbdPageC.setSeparationStyle(DiagramPageController.SeparationStyle.STANDARD_SEP);
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            callback.applyChanges();
            Close();
        }

    }


}
