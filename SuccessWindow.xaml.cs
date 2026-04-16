using System.Windows;

namespace MedicalReportsApp
{
    public partial class SuccessWindow : Window
    {
        public SuccessWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
