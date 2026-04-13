using System;
using System.Collections.Generic;

namespace MedicalReportsApp.Classes
{
    public abstract class Account
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Admin : Account
    {
    }

    public class Doctor : Account
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Specialization { get; set; }

        public int Age
        {
            get
            {
                DateTime now = DateTime.Now;
                int age = now.Year - BirthDate.Year;
                if (now.Month < BirthDate.Month || (now.Month == BirthDate.Month && now.Day < BirthDate.Day))
                {
                    age--;
                }
                return age;
            }
        }

        public string FullName
        {
            get { return (FirstName + " " + LastName).Trim(); }
        }
    }

    public class Patient : Account
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string BloodType { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
        public int? FamilyDoctorId { get; set; }

        public int Age
        {
            get
            {
                DateTime now = DateTime.Now;
                int age = now.Year - BirthDate.Year;
                if (now.Month < BirthDate.Month || (now.Month == BirthDate.Month && now.Day < BirthDate.Day))
                {
                    age--;
                }
                return age;
            }
        }

        public string FullName
        {
            get { return (FirstName + " " + LastName).Trim(); }
        }
    }

    public class PendingPatient
    {
        public int PendingPatientId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string VerificationCode { get; set; }
        public string RoleName { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
    }

    public class PendingPasswordReset
    {
        public int PendingPasswordResetId { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string VerificationCode { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VisitCard
    {
        public string DoctorName { get; set; }
        public DateTime VisitDate { get; set; }
        public string VisitType { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
    }

    public class PrescriptionCard
    {
        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Status { get; set; }
    }

    public class PatientDashboardData
    {
        public Patient Patient { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public List<string> Allergies { get; set; }
        public List<string> ChronicProblems { get; set; }
        public List<VisitCard> Visits { get; set; }
        public List<PrescriptionCard> Prescriptions { get; set; }

        public PatientDashboardData()
        {
            Allergies = new List<string>();
            ChronicProblems = new List<string>();
            Visits = new List<VisitCard>();
            Prescriptions = new List<PrescriptionCard>();
        }
    }

    public class DoctorDashboardPatientCard
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public string Condition { get; set; }

        public string LastVisitText
        {
            get { return LastVisitDate == null ? "No visits yet" : LastVisitDate.Value.ToString("yyyy-MM-dd"); }
        }
    }

    public class DoctorDashboardData
    {
        public Doctor Doctor { get; set; }
        public List<DoctorDashboardPatientCard> Patients { get; set; }

        public DoctorDashboardData()
        {
            Patients = new List<DoctorDashboardPatientCard>();
        }
    }

    public class DoctorPatientDetailsData
    {
        public Patient Patient { get; set; }
        public List<string> Allergies { get; set; }
        public List<string> ChronicProblems { get; set; }
        public List<VisitCard> Visits { get; set; }
        public List<PrescriptionCard> Prescriptions { get; set; }

        public DoctorPatientDetailsData()
        {
            Allergies = new List<string>();
            ChronicProblems = new List<string>();
            Visits = new List<VisitCard>();
            Prescriptions = new List<PrescriptionCard>();
        }
    }
}
