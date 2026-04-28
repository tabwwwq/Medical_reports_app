using System;
using System.Windows;
using Microsoft.Win32;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class VisitDetailsWindow : Window
    {
        private readonly VisitCard currentVisit;
        private readonly VisitPdfService visitPdfService = new VisitPdfService();

        public VisitDetailsWindow(VisitCard visit)
        {
            InitializeComponent();
            currentVisit = visit;
            txtDoctor.Text = visit.DoctorDisplayText;
            txtDate.Text = visit.VisitDate.ToString("yyyy-MM-dd");
            txtReason.Text = string.IsNullOrWhiteSpace(visit.VisitType) ? "No reason added" : visit.VisitType;
            txtDiagnosis.Text = string.IsNullOrWhiteSpace(visit.Diagnosis) ? "No diagnosis added" : visit.Diagnosis;
            txtNotes.Text = string.IsNullOrWhiteSpace(visit.Notes) ? "No notes added" : visit.Notes;
        }


        private void btnDownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Title = "Save visit as PDF";
                dialog.Filter = "PDF files (*.pdf)|*.pdf";
                dialog.FileName = "recent-visit-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".pdf";

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    return;
                }

                visitPdfService.SaveVisitAsPdf(currentVisit, dialog.FileName);
                MessageBox.Show("PDF was downloaded successfully.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create PDF.\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
