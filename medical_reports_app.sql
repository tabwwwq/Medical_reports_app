-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1
-- Время создания: Апр 21 2026 г., 10:18
-- Версия сервера: 10.4.32-MariaDB
-- Версия PHP: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `medical_reports_app`
--

-- --------------------------------------------------------

--
-- Структура таблицы `adminactions`
--

CREATE TABLE `adminactions` (
  `AdminActionId` int(11) NOT NULL,
  `AdminId` int(11) NOT NULL,
  `ActionType` varchar(100) NOT NULL,
  `TargetType` varchar(50) NOT NULL,
  `TargetId` int(11) NOT NULL,
  `ActionDate` datetime NOT NULL DEFAULT current_timestamp(),
  `Notes` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `admins`
--

CREATE TABLE `admins` (
  `AdminId` int(11) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `allergytypes`
--

CREATE TABLE `allergytypes` (
  `AllergyTypeId` int(11) NOT NULL,
  `Name` varchar(150) NOT NULL,
  `Description` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `auditlogs`
--

CREATE TABLE `auditlogs` (
  `AuditLogId` int(11) NOT NULL,
  `ActorUserType` varchar(50) NOT NULL,
  `ActorUserId` int(11) DEFAULT NULL,
  `ActorName` varchar(255) DEFAULT NULL,
  `TargetUserType` varchar(50) NOT NULL,
  `TargetUserId` int(11) DEFAULT NULL,
  `TargetName` varchar(255) DEFAULT NULL,
  `ActionType` varchar(100) NOT NULL,
  `EntityType` varchar(100) NOT NULL,
  `FieldName` varchar(100) DEFAULT NULL,
  `OldValue` text DEFAULT NULL,
  `NewValue` text DEFAULT NULL,
  `Description` text DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `chronicdiseasetypes`
--

CREATE TABLE `chronicdiseasetypes` (
  `ChronicDiseaseTypeId` int(11) NOT NULL,
  `Name` varchar(150) NOT NULL,
  `Description` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `doctors`
--

CREATE TABLE `doctors` (
  `DoctorId` int(11) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `Gender` varchar(20) NOT NULL,
  `BirthDate` date NOT NULL,
  `Specialization` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `Avatar` longblob DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `familydoctorrequests`
--

CREATE TABLE `familydoctorrequests` (
  `RequestId` int(11) NOT NULL,
  `PatientId` int(11) NOT NULL,
  `RequestedDoctorId` int(11) NOT NULL,
  `RequestDate` datetime NOT NULL DEFAULT current_timestamp(),
  `Status` varchar(30) NOT NULL DEFAULT 'Pending',
  `AdminComment` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `patientallergies`
--

CREATE TABLE `patientallergies` (
  `PatientAllergyId` int(11) NOT NULL,
  `PatientId` int(11) NOT NULL,
  `AllergyTypeId` int(11) NOT NULL,
  `Notes` varchar(500) DEFAULT NULL,
  `AddedByDoctorId` int(11) DEFAULT NULL,
  `AddedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `patientchangeaudit`
--

CREATE TABLE `patientchangeaudit` (
  `AuditId` int(11) NOT NULL,
  `DoctorId` int(11) DEFAULT NULL,
  `DoctorName` varchar(255) DEFAULT NULL,
  `PatientId` int(11) DEFAULT NULL,
  `PatientName` varchar(255) DEFAULT NULL,
  `FieldChanged` varchar(255) NOT NULL,
  `OldValue` text DEFAULT NULL,
  `NewValue` text DEFAULT NULL,
  `ChangeType` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `ChangeId` int(11) DEFAULT NULL,
  `FieldName` varchar(100) DEFAULT NULL,
  `ChangeDescription` text DEFAULT NULL,
  `IsReviewed` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `patientchronicdiseases`
--

CREATE TABLE `patientchronicdiseases` (
  `PatientChronicDiseaseId` int(11) NOT NULL,
  `PatientId` int(11) NOT NULL,
  `ChronicDiseaseTypeId` int(11) NOT NULL,
  `Notes` varchar(500) DEFAULT NULL,
  `AddedByDoctorId` int(11) DEFAULT NULL,
  `AddedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `patients`
--

CREATE TABLE `patients` (
  `PatientId` int(11) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `Address` varchar(255) DEFAULT NULL,
  `City` varchar(100) DEFAULT NULL,
  `ZipCode` varchar(20) DEFAULT NULL,
  `BloodType` varchar(10) DEFAULT NULL,
  `Gender` varchar(20) NOT NULL,
  `BirthDate` date NOT NULL,
  `Phone` varchar(30) DEFAULT NULL,
  `FamilyDoctorId` int(11) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `Avatar` longblob DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `patienttwofactorauth`
--

CREATE TABLE `patienttwofactorauth` (
  `PatientId` int(11) NOT NULL,
  `SecretKey` varchar(64) NOT NULL,
  `IsEnabled` bit(1) NOT NULL DEFAULT b'0',
  `CreatedAt` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `pendingaccountdeletions`
--

CREATE TABLE `pendingaccountdeletions` (
  `PendingAccountDeletionId` int(11) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `RoleName` varchar(50) NOT NULL,
  `VerificationCode` varchar(6) NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `pendingpasswordresets`
--

CREATE TABLE `pendingpasswordresets` (
  `PendingPasswordResetId` int(11) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `RoleName` varchar(20) NOT NULL,
  `VerificationCode` varchar(20) NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `pendingpatients`
--

CREATE TABLE `pendingpatients` (
  `PendingPatientId` int(11) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `VerificationCode` varchar(20) NOT NULL,
  `RoleName` varchar(30) NOT NULL DEFAULT 'Patient',
  `ExpiresAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `IsVerified` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `pendingprofiledeletions`
--

CREATE TABLE `pendingprofiledeletions` (
  `Email` varchar(255) NOT NULL,
  `VerificationCode` varchar(10) NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `prescriptions`
--

CREATE TABLE `prescriptions` (
  `PrescriptionId` int(11) NOT NULL,
  `PatientId` int(11) NOT NULL,
  `DoctorId` int(11) DEFAULT NULL,
  `Name` varchar(255) NOT NULL,
  `Dosage` varchar(120) NOT NULL,
  `Quantity` varchar(120) NOT NULL,
  `Status` varchar(50) NOT NULL DEFAULT 'Active',
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `UpdatedAt` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `suspiciousactivityactions`
--

CREATE TABLE `suspiciousactivityactions` (
  `SuspiciousActivityActionId` int(11) NOT NULL,
  `DoctorId` int(11) DEFAULT NULL,
  `DoctorName` varchar(255) NOT NULL,
  `WindowStart` datetime NOT NULL,
  `WindowEnd` datetime NOT NULL,
  `ActionStatus` varchar(50) NOT NULL,
  `ActionByAdminEmail` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `systemlogs`
--

CREATE TABLE `systemlogs` (
  `LogId` int(11) NOT NULL,
  `Title` varchar(255) NOT NULL,
  `Description` text DEFAULT NULL,
  `LogLevel` varchar(50) NOT NULL DEFAULT 'INFO',
  `UserEmail` varchar(255) DEFAULT NULL,
  `EntityType` varchar(100) DEFAULT NULL,
  `EntityId` int(11) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `LogType` varchar(100) NOT NULL DEFAULT 'General',
  `SeverityLevel` varchar(20) NOT NULL DEFAULT 'Info',
  `ActorEmail` varchar(255) DEFAULT NULL,
  `ActorRole` varchar(50) DEFAULT NULL,
  `TargetEmail` varchar(255) DEFAULT NULL,
  `TargetName` varchar(255) DEFAULT NULL,
  `RelatedDoctorId` int(11) DEFAULT NULL,
  `RelatedPatientId` int(11) DEFAULT NULL,
  `SearchText` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `visits`
--

CREATE TABLE `visits` (
  `VisitId` int(11) NOT NULL,
  `PatientId` int(11) NOT NULL,
  `DoctorId` int(11) NOT NULL,
  `VisitDate` datetime NOT NULL DEFAULT current_timestamp(),
  `VisitType` varchar(50) NOT NULL,
  `Diagnosis` varchar(500) DEFAULT NULL,
  `Notes` varchar(1000) DEFAULT NULL,
  `CertificateText` longtext DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `adminactions`
--
ALTER TABLE `adminactions`
  ADD PRIMARY KEY (`AdminActionId`),
  ADD KEY `IX_AdminActions_AdminId` (`AdminId`);

--
-- Индексы таблицы `admins`
--
ALTER TABLE `admins`
  ADD PRIMARY KEY (`AdminId`),
  ADD UNIQUE KEY `UQ_Admins_Email` (`Email`);

--
-- Индексы таблицы `allergytypes`
--
ALTER TABLE `allergytypes`
  ADD PRIMARY KEY (`AllergyTypeId`),
  ADD UNIQUE KEY `UQ_AllergyTypes_Name` (`Name`);

--
-- Индексы таблицы `auditlogs`
--
ALTER TABLE `auditlogs`
  ADD PRIMARY KEY (`AuditLogId`),
  ADD KEY `IX_AuditLogs_ActorUserId` (`ActorUserId`),
  ADD KEY `IX_AuditLogs_TargetUserId` (`TargetUserId`),
  ADD KEY `IX_AuditLogs_ActionType` (`ActionType`),
  ADD KEY `IX_AuditLogs_CreatedAt` (`CreatedAt`);

--
-- Индексы таблицы `chronicdiseasetypes`
--
ALTER TABLE `chronicdiseasetypes`
  ADD PRIMARY KEY (`ChronicDiseaseTypeId`),
  ADD UNIQUE KEY `UQ_ChronicDiseaseTypes_Name` (`Name`);

--
-- Индексы таблицы `doctors`
--
ALTER TABLE `doctors`
  ADD PRIMARY KEY (`DoctorId`),
  ADD UNIQUE KEY `UQ_Doctors_Email` (`Email`);

--
-- Индексы таблицы `familydoctorrequests`
--
ALTER TABLE `familydoctorrequests`
  ADD PRIMARY KEY (`RequestId`),
  ADD KEY `IX_FamilyDoctorRequests_PatientId` (`PatientId`),
  ADD KEY `IX_FamilyDoctorRequests_RequestedDoctorId` (`RequestedDoctorId`);

--
-- Индексы таблицы `patientallergies`
--
ALTER TABLE `patientallergies`
  ADD PRIMARY KEY (`PatientAllergyId`),
  ADD UNIQUE KEY `UQ_PatientAllergies_Patient_Allergy` (`PatientId`,`AllergyTypeId`),
  ADD KEY `IX_PatientAllergies_PatientId` (`PatientId`),
  ADD KEY `IX_PatientAllergies_AllergyTypeId` (`AllergyTypeId`),
  ADD KEY `IX_PatientAllergies_AddedByDoctorId` (`AddedByDoctorId`);

--
-- Индексы таблицы `patientchangeaudit`
--
ALTER TABLE `patientchangeaudit`
  ADD PRIMARY KEY (`AuditId`);

--
-- Индексы таблицы `patientchronicdiseases`
--
ALTER TABLE `patientchronicdiseases`
  ADD PRIMARY KEY (`PatientChronicDiseaseId`),
  ADD UNIQUE KEY `UQ_PatientChronicDiseases_Patient_Disease` (`PatientId`,`ChronicDiseaseTypeId`),
  ADD KEY `IX_PatientChronicDiseases_PatientId` (`PatientId`),
  ADD KEY `IX_PatientChronicDiseases_ChronicDiseaseTypeId` (`ChronicDiseaseTypeId`),
  ADD KEY `IX_PatientChronicDiseases_AddedByDoctorId` (`AddedByDoctorId`);

--
-- Индексы таблицы `patients`
--
ALTER TABLE `patients`
  ADD PRIMARY KEY (`PatientId`),
  ADD UNIQUE KEY `UQ_Patients_Email` (`Email`),
  ADD KEY `IX_Patients_FamilyDoctorId` (`FamilyDoctorId`);

--
-- Индексы таблицы `patienttwofactorauth`
--
ALTER TABLE `patienttwofactorauth`
  ADD PRIMARY KEY (`PatientId`);

--
-- Индексы таблицы `pendingaccountdeletions`
--
ALTER TABLE `pendingaccountdeletions`
  ADD PRIMARY KEY (`PendingAccountDeletionId`),
  ADD UNIQUE KEY `uq_pending_account_deletion` (`Email`,`RoleName`);

--
-- Индексы таблицы `pendingpasswordresets`
--
ALTER TABLE `pendingpasswordresets`
  ADD PRIMARY KEY (`PendingPasswordResetId`),
  ADD UNIQUE KEY `UQ_PendingPasswordResets_Email_Role` (`Email`,`RoleName`);

--
-- Индексы таблицы `pendingpatients`
--
ALTER TABLE `pendingpatients`
  ADD PRIMARY KEY (`PendingPatientId`),
  ADD UNIQUE KEY `UQ_PendingPatients_Email` (`Email`);

--
-- Индексы таблицы `pendingprofiledeletions`
--
ALTER TABLE `pendingprofiledeletions`
  ADD PRIMARY KEY (`Email`);

--
-- Индексы таблицы `prescriptions`
--
ALTER TABLE `prescriptions`
  ADD PRIMARY KEY (`PrescriptionId`),
  ADD KEY `IX_Prescriptions_PatientId` (`PatientId`),
  ADD KEY `IX_Prescriptions_DoctorId` (`DoctorId`);

--
-- Индексы таблицы `suspiciousactivityactions`
--
ALTER TABLE `suspiciousactivityactions`
  ADD PRIMARY KEY (`SuspiciousActivityActionId`),
  ADD KEY `IX_SuspiciousActivityActions_DoctorWindow` (`DoctorName`,`WindowStart`,`WindowEnd`);

--
-- Индексы таблицы `systemlogs`
--
ALTER TABLE `systemlogs`
  ADD PRIMARY KEY (`LogId`);

--
-- Индексы таблицы `visits`
--
ALTER TABLE `visits`
  ADD PRIMARY KEY (`VisitId`),
  ADD KEY `IX_Visits_PatientId` (`PatientId`),
  ADD KEY `IX_Visits_DoctorId` (`DoctorId`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `adminactions`
--
ALTER TABLE `adminactions`
  MODIFY `AdminActionId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `admins`
--
ALTER TABLE `admins`
  MODIFY `AdminId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `allergytypes`
--
ALTER TABLE `allergytypes`
  MODIFY `AllergyTypeId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `auditlogs`
--
ALTER TABLE `auditlogs`
  MODIFY `AuditLogId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `chronicdiseasetypes`
--
ALTER TABLE `chronicdiseasetypes`
  MODIFY `ChronicDiseaseTypeId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `doctors`
--
ALTER TABLE `doctors`
  MODIFY `DoctorId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `familydoctorrequests`
--
ALTER TABLE `familydoctorrequests`
  MODIFY `RequestId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `patientallergies`
--
ALTER TABLE `patientallergies`
  MODIFY `PatientAllergyId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `patientchangeaudit`
--
ALTER TABLE `patientchangeaudit`
  MODIFY `AuditId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `patientchronicdiseases`
--
ALTER TABLE `patientchronicdiseases`
  MODIFY `PatientChronicDiseaseId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `patients`
--
ALTER TABLE `patients`
  MODIFY `PatientId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `pendingaccountdeletions`
--
ALTER TABLE `pendingaccountdeletions`
  MODIFY `PendingAccountDeletionId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `pendingpasswordresets`
--
ALTER TABLE `pendingpasswordresets`
  MODIFY `PendingPasswordResetId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `pendingpatients`
--
ALTER TABLE `pendingpatients`
  MODIFY `PendingPatientId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `prescriptions`
--
ALTER TABLE `prescriptions`
  MODIFY `PrescriptionId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `suspiciousactivityactions`
--
ALTER TABLE `suspiciousactivityactions`
  MODIFY `SuspiciousActivityActionId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `systemlogs`
--
ALTER TABLE `systemlogs`
  MODIFY `LogId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `visits`
--
ALTER TABLE `visits`
  MODIFY `VisitId` int(11) NOT NULL AUTO_INCREMENT;

--
-- Ограничения внешнего ключа сохраненных таблиц
--

--
-- Ограничения внешнего ключа таблицы `adminactions`
--
ALTER TABLE `adminactions`
  ADD CONSTRAINT `FK_AdminActions_Admins` FOREIGN KEY (`AdminId`) REFERENCES `admins` (`AdminId`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Ограничения внешнего ключа таблицы `familydoctorrequests`
--
ALTER TABLE `familydoctorrequests`
  ADD CONSTRAINT `FK_FamilyDoctorRequests_Doctors` FOREIGN KEY (`RequestedDoctorId`) REFERENCES `doctors` (`DoctorId`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_FamilyDoctorRequests_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Ограничения внешнего ключа таблицы `patientallergies`
--
ALTER TABLE `patientallergies`
  ADD CONSTRAINT `FK_PatientAllergies_AllergyTypes` FOREIGN KEY (`AllergyTypeId`) REFERENCES `allergytypes` (`AllergyTypeId`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_PatientAllergies_Doctors` FOREIGN KEY (`AddedByDoctorId`) REFERENCES `doctors` (`DoctorId`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_PatientAllergies_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Ограничения внешнего ключа таблицы `patientchronicdiseases`
--
ALTER TABLE `patientchronicdiseases`
  ADD CONSTRAINT `FK_PatientDiseases_DiseaseTypes` FOREIGN KEY (`ChronicDiseaseTypeId`) REFERENCES `chronicdiseasetypes` (`ChronicDiseaseTypeId`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_PatientDiseases_Doctors` FOREIGN KEY (`AddedByDoctorId`) REFERENCES `doctors` (`DoctorId`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_PatientDiseases_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Ограничения внешнего ключа таблицы `patients`
--
ALTER TABLE `patients`
  ADD CONSTRAINT `FK_Patients_Doctors` FOREIGN KEY (`FamilyDoctorId`) REFERENCES `doctors` (`DoctorId`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Ограничения внешнего ключа таблицы `patienttwofactorauth`
--
ALTER TABLE `patienttwofactorauth`
  ADD CONSTRAINT `FK_PatientTwoFactorAuth_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE;

--
-- Ограничения внешнего ключа таблицы `prescriptions`
--
ALTER TABLE `prescriptions`
  ADD CONSTRAINT `FK_Prescriptions_Doctors` FOREIGN KEY (`DoctorId`) REFERENCES `doctors` (`DoctorId`) ON DELETE SET NULL,
  ADD CONSTRAINT `FK_Prescriptions_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE;

--
-- Ограничения внешнего ключа таблицы `visits`
--
ALTER TABLE `visits`
  ADD CONSTRAINT `FK_Visits_Doctors` FOREIGN KEY (`DoctorId`) REFERENCES `doctors` (`DoctorId`) ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_Visits_Patients` FOREIGN KEY (`PatientId`) REFERENCES `patients` (`PatientId`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
