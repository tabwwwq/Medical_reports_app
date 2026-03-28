using System;
using System.Net;
using System.Net.Mail;

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
    }
}