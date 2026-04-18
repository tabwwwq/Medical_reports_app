CREATE TABLE IF NOT EXISTS AuditLogs
(
    AuditLogId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ActorUserType VARCHAR(50) NOT NULL,
    ActorUserId INT NULL,
    ActorName VARCHAR(255) NULL,
    TargetUserType VARCHAR(50) NOT NULL,
    TargetUserId INT NULL,
    TargetName VARCHAR(255) NULL,
    ActionType VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100) NOT NULL,
    FieldName VARCHAR(100) NULL,
    OldValue TEXT NULL,
    NewValue TEXT NULL,
    Description TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_AuditLogs_ActorUserId (ActorUserId),
    INDEX IX_AuditLogs_TargetUserId (TargetUserId),
    INDEX IX_AuditLogs_ActionType (ActionType),
    INDEX IX_AuditLogs_CreatedAt (CreatedAt)
);
