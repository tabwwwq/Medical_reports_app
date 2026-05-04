using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using MedicalReportsApp.Classes;

namespace MedicalReportsApp.Services
{
    public class EmailService
    {
        private string host = "smtp.gmail.com";
        private int port = 587;
        private string smtpEmail = "medicalreportsappoff@gmail.com";
        private string smtpPassword = "bvhp ougz yiiu xzmv";
        private bool enableSsl = true;
        private string fromName = "Medical Reports App";

        public void SendVerificationCode(string toEmail, string code)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(smtpEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = "Verification code for Medical Reports App";
            message.Body = "Your verification code is: " + code + "\n\nThis code is valid for 10 minutes.";

            SmtpClient client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            client.EnableSsl = enableSsl;
            client.Send(message);
        }

        public void SendPasswordResetCode(string toEmail, string code)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(smtpEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = "Password reset code for Medical Reports App";
            message.Body = "Your password reset code is: " + code + "\n\nThis code is valid for 10 minutes.";

            SmtpClient client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            client.EnableSsl = enableSsl;
            client.Send(message);
        }

        public void SendDeleteProfileCode(string toEmail, string code)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(smtpEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = "Delete profile code for Medical Reports App";
            message.Body = "Your profile deletion code is: " + code + "\n\nEnter this code in the app to permanently delete your profile.";

            SmtpClient client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            client.EnableSsl = enableSsl;
            client.Send(message);
        }

        public void SendEmailChangeCode(string toEmail, string code)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(smtpEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = "Email change code for Medical Reports App";
            message.Body = "Your email change verification code is: " + code + "\n\nEnter this code in the app to confirm your new email address.";

            SmtpClient client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            client.EnableSsl = enableSsl;
            client.Send(message);
        }

        public void SendPatientRecordUpdateEmail(string toEmail, PatientRecordChangeSummary summary)
        {
            if (summary == null || string.IsNullOrWhiteSpace(toEmail) || !summary.HasAnyChanges)
            {
                return;
            }

            MailMessage message = new MailMessage();
            message.From = new MailAddress(smtpEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = "Your medical record was updated";
            message.IsBodyHtml = true;
            message.Body = BuildPatientUpdateHtml(summary);

            SmtpClient client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            client.EnableSsl = enableSsl;
            client.Send(message);
        }

        private string BuildPatientUpdateHtml(PatientRecordChangeSummary summary)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<div style='font-family:Segoe UI,Arial,sans-serif;background:#f3f4f6;padding:24px;'>");
            html.Append("<div style='max-width:700px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden;'>");
            html.Append("<div style='background:#2563eb;padding:24px 28px;color:#ffffff;'>");
            html.Append("<div style='font-size:28px;font-weight:700;'>Medical Reports App</div>");
            html.Append("<div style='margin-top:8px;font-size:15px;opacity:0.95;'>Your patient record has been updated.</div>");
            html.Append("</div>");
            html.Append("<div style='padding:28px;'>");
            html.Append("<p style='font-size:15px;color:#111827;margin:0 0 18px 0;'>Hello <strong>");
            html.Append(Encode(summary.PatientFullName));
            html.Append("</strong>,</p>");
            html.Append("<p style='font-size:15px;color:#374151;line-height:1.7;margin:0 0 18px 0;'>Doctor <strong>");
            html.Append(Encode(summary.DoctorName));
            html.Append("</strong>");
            if (!string.IsNullOrWhiteSpace(summary.DoctorSpecialization))
            {
                html.Append(" (<span>");
                html.Append(Encode(summary.DoctorSpecialization));
                html.Append("</span>)");
            }
            html.Append(" added the following changes to your account:</p>");

            AppendSection(html, "Updated patient information", BuildBasicInfoLines(summary));
            AppendSection(html, "Added allergies", summary.AddedAllergies);
            AppendSection(html, "Removed allergies", summary.RemovedAllergies);
            AppendSection(html, "Added chronic problems", summary.AddedChronicProblems);
            AppendSection(html, "Removed chronic problems", summary.RemovedChronicProblems);
            AppendSection(html, "Added prescriptions", summary.AddedPrescriptions);
            AppendSection(html, "Removed prescriptions", summary.RemovedPrescriptions);
            AppendSection(html, "New consultation information", BuildVisitLines(summary));

            html.Append("<div style='margin-top:24px;padding:16px 18px;background:#eff6ff;border:1px solid #bfdbfe;border-radius:14px;color:#1e3a8a;font-size:14px;line-height:1.6;'>");
            html.Append("This message was sent after the doctor pressed Save, so all updates are grouped in one email.");
            html.Append("</div>");
            html.Append("</div></div></div>");
            return html.ToString();
        }

        private List<string> BuildBasicInfoLines(PatientRecordChangeSummary summary)
        {
            List<string> lines = new List<string>();
            if (summary.BasicInfoChanged)
            {
                lines.Add("Name: " + (summary.UpdatedFirstName + " " + summary.UpdatedLastName).Trim());
            }
            return lines;
        }

        private List<string> BuildVisitLines(PatientRecordChangeSummary summary)
        {
            List<string> lines = new List<string>();
            if (!summary.VisitAdded)
            {
                return lines;
            }

            if (summary.VisitDate != null)
            {
                lines.Add("Date: " + summary.VisitDate.Value.ToString("yyyy-MM-dd"));
            }
            if (!string.IsNullOrWhiteSpace(summary.VisitReason))
            {
                lines.Add("Reason: " + summary.VisitReason);
            }
            if (!string.IsNullOrWhiteSpace(summary.VisitDiagnosis))
            {
                lines.Add("Diagnosis: " + summary.VisitDiagnosis);
            }
            if (!string.IsNullOrWhiteSpace(summary.VisitNotes))
            {
                lines.Add("Notes: " + summary.VisitNotes);
            }
            return lines;
        }

        private void AppendSection(StringBuilder html, string title, List<string> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            html.Append("<div style='margin-top:18px;border:1px solid #e5e7eb;border-radius:14px;padding:18px;background:#fafafa;'>");
            html.Append("<div style='font-size:16px;font-weight:700;color:#111827;margin-bottom:10px;'>");
            html.Append(Encode(title));
            html.Append("</div><ul style='padding-left:20px;margin:0;color:#374151;line-height:1.8;'>");

            foreach (string item in items.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                html.Append("<li>");
                html.Append(Encode(item));
                html.Append("</li>");
            }

            html.Append("</ul></div>");
        }

        private string Encode(string value)
        {
            return WebUtility.HtmlEncode(value ?? "");
        }

    }
}
