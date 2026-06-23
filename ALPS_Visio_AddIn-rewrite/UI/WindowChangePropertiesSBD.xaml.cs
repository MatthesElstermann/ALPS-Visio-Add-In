
using System.Windows;
using AppStrings = ALPS_Visio_AddIn_rewrite.Resources.strings;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Interaktionslogik für WindowChangePropertiesSBD.xaml
    /// </summary>
    public partial class WindowChangePropertiesSbd : Window
    {

        private ThisAddIn addIn;
        private readonly ModelController modelController;
        private readonly SBDPage sbdPage;
        private readonly IDialogCallback callback;

        public WindowChangePropertiesSbd(IDialogCallback callback, ThisAddIn addIn, ModelController modelController, SBDPage sbdPage)
        {
            InitializeComponent();

            this.callback = callback;
            this.addIn = addIn;
            this.modelController = modelController;
            this.sbdPage = sbdPage;


            if (this.sbdPage.getExtends() != null)
            {
                chooseSep.Content = string.Format(AppStrings.WindowChangePropertiesChooseSepExtended, "\"" + this.sbdPage.getExtends().getNameU() + "\""); ;
            }
            else
            {
                chooseSep.Content = "";
                btnDialogOK.Visibility = Visibility.Collapsed;
                btnFullSeparation.Visibility = Visibility.Collapsed;
                btnNormalSeparation.Visibility = Visibility.Collapsed;
                btnNoSeparation.Visibility = Visibility.Collapsed;
            }
            Title = string.Format(AppStrings.WindowChangePropertiesTitle, sbdPage.getNameU());
            btnDialogOK.Content = AppStrings.OK;
            btnFullSeparation.Content = AppStrings.SeparationFullSep;
            btnNormalSeparation.Content = AppStrings.SeparationStandardSep;
            btnNoSeparation.Content = AppStrings.SeparationNoSep;

            btnFullSeparation.ToolTip = AppStrings.SeparationFullSepTooltip;
            btnNormalSeparation.ToolTip = AppStrings.SeparationStandardSepTooltip;
            btnNoSeparation.ToolTip = AppStrings.SeparationNoSepTooltip;

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
