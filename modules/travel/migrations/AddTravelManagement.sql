-- Travel Management Module Tables
-- Run this script against your HRMST database after the module is linked

IF OBJECT_ID(N'[dbo].[TravelTravelRequestEmployees]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TravelTravelRequestEmployees] (
        [Id] uniqueidentifier NOT NULL,
        [TravelRequestId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [EmployeeName] nvarchar(200) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterId] uniqueidentifier NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_TravelTravelRequestEmployees] PRIMARY KEY ([Id])
    );
END
GO

IF OBJECT_ID(N'[dbo].[TravelTravelDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TravelTravelDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [TravelRequestId] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [FileExtension] nvarchar(10) NOT NULL,
        [FileSize] bigint NOT NULL,
        [BlobName] nvarchar(500) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterId] uniqueidentifier NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_TravelTravelDocuments] PRIMARY KEY ([Id])
    );
END
GO

IF OBJECT_ID(N'[dbo].[TravelTravelRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TravelTravelRequests] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [DestinationCountry] nvarchar(100) NOT NULL,
        [DestinationCity] nvarchar(100) NOT NULL,
        [Department] nvarchar(200) NULL,
        [DailyAllowanceRate] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NULL,
        [NeedsPermission] bit NOT NULL,
        [IncludesAccommodation] bit NOT NULL,
        [RequesterId] uniqueidentifier NOT NULL,
        [AllowanceSnapshot_OverseasTotal] decimal(18,2) NULL,
        [AllowanceSnapshot_ClothingTotal] decimal(18,2) NULL,
        [AllowanceSnapshot_DeductionAmount] decimal(18,2) NULL,
        [AllowanceSnapshot_CalculatedAt] datetime2 NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterId] uniqueidentifier NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_TravelTravelRequests] PRIMARY KEY ([Id])
    );
END
GO

-- Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequests_Type' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequests]'))
    CREATE INDEX [IX_TravelTravelRequests_Type] ON [dbo].[TravelTravelRequests] ([Type]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequests_Status' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequests]'))
    CREATE INDEX [IX_TravelTravelRequests_Status] ON [dbo].[TravelTravelRequests] ([Status]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequests_RequesterId' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequests]'))
    CREATE INDEX [IX_TravelTravelRequests_RequesterId] ON [dbo].[TravelTravelRequests] ([RequesterId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequests_Department' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequests]'))
    CREATE INDEX [IX_TravelTravelRequests_Department] ON [dbo].[TravelTravelRequests] ([Department]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequests_CreationTime' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequests]'))
    CREATE INDEX [IX_TravelTravelRequests_CreationTime] ON [dbo].[TravelTravelRequests] ([CreationTime]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequestEmployees_TravelRequestId' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequestEmployees]'))
    CREATE INDEX [IX_TravelTravelRequestEmployees_TravelRequestId] ON [dbo].[TravelTravelRequestEmployees] ([TravelRequestId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelRequestEmployees_EmployeeId' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelRequestEmployees]'))
    CREATE INDEX [IX_TravelTravelRequestEmployees_EmployeeId] ON [dbo].[TravelTravelRequestEmployees] ([EmployeeId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_TravelTravelDocuments_TravelRequestId' AND object_id = OBJECT_ID(N'[dbo].[TravelTravelDocuments]'))
    CREATE INDEX [IX_TravelTravelDocuments_TravelRequestId] ON [dbo].[TravelTravelDocuments] ([TravelRequestId]);
GO

-- Foreign Keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_TravelTravelRequestEmployees_TravelTravelRequests_TravelRequestId')
    ALTER TABLE [dbo].[TravelTravelRequestEmployees] ADD CONSTRAINT [FK_TravelTravelRequestEmployees_TravelTravelRequests_TravelRequestId] FOREIGN KEY ([TravelRequestId]) REFERENCES [dbo].[TravelTravelRequests] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_TravelTravelDocuments_TravelTravelRequests_TravelRequestId')
    ALTER TABLE [dbo].[TravelTravelDocuments] ADD CONSTRAINT [FK_TravelTravelDocuments_TravelTravelRequests_TravelRequestId] FOREIGN KEY ([TravelRequestId]) REFERENCES [dbo].[TravelTravelRequests] ([Id]) ON DELETE CASCADE;
GO
