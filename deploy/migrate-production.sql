IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    IF SCHEMA_ID(N'Audit') IS NULL EXEC(N'CREATE SCHEMA [Audit];');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    IF SCHEMA_ID(N'History') IS NULL EXEC(N'CREATE SCHEMA [History];');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    IF SCHEMA_ID(N'Releases') IS NULL EXEC(N'CREATE SCHEMA [Releases];');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    IF SCHEMA_ID(N'Authorization') IS NULL EXEC(N'CREATE SCHEMA [Authorization];');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Audit].[DomainEvents] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [OccurredOn] datetime2(3) NOT NULL,
        [RecordedOn] datetime2(3) NOT NULL,
        [ActionedBy] nvarchar(250) NULL,
        CONSTRAINT [PK_DomainEvents] PRIMARY KEY NONCLUSTERED ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [History].[HistoryRecords] (
        [Id] uniqueidentifier NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [HappenedOn] datetime2(3) NOT NULL,
        [HappenedBy] nvarchar(250) NULL,
        CONSTRAINT [PK_HistoryRecords] PRIMARY KEY NONCLUSTERED ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Releases].[Projects] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsArchived] bit NOT NULL,
        [CreatedOn] datetime2(3) NOT NULL,
        [CreatedBy] nvarchar(120) NOT NULL,
        [CreatedByUserId] nvarchar(64) NOT NULL,
        [ModifiedOn] datetime2(3) NULL,
        [ModifiedBy] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Authorization].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Authorization].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ExternalId] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Releases].[ReleaseNotices] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] nvarchar(20) NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [TargetEnvironment] nvarchar(20) NOT NULL,
        [Version] nvarchar(50) NOT NULL,
        [LeadDeveloper] nvarchar(150) NOT NULL,
        [CreatedOn] datetime2(3) NOT NULL,
        [CreatedBy] nvarchar(120) NOT NULL,
        [CreatedByUserId] nvarchar(64) NOT NULL,
        [DeployedOn] datetime2(3) NULL,
        [ReleaseNotes] nvarchar(max) NULL,
        [ScheduledReleaseId] uniqueidentifier NULL,
        [RolledBackOn] datetime2(3) NULL,
        [RollbackReason] nvarchar(2000) NULL,
        [RollbackOfReleaseId] uniqueidentifier NULL,
        [ScheduledFor] datetime2(3) NULL,
        [Summary] nvarchar(2000) NULL,
        [RiskLevel] nvarchar(20) NULL,
        [RiskNotes] nvarchar(2000) NULL,
        CONSTRAINT [PK_ReleaseNotices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReleaseNotices_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Releases].[Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReleaseNotices_ReleaseNotices_RollbackOfReleaseId] FOREIGN KEY ([RollbackOfReleaseId]) REFERENCES [Releases].[ReleaseNotices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReleaseNotices_ReleaseNotices_ScheduledReleaseId] FOREIGN KEY ([ScheduledReleaseId]) REFERENCES [Releases].[ReleaseNotices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Releases].[StakeholderGroups] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NULL,
        [IsMandatory] bit NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetime2(3) NOT NULL,
        [CreatedBy] nvarchar(120) NOT NULL,
        [CreatedByUserId] nvarchar(64) NOT NULL,
        [ModifiedOn] datetime2(3) NULL,
        [ModifiedBy] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_StakeholderGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StakeholderGroups_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Releases].[Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Authorization].[UserRoles] (
        [RolesId] uniqueidentifier NOT NULL,
        [UsersId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([RolesId], [UsersId]),
        CONSTRAINT [FK_UserRoles_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Authorization].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Authorization].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE TABLE [Releases].[Stakeholders] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NULL,
        [IsMandatory] bit NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedOn] datetime2(3) NOT NULL,
        [CreatedBy] nvarchar(120) NOT NULL,
        [CreatedByUserId] nvarchar(64) NOT NULL,
        [ModifiedOn] datetime2(3) NULL,
        [ModifiedBy] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_Stakeholders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stakeholders_StakeholderGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Releases].[StakeholderGroups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_DomainEvents_Name] ON [Audit].[DomainEvents] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_DomainEvents_OccurredOn] ON [Audit].[DomainEvents] ([OccurredOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_HistoryRecords_CorrelationId] ON [History].[HistoryRecords] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_HistoryRecords_HappenedOn] ON [History].[HistoryRecords] ([HappenedOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Projects_Name] ON [Releases].[Projects] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_ReleaseNotices_ProjectId_CreatedOn] ON [Releases].[ReleaseNotices] ([ProjectId], [CreatedOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_ReleaseNotices_RollbackOfReleaseId] ON [Releases].[ReleaseNotices] ([RollbackOfReleaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_ReleaseNotices_ScheduledFor] ON [Releases].[ReleaseNotices] ([ScheduledFor]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_ReleaseNotices_ScheduledReleaseId] ON [Releases].[ReleaseNotices] ([ScheduledReleaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ReleaseNotices_ProjectId_Kind_Version] ON [Releases].[ReleaseNotices] ([ProjectId], [Kind], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Roles_Name] ON [Authorization].[Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_StakeholderGroups_ProjectId] ON [Releases].[StakeholderGroups] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_StakeholderGroups_ProjectId_Name] ON [Releases].[StakeholderGroups] ([ProjectId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_Stakeholders_GroupId] ON [Releases].[Stakeholders] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_Stakeholders_ProjectId] ON [Releases].[Stakeholders] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Stakeholders_ProjectId_Email] ON [Releases].[Stakeholders] ([ProjectId], [Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE INDEX [IX_UserRoles_UsersId] ON [Authorization].[UserRoles] ([UsersId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Users_ExternalId] ON [Authorization].[Users] ([ExternalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904003026_Initial_Migration'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904003026_Initial_Migration', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013220908_Add_ProjectDocuments'
)
BEGIN
    CREATE TABLE [Releases].[ProjectDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Kind] nvarchar(20) NOT NULL,
        [PageCount] int NOT NULL,
        [Summary] nvarchar(2000) NULL,
        [ExtractedText] nvarchar(max) NOT NULL,
        [Entities] nvarchar(max) NULL,
        [OcrProvider] nvarchar(50) NULL,
        [NerProvider] nvarchar(50) NOT NULL,
        [MatchConfidence] decimal(4,3) NULL,
        [MatchReason] nvarchar(1000) NULL,
        [AddedOn] datetime2(3) NOT NULL,
        [AddedBy] nvarchar(120) NOT NULL,
        [AddedByUserId] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_ProjectDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectDocuments_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Releases].[Projects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013220908_Add_ProjectDocuments'
)
BEGIN
    CREATE INDEX [IX_ProjectDocuments_ProjectId_AddedOn] ON [Releases].[ProjectDocuments] ([ProjectId], [AddedOn]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013220908_Add_ProjectDocuments'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013220908_Add_ProjectDocuments', N'10.0.2');
END;

COMMIT;
GO

