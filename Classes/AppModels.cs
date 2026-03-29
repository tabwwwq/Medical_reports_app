using System;

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
    }

    public class Patient : Account
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
        public int? FamilyDoctorId { get; set; }
    }

    public class PendingPatient
    {
        public int PendingPatientId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string VerificationCode { get; set; }
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
}
