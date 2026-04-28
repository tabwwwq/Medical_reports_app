using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MedicalReportsApp.Classes;

namespace MedicalReportsApp.Services
{
    public class VisitPdfService
    {
        public void SaveVisitAsPdf(VisitCard visit, string filePath)
        {
            if (visit == null)
            {
                throw new ArgumentNullException(nameof(visit));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            List<string> drawing = new List<string>();

            double pageWidth = 595;
            double pageHeight = 842;
            double margin = 48;
            double contentWidth = pageWidth - (margin * 2);
            double currentTopY = 780;

            string visitTitle = BuildVisitTitle(visit);
            AddTitle(drawing, visitTitle, margin, currentTopY);
            currentTopY -= 38;
            AddSmallText(drawing, "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), margin, currentTopY, 10);
            currentTopY -= 36;

            double smallGap = 14;
            double halfWidth = (contentWidth - smallGap) / 2;

            DrawSectionBox(drawing, margin, currentTopY - 74, halfWidth, 74, "Doctor", visit.DoctorDisplayText);
            DrawSectionBox(drawing, margin + halfWidth + smallGap, currentTopY - 74, halfWidth, 74, "Date", visit.VisitDate.ToString("yyyy-MM-dd"));
            currentTopY -= 92;

            currentTopY = DrawWrappedSectionBox(drawing, margin, currentTopY, contentWidth, "Reason for Visit", Safe(visit.VisitType, "No reason added"), 1);
            currentTopY -= 18;
            currentTopY = DrawWrappedSectionBox(drawing, margin, currentTopY, contentWidth, "Diagnosis", Safe(visit.Diagnosis, "No diagnosis added"), 2);
            currentTopY -= 18;
            currentTopY = DrawWrappedSectionBox(drawing, margin, currentTopY, contentWidth, "Notes", Safe(visit.Notes, "No notes added"), 5);
            currentTopY -= 30;

            AddSmallText(drawing, "MedicalReportsApp", margin, currentTopY, 10);
            AddSmallText(drawing, "This PDF was exported from Recent Visits.", pageWidth - margin - 210, currentTopY, 10);

            string contentStream = string.Join("\n", drawing);
            WriteSimplePdf(filePath, pageWidth, pageHeight, contentStream);
        }

        private string BuildVisitTitle(VisitCard visit)
        {
            string specialization = Safe(visit.DoctorSpecialization, "doctor");
            string doctorName = Safe(visit.DoctorName, "Doctor not added");
            return "Visit Report - " + specialization + " - " + doctorName;
        }

        private void AddTitle(List<string> drawing, string text, double x, double y)
        {
            drawing.Add("BT /F1 24 Tf " + F(x) + " " + F(y) + " Td " + PdfText(text) + " Tj ET");
        }

        private void AddSmallText(List<string> drawing, string text, double x, double y, double fontSize)
        {
            drawing.Add("0.42 0.46 0.53 rg BT /F1 " + F(fontSize) + " Tf " + F(x) + " " + F(y) + " Td " + PdfText(text) + " Tj ET");
            drawing.Add("0 0 0 rg");
        }

        private void DrawSectionBox(List<string> drawing, double x, double bottomY, double width, double height, string label, string value)
        {
            DrawRoundedLikeBox(drawing, x, bottomY, width, height);
            AddSmallText(drawing, label, x + 16, bottomY + height - 24, 11);
            drawing.Add("BT /F1 16 Tf " + F(x + 16) + " " + F(bottomY + height - 48) + " Td " + PdfText(value) + " Tj ET");
        }

        private double DrawWrappedSectionBox(List<string> drawing, double x, double topY, double width, string label, string value, int minLines)
        {
            List<string> lines = WrapText(value, width - 32, 16);
            if (lines.Count < minLines)
            {
                while (lines.Count < minLines)
                {
                    lines.Add(string.Empty);
                }
            }

            double lineHeight = 22;
            double height = 24 + 18 + (lines.Count * lineHeight) + 16;
            double bottomY = topY - height;

            DrawRoundedLikeBox(drawing, x, bottomY, width, height);
            AddSmallText(drawing, label, x + 16, topY - 24, 11);

            double textY = topY - 50;
            for (int i = 0; i < lines.Count; i++)
            {
                drawing.Add("BT /F1 16 Tf " + F(x + 16) + " " + F(textY - (i * lineHeight)) + " Td " + PdfText(lines[i]) + " Tj ET");
            }

            return bottomY;
        }

        private void DrawRoundedLikeBox(List<string> drawing, double x, double y, double width, double height)
        {
            drawing.Add("0.96 0.98 0.99 rg " + F(x) + " " + F(y) + " " + F(width) + " " + F(height) + " re f");
            drawing.Add("0.82 0.85 0.89 RG 1 w " + F(x) + " " + F(y) + " " + F(width) + " " + F(height) + " re S");
            drawing.Add("0 0 0 rg");
            drawing.Add("0 0 0 RG");
        }

        private List<string> WrapText(string text, double maxWidth, double fontSize)
        {
            List<string> lines = new List<string>();
            string normalized = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            string[] words = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                return lines;
            }

            StringBuilder current = new StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                string word = SimplifyForPdf(words[i]);
                string possible = current.Length == 0 ? word : current + " " + word;
                if (EstimateWidth(possible, fontSize) <= maxWidth)
                {
                    current.Clear();
                    current.Append(possible);
                }
                else
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    if (EstimateWidth(word, fontSize) <= maxWidth)
                    {
                        current.Append(word);
                    }
                    else
                    {
                        List<string> pieces = BreakLongWord(word, maxWidth, fontSize);
                        for (int p = 0; p < pieces.Count - 1; p++)
                        {
                            lines.Add(pieces[p]);
                        }
                        current.Append(pieces[pieces.Count - 1]);
                    }
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }

            return lines;
        }

        private List<string> BreakLongWord(string word, double maxWidth, double fontSize)
        {
            List<string> pieces = new List<string>();
            StringBuilder current = new StringBuilder();

            foreach (char ch in word)
            {
                string possible = current.ToString() + ch;
                if (EstimateWidth(possible, fontSize) <= maxWidth || current.Length == 0)
                {
                    current.Append(ch);
                }
                else
                {
                    pieces.Add(current.ToString());
                    current.Clear();
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
            {
                pieces.Add(current.ToString());
            }

            return pieces;
        }

        private double EstimateWidth(string text, double fontSize)
        {
            return SimplifyForPdf(text).Length * fontSize * 0.52;
        }

        private string Safe(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private string SimplifyForPdf(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (char ch in text)
            {
                if (ch >= 32 && ch <= 126)
                {
                    builder.Append(ch);
                }
                else if (ch == '–' || ch == '—')
                {
                    builder.Append('-');
                }
                else if (ch == '•')
                {
                    builder.Append('-');
                }
                else
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString().Trim();
        }

        private string PdfText(string text)
        {
            string safe = SimplifyForPdf(text).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            return "(" + safe + ")";
        }

        private string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void WriteSimplePdf(string filePath, double width, double height, string contentStream)
        {
            byte[] contentBytes = Encoding.ASCII.GetBytes(contentStream);

            List<string> objects = new List<string>();
            objects.Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
            objects.Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
            objects.Add("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + F(width) + " " + F(height) + "] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
            objects.Add("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
            objects.Add("5 0 obj\n<< /Length " + contentBytes.Length + " >>\nstream\n" + contentStream + "\nendstream\nendobj\n");

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                WriteAscii(stream, "%PDF-1.4\n");
                List<long> offsets = new List<long>();
                offsets.Add(0);

                for (int i = 0; i < objects.Count; i++)
                {
                    offsets.Add(stream.Position);
                    WriteAscii(stream, objects[i]);
                }

                long xrefPosition = stream.Position;
                WriteAscii(stream, "xref\n0 " + (objects.Count + 1) + "\n");
                WriteAscii(stream, "0000000000 65535 f \n");

                for (int i = 1; i < offsets.Count; i++)
                {
                    WriteAscii(stream, offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
                }

                WriteAscii(stream, "trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xrefPosition + "\n%%EOF");
            }
        }

        private void WriteAscii(FileStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
