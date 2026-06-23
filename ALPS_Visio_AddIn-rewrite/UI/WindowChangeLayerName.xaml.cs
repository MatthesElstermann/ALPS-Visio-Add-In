
using System;
using System.Windows;
using System.Windows.Input;
using AppStrings = ALPS_Visio_AddIn_rewrite.Resources.strings;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// Interaktionslogik für WindowChangeLayerName.xaml
    /// </summary>
    public partial class WindowChangeLayerName : Window
    {
        private ModelController ModelController;
        private SIDPage changed;
        private IDialogCallback callback;
        private string oldName;

        public WindowChangeLayerName(IDialogCallback callback, ModelController modelController, SIDPage changed)
        {
            InitializeComponent();

            this.callback = callback;
            ModelController = modelController;
            this.changed = changed;

            oldName = changed.getNameU();

            textBoxChooseName.Text = oldName;
            btnDialogCancel.Content = AppStrings.Cancel;
            btnDialogOK.Content = AppStrings.OK;

            labelRename.Content = AppStrings.ChangeLayerNameRename;
            Title = AppStrings.ChangeLayerNameTitle;

            textBoxChooseName.KeyUp += new KeyEventHandler(TextBox_KeyUp);

        }

        private void btnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            // Closes the dialog
            this.Close();
        }


        private void btnDialogOK_Click(object sender, RoutedEventArgs e)
        {
            String newName = textBoxChooseName.Text;
            if (!string.IsNullOrWhiteSpace(newName))
                if (!oldName.Equals(newName))
                {
                    ModelController.changeLayerName(changed, newName);
                    callback.applyChanges();
                }
            this.Close();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnDialogOK_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
