
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VisioAddIn.Snapping;

namespace VisioAddIn
{

    /// <summary>
    /// Interaktionslogik für WindowChangeProperties.xaml
    /// </summary>
    public partial class WindowChangeProperties : Window
    {

        private static readonly string chooseExt = ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesChoose;
        private const string NONE_CHOSEN = " - ";
        private ALPS_Visio_AddIn_rewrite.ThisAddIn addIn;
        private readonly ModelController modelController;
        private int currentPriority;
        private readonly int priorityOnStart;
        private readonly SIDPage currentlyExtends;
        private readonly IDialogCallback callback;
        private readonly SIDPageController sidPageC;

        /// <summary>
        /// reference to the Page that is modified at the moment.
        /// </summary>
        private readonly SIDPage sidPage;

        private readonly IList<SIDPage> extendableSidPages;

        /// <summary>
        /// A window that allows the user to change the properties of an SID
        /// </summary>
        /// <param name="callback">the calling class</param>
        /// <param name="addIn">the visio addIn (the main class)</param>
        /// <param name="modelController">the model controller</param>
        /// <param name="sidPage">the current SID page</param>
        /// <param name="extendableSidPages">the list of SID pages that can possibly be extended</param>
        public WindowChangeProperties(IDialogCallback callback, ALPS_Visio_AddIn_rewrite.ThisAddIn addIn, ModelController modelController, SIDPage sidPage, IList<SIDPage> extendableSidPages)
        {
            InitializeComponent();


            this.addIn = addIn;
            this.sidPage = sidPage;
            this.callback = callback;

            this.extendableSidPages = extendableSidPages;
            this.modelController = modelController;

            sidPageC = this.modelController.getSidPageController(this.sidPage);

            currentlyExtends = this.sidPage.getExtends();

            initializeComboBox();

            textBoxPriority.KeyUp += keyPressed;

            // Only show separation-options if current SID extends another SID
            if (currentlyExtends != null)
            {
                WrapPanelSeparation.Visibility = Visibility.Visible;
                labelChooseSep.Visibility = Visibility.Visible;
            }
            else
            {
                WrapPanelSeparation.Visibility = Visibility.Collapsed;
                labelChooseSep.Visibility = Visibility.Collapsed;
            }
            priorityOnStart = this.sidPage.getPriorityOrder();
            currentPriority = priorityOnStart;

            textBoxPriority.Text = currentPriority.ToString();
            textBoxPriority.MouseWheel += textBoxMouseWheel;

            Title = string.Format(ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesTitle, sidPage.getNameU());
            btnDialogOK.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.OK;
            btnDialogCancel.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.Cancel;
            btnFullSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationFullSep;
            btnNormalSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationStandardSep;
            btnNoSeparation.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationNoSep;
            labelChooseSep.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesChooseSep;

            btnFullSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationFullSepTooltip;
            btnNormalSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationStandardSepTooltip;
            btnNoSeparation.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationNoSepTooltip;
            labelChooseSep.ToolTip = ALPS_Visio_AddIn_rewrite.Resources.strings.SeparationChooseSepTooltip;

            labelExtends.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesExtends;
            labelPriority.Content = ALPS_Visio_AddIn_rewrite.Resources.strings.WindowChangePropertiesPriority;
        }



        /// <summary>
        /// Increases or decreases the number in the TextBoxPriority if mousewheel is being scrolled inside the (selected) textbox.
        /// </summary>
        /// <param name="sender">The form in which the mouse scrolled</param>
        /// <param name="e">the arguments of the event</param>
        private void textBoxMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                incPriority();
            else
                decPriority();
            textBoxPriority.Select(textBoxPriority.Text.Length, 0);
        }



        private void initializeComboBox()
        {

            IList<string> extendableSidString = extendableSidPages.Select(sidPage => sidPage.getLayer()).ToList();

            // If currently this page extends another SID
            if (currentlyExtends != null)
            {
                // Insert the extended at index 0
                extendableSidString.Insert(0, NONE_CHOSEN);
                comboBoxExtends.SelectedIndex = 0;
                if (extendableSidString.Contains(currentlyExtends.getLayer()))
                {
                    // Remove it at its previous index
                    extendableSidString.Remove(currentlyExtends.getLayer());
                    extendableSidString.Insert(1, currentlyExtends.getLayer());
                    comboBoxExtends.SelectedIndex = 1;
                }
            }
            else
            {
                // Else display the choose-message
                extendableSidString.Insert(0, chooseExt);
                comboBoxExtends.SelectedIndex = 0;
            }
            comboBoxExtends.ItemsSource = extendableSidString.ToArray();

        }

        private void keyPressed(object sender, System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Up:
                    incPriority();
                    break;
                case Key.Down:
                    decPriority();
                    break;
                case Key.Enter:
                    btnDialogOK_Click(sender, args);
                    break;
            }

            args.Handled = true;
        }

        /// <summary>
        /// Increases the current priority-value by one.
        /// Uses the value in the text box as previous value if parseable,
        /// else uses the latest parseable prio known and increases by one.
        /// </summary>
        private void incPriority()
        {
            if (int.TryParse(textBoxPriority.Text, out int parsedResult))
            {
                // Overwrite current priority if text is parseable
                currentPriority = parsedResult;
            }
            currentPriority++;
            textBoxPriority.Text = currentPriority.ToString();
        }

        /// <summary>
        /// Decreases the current priority-value by one.
        /// Uses the value in the text box as previous value if parseable,
        /// else uses the latest parseable prio known and decreases by one.
        /// Does not drop below zero.
        /// </summary>
        private void decPriority()
        {
            if (int.TryParse(textBoxPriority.Text, out int parsedResult))
            {
                // Overwrite current priority if text is parseable
                currentPriority = parsedResult;
            }

            if (currentPriority <= 1) return;
            currentPriority--;
            textBoxPriority.Text = currentPriority.ToString();
        }

        private void btnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            // Closes the dialog
            this.Close();
        }


        private void btnDialogOK_Click(object sender, RoutedEventArgs e)
        {
            // Update the priority if a number is entered
            if (currentPriority != priorityOnStart)
            {
                modelController.updatePagePriority(currentPriority.ToString(), sidPageC);
                addIn.refreshLayerExplorerTreeView();
            }

            if (!comboBoxExtends.SelectedItem.ToString().Equals(NONE_CHOSEN))
            {
                // If a valid SID is picked
                foreach (SIDPage extendableSid in extendableSidPages)
                {
                    if (extendableSid.getLayer().Equals(comboBoxExtends.SelectedItem.ToString()))
                    {
                        addIn.extendsChanged(extendableSid, sidPage);
                    }
                }
            }
            else
            {
                // If none was picked
                addIn.extendsChanged(sidPage, sidPage);

            }
            //TODO: better concept for extending nothing. 
            callback.applyChanges();
            Close();
        }

        private void noSeparation_Click(object sender, RoutedEventArgs e)
        {
            sidPageC.setSeparationStyle(DiagramPageController.SeparationStyle.NO_SEP);
        }

        private void normalSeparation_Click(object sender, RoutedEventArgs e)
        {
            sidPageC.setSeparationStyle(DiagramPageController.SeparationStyle.STANDARD_SEP);
        }

        private void fullSeparation_Click(object sender, RoutedEventArgs e)
        {
            sidPageC.setSeparationStyle(DiagramPageController.SeparationStyle.FULL_SEP);
        }

    }
}
