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
        public string AvatarUrl { get; set; }

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
        public string AvatarUrl { get; set; }

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
        public string DoctorSpecialization { get; set; }
        public DateTime VisitDate { get; set; }
        public string VisitType { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }

        public string DoctorDisplayText
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(DoctorName) ? "Doctor not added" : DoctorName.Trim();
                string specialization = string.IsNullOrWhiteSpace(DoctorSpecialization) ? "" : DoctorSpecialization.Trim();

                if (string.IsNullOrWhiteSpace(specialization))
                {
                    return name;
                }

                return name + " (" + specialization + ")";
            }
        }
    }

    public class PrescriptionCard
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string MedicineName { get; set; }
        public string Name { get; set; }
        public string Dosage { get; set; }
        public string Quantity { get; set; }
        public string Status { get; set; }
        public string DoctorName { get; set; }
        public DateTime CreatedAt { get; set; }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(Name) ? MedicineName : Name; }
        }

        public string DisplayLine
        {
            get { return Dosage + " • " + Quantity; }
        }

        public string QrText
        {
            get
            {
                return "Prescription" + Environment.NewLine
                    + "Name: " + DisplayName + Environment.NewLine
                    + "Dosage: " + Dosage + Environment.NewLine
                    + "Quantity: " + Quantity;
            }
        }
    }

    public class PrescriptionEditorItem
    {
        public int PrescriptionId { get; set; }
        public string Name { get; set; }
        public string Dosage { get; set; }
        public string Quantity { get; set; }

        public string DisplayLine
        {
            get { return Dosage + " • " + Quantity; }
        }

        public string AuditText
        {
            get { return (Name ?? "").Trim() + " - " + (Dosage ?? "").Trim() + " - " + (Quantity ?? "").Trim(); }
        }
    }


    public class DoctorSearchCard
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; }
        public string Specialization { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public int FamilyPatientCount { get; set; }
        public string RequestStatus { get; set; }

        public string CountText
        {
            get { return FamilyPatientCount + "/10 family patients"; }
        }
    }

    public class FamilyDoctorRequestCard
    {
        public int RequestId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }
        public DateTime RequestDate { get; set; }
        public string AvatarUrl { get; set; }

        public string RequestDateText
        {
            get { return RequestDate.ToString("yyyy-MM-dd"); }
        }
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
        public string AvatarUrl { get; set; }

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

    public class PatientRecordChangeSummary
    {
        public string PatientEmail { get; set; }
        public string PatientFullName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public List<string> AddedAllergies { get; set; }
        public List<string> RemovedAllergies { get; set; }
        public List<string> AddedChronicProblems { get; set; }
        public List<string> RemovedChronicProblems { get; set; }
        public List<string> AddedPrescriptions { get; set; }
        public List<string> RemovedPrescriptions { get; set; }
        public bool VisitAdded { get; set; }
        public DateTime? VisitDate { get; set; }
        public string VisitReason { get; set; }
        public string VisitDiagnosis { get; set; }
        public string VisitNotes { get; set; }
        public bool BasicInfoChanged { get; set; }
        public string UpdatedFirstName { get; set; }
        public string UpdatedLastName { get; set; }

        public PatientRecordChangeSummary()
        {
            AddedAllergies = new List<string>();
            RemovedAllergies = new List<string>();
            AddedChronicProblems = new List<string>();
            RemovedChronicProblems = new List<string>();
            AddedPrescriptions = new List<string>();
            RemovedPrescriptions = new List<string>();
        }

        public bool HasAnyChanges
        {
            get
            {
                return BasicInfoChanged
                    || AddedAllergies.Count > 0
                    || RemovedAllergies.Count > 0
                    || AddedChronicProblems.Count > 0
                    || RemovedChronicProblems.Count > 0
                    || AddedPrescriptions.Count > 0
                    || RemovedPrescriptions.Count > 0
                    || VisitAdded;
            }
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

    public class AdminAuditLogCard
    {
        public int AuditLogId { get; set; }
        public int? ActorUserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ActorName { get; set; }
        public string TargetName { get; set; }
        public string ActorUserType { get; set; }
        public string TargetUserType { get; set; }
        public string ActionType { get; set; }
        public string EntityType { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LogTypeText { get; set; }

        public string MetaText
        {
            get
            {
                string actorLabel = string.IsNullOrWhiteSpace(ActorUserType) ? "Actor" : ActorUserType;
                string targetLabel = string.IsNullOrWhiteSpace(TargetUserType) ? "Target" : TargetUserType;
                return actorLabel + ": " + (string.IsNullOrWhiteSpace(ActorName) ? "-" : ActorName) + " • " + targetLabel + ": " + (string.IsNullOrWhiteSpace(TargetName) ? "-" : TargetName) + " • Time: " + CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }

    public class SuspiciousActivityAlert
    {
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int ChangeCount { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string Priority { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<AdminAuditLogCard> Changes { get; set; }

        public SuspiciousActivityAlert()
        {
            Changes = new List<AdminAuditLogCard>();
        }
    }

    public class AdminDoctorCard
    {
        public int DoctorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Specialization { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string AvatarUrl { get; set; }

        public string FullName
        {
            get { return (FirstName + " " + LastName).Trim(); }
        }

        public string Initials
        {
            get
            {
                string first = string.IsNullOrWhiteSpace(FirstName) ? "" : FirstName.Trim().Substring(0, 1).ToUpper();
                string last = string.IsNullOrWhiteSpace(LastName) ? "" : LastName.Trim().Substring(0, 1).ToUpper();
                string value = (first + last).Trim();
                return value == "" ? "DR" : value;
            }
        }

        public string StatusText
        {
            get { return IsActive ? "Active" : "Frozen"; }
        }
    }

    public class AdminDashboardData
    {
        public Admin Admin { get; set; }
        public List<AdminAuditLogCard> Logs { get; set; }
        public List<SuspiciousActivityAlert> SuspiciousActivities { get; set; }
        public List<AdminDoctorCard> Doctors { get; set; }

        public AdminDashboardData()
        {
            Logs = new List<AdminAuditLogCard>();
            SuspiciousActivities = new List<SuspiciousActivityAlert>();
            Doctors = new List<AdminDoctorCard>();
        }
    }
}
