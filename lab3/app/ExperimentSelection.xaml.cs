using System.Windows;

namespace Lab3
{
    public partial class ExperimentSelection : Window
    {
        public string? SelectedExperiment {get; private set;}
        public ExperimentSelection(string[] experimentNames)
        {
            InitializeComponent();
            ExperimentListBox.ItemsSource = experimentNames;
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExperimentListBox.SelectedItem != null)
            {
                SelectedExperiment = ExperimentListBox.SelectedItem.ToString();
                DialogResult = true;
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}