IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [ProfilePictureUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('Organiser','Participant'))
);
GO

CREATE TABLE [Events] (
    [EventId] int NOT NULL IDENTITY,
    [OrganiserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DisciplineType] nvarchar(20) NOT NULL,
    [EventDate] date NOT NULL,
    [StartTime] time NULL,
    [Province] nvarchar(50) NULL,
    [Venue] nvarchar(200) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Published',
    [BannerImageUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Events] PRIMARY KEY ([EventId]),
    CONSTRAINT [CK_Events_DisciplineType] CHECK ([DisciplineType] IN ('Running','Cycling','Walking')),
    CONSTRAINT [CK_Events_Status] CHECK ([Status] IN ('Draft','Published','Cancelled','Completed')),
    CONSTRAINT [FK_Events_Organiser] FOREIGN KEY ([OrganiserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [DistanceKm] decimal(6,2) NOT NULL,
    [EntryFee] decimal(8,2) NOT NULL DEFAULT 0.0,
    [MaxParticipants] int NULL,
    [AgeGroup] nvarchar(30) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [CK_Categories_Distance] CHECK ([DistanceKm] > 0),
    CONSTRAINT [FK_Categories_Event] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [WeatherSnapshots] (
    [WeatherSnapshotId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [RecordedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [TemperatureC] decimal(4,1) NULL,
    [WindSpeedKmh] decimal(5,1) NULL,
    [ConditionDescription] nvarchar(100) NULL,
    [Source] nvarchar(50) NULL DEFAULT N'OpenWeatherMap',
    CONSTRAINT [PK_WeatherSnapshots] PRIMARY KEY ([WeatherSnapshotId]),
    CONSTRAINT [FK_WeatherSnapshots_Event] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Enrolments] (
    [EnrolmentId] int NOT NULL IDENTITY,
    [ParticipantId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [BibNumber] nvarchar(10) NULL,
    [EnrolmentDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Status] nvarchar(20) NOT NULL DEFAULT N'Confirmed',
    CONSTRAINT [PK_Enrolments] PRIMARY KEY ([EnrolmentId]),
    CONSTRAINT [CK_Enrolments_Status] CHECK ([Status] IN ('Confirmed','Cancelled')),
    CONSTRAINT [FK_Enrolments_Category] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Enrolments_Participant] FOREIGN KEY ([ParticipantId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Routes] (
    [RouteId] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [StartPoint] nvarchar(200) NOT NULL,
    [EndPoint] nvarchar(200) NOT NULL,
    [ElevationGainM] int NULL,
    [MapUrl] nvarchar(300) NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([RouteId]),
    CONSTRAINT [FK_Routes_Category] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Results] (
    [ResultId] int NOT NULL IDENTITY,
    [EnrolmentId] int NOT NULL,
    [CapturedByUserId] int NOT NULL,
    [FinishTime] time NULL,
    [Position] int NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Finished',
    [CapturedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Results] PRIMARY KEY ([ResultId]),
    CONSTRAINT [CK_Results_Status] CHECK ([Status] IN ('Finished','DNF','DSQ')),
    CONSTRAINT [FK_Results_CapturedBy] FOREIGN KEY ([CapturedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Results_Enrolment] FOREIGN KEY ([EnrolmentId]) REFERENCES [Enrolments] ([EnrolmentId]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [UQ_Categories_EventName] ON [Categories] ([EventId], [Name]);
GO

CREATE INDEX [IX_Enrolments_CategoryId] ON [Enrolments] ([CategoryId]);
GO

CREATE UNIQUE INDEX [UQ_Enrolments_ParticipantCategory] ON [Enrolments] ([ParticipantId], [CategoryId]);
GO

CREATE INDEX [IX_Events_OrganiserId] ON [Events] ([OrganiserId]);
GO

CREATE INDEX [IX_Results_CapturedByUserId] ON [Results] ([CapturedByUserId]);
GO

CREATE UNIQUE INDEX [UQ_Results_Enrolment] ON [Results] ([EnrolmentId]);
GO

CREATE UNIQUE INDEX [UQ_Routes_Category] ON [Routes] ([CategoryId]);
GO

CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_WeatherSnapshots_EventId] ON [WeatherSnapshots] ([EventId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019121830_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

