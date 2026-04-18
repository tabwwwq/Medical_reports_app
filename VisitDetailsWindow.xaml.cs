using System.Windows;
using MedicalReportsApp.Classes;

namespace MedicalReportsApp
{
    public partial class VisitDetailsWindow : Window
    {
        public VisitDetailsWindow(VisitCard visit)
        {
            InitializeComponent();
            txtDoctor.Text = string.IsNullOrWhiteSpace(visit.DoctorName) ? "Doctor not added" : visit.DoctorName;
            txtDate.Text = visit.VisitDate.ToString("yyyy-MM-dd");
            txtReason.Text = string.IsNullOrWhiteSpace(visit.VisitType) ? "No reason added" : visit.VisitType;
            txtDiagnosis.Text = string.IsNullOrWhiteSpace(visit.Diagnosis) ? "No diagnosis added" : visit.Diagnosis;
            txtNotes.Text = string.IsNullOrWhiteSpace(visit.Notes) ? "No notes added" : visit.Notes;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
