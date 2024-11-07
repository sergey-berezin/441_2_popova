using System.Windows;

namespace Lab3
{
    public partial class InputDialog : Window
    {
        public string? InputText {get; private set;}
        public InputDialog()
        {
            InitializeComponent();
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}