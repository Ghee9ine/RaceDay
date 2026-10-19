/*
    RaceDay - Database Schema and Seed Data
    Target: Microsoft SQL Server (SSMS)

    Run this whole script against a clean instance. It creates the RaceDayDb database,
    all tables with their keys and constraints, then seeds realistic sample data.
    Safe to re-run: it drops the database first if it already exists.
*/

IF DB_ID('RaceDayDb') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDb;
END
GO

CREATE DATABASE RaceDayDb;
GO

USE RaceDayDb;
GO

-- ==========================================================================
-- Table: Users
-- Holds both Organisers and Participants, distinguished by the Role column.
-- ProfilePictureUrl is nullable and reserved for Part 3 (Azure Blob Storage).
-- ==========================================================================
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(150)       NOT NULL,
    Email           NVARCHAR(200)       NOT NULL,
    PasswordHash    NVARCHAR(256)       NOT NULL,
    Role            NVARCHAR(20)        NOT NULL,
    PhoneNumber     NVARCHAR(20)        NULL,
    ProfilePictureUrl NVARCHAR(500)     NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- ==========================================================================
-- Table: Events
-- Each event is owned by exactly one Organiser (one-to-many: Users -> Events).
-- BannerImageUrl is nullable and reserved for Part 3 (Azure Blob Storage).
-- ==========================================================================
CREATE TABLE Events (
    EventId         INT IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT                 NOT NULL,
    Name            NVARCHAR(150)       NOT NULL,
    Description     NVARCHAR(MAX)       NULL,
    DisciplineType  NVARCHAR(20)        NOT NULL,
    EventDate       DATE                NOT NULL,
    StartTime       TIME                NULL,
    Province        NVARCHAR(50)        NULL,
    Venue           NVARCHAR(200)       NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Published',
    BannerImageUrl  NVARCHAR(500)       NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_DisciplineType CHECK (DisciplineType IN ('Running', 'Cycling', 'Walking')),
    CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft', 'Published', 'Cancelled', 'Completed'))
);
GO

-- ==========================================================================
-- Table: Categories
-- Each event offers many categories (one-to-many: Events -> Categories).
-- AgeGroup is nullable and distinct from distance (e.g. Open, Under 20,
-- Senior, Veteran), so age-based and distance-based categories both fit.
-- ==========================================================================
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1)   NOT NULL,
    EventId         INT                 NOT NULL,
    Name            NVARCHAR(100)       NOT NULL,
    DistanceKm      DECIMAL(6,2)        NOT NULL,
    EntryFee        DECIMAL(8,2)        NOT NULL DEFAULT 0,
    MaxParticipants INT                 NULL,
    AgeGroup        NVARCHAR(30)        NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT UQ_Categories_EventName UNIQUE (EventId, Name),
    CONSTRAINT CK_Categories_Distance CHECK (DistanceKm > 0)
);
GO

-- ==========================================================================
-- Table: Routes
-- One route per category (one-to-one: Categories -> Routes).
-- ==========================================================================
CREATE TABLE Routes (
    RouteId         INT IDENTITY(1,1)   NOT NULL,
    CategoryId      INT                 NOT NULL,
    StartPoint      NVARCHAR(200)       NOT NULL,
    EndPoint        NVARCHAR(200)       NOT NULL,
    ElevationGainM  INT                 NULL,
    MapUrl          NVARCHAR(300)       NULL,
    CONSTRAINT PK_Routes PRIMARY KEY (RouteId),
    CONSTRAINT FK_Routes_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Routes_Category UNIQUE (CategoryId)
);
GO

-- ==========================================================================
-- Table: WeatherSnapshots
-- Each event can have many recorded forecasts over time (one-to-many).
-- ==========================================================================
CREATE TABLE WeatherSnapshots (
    WeatherSnapshotId   INT IDENTITY(1,1)  NOT NULL,
    EventId             INT                NOT NULL,
    RecordedAt          DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
    TemperatureC        DECIMAL(4,1)       NULL,
    WindSpeedKmh        DECIMAL(5,1)       NULL,
    ConditionDescription NVARCHAR(100)     NULL,
    Source              NVARCHAR(50)       NULL DEFAULT 'OpenWeatherMap',
    CONSTRAINT PK_WeatherSnapshots PRIMARY KEY (WeatherSnapshotId),
    CONSTRAINT FK_WeatherSnapshots_Event FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

-- ==========================================================================
-- Table: Enrolments
-- Junction table resolving the many-to-many relationship between
-- Participants (Users) and Categories: a participant can enter many
-- categories, and a category can have many participants.
-- ==========================================================================
CREATE TABLE Enrolments (
    EnrolmentId     INT IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT                 NOT NULL,
    CategoryId      INT                 NOT NULL,
    BibNumber       NVARCHAR(10)        NULL,
    EnrolmentDate   DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_ParticipantCategory UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled'))
);
GO

-- ==========================================================================
-- Table: Results
-- One result per enrolment (one-to-one: Enrolments -> Results). Also
-- records which organiser captured the result (many-to-one to Users).
-- ==========================================================================
CREATE TABLE Results (
    ResultId            INT IDENTITY(1,1) NOT NULL,
    EnrolmentId         INT               NOT NULL,
    CapturedByUserId    INT               NOT NULL,
    FinishTime          TIME              NULL,
    Position            INT               NULL,
    Status              NVARCHAR(20)      NOT NULL DEFAULT 'Finished',
    CapturedAt          DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT FK_Results_CapturedBy FOREIGN KEY (CapturedByUserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DSQ'))
);
GO

-- ==========================================================================
-- Seed data
-- ==========================================================================

-- Organisers and Participants (2 of each minimum). Password for every
-- seeded user is "RaceDay@2026", hashed with ASP.NET Core's
-- PasswordHasher<User> (PBKDF2/HMACSHA256, v3 format) so a database built
-- from this script can log in through the Part 2 API, not just via EF seed.
INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
    ('Thandiwe Mokoena',  'thandiwe.mokoena@raceday.co.za', 'AQAAAAIAAYagAAAAEN/igyvthei00bw20ou/nQjhb5lhjkbGJRJZgARHm7rWq1irGZmpfNAelwntf31FUg==', 'Organiser',   '0821234567'),
    ('Johan van der Merwe','johan.vdm@raceday.co.za',        'AQAAAAIAAYagAAAAEHRth9G5/D9pTe1DYKMCCCECWo+VQcj5BXBf1HEbVn+8rH03+dvVNbht0LzhwU0iqA==', 'Organiser',   '0837654321'),
    ('Lindiwe Dlamini',   'lindiwe.dlamini@example.com',    'AQAAAAIAAYagAAAAEHXXmr59UQXdvNs3XXfu7ZFqnmHQKxbMGaqunyd7Hy/fvXOVzTa8h4tRnzkKvsJ7CQ==', 'Participant', '0731112222'),
    ('Ryan Naidoo',       'ryan.naidoo@example.com',        'AQAAAAIAAYagAAAAEELtWjuh3P65EEQYZCd4C1Wkmy55hEFWwtK7YV1oR7GpT2RqsiXHVozafFD2kBATdQ==', 'Participant', '0723334444');
GO

-- Events: 3 already-run events (dated earlier in 2026, so Completed rather
-- than Published - an upcoming-events browse should not return them) plus
-- one genuinely upcoming Published event for "browse upcoming events" to
-- have something to return.
INSERT INTO Events (OrganiserId, Name, Description, DisciplineType, EventDate, StartTime, Province, Venue, Status) VALUES
    (1, 'Pietermaritzburg to Durban Classic', 'Ultramarathon following the traditional Comrades-style route.', 'Running', '2026-06-14', '05:30:00', 'KwaZulu-Natal', 'Pietermaritzburg City Hall', 'Completed'),
    (1, 'Cape Peninsula Cycle Tour',          'Scenic road cycling event around the Cape Peninsula.',         'Cycling', '2026-03-08', '06:00:00', 'Western Cape',   'Grand Parade, Cape Town', 'Completed'),
    (2, 'Soweto Community Fun Walk',          'Family-friendly charity walk through Soweto.',                  'Walking', '2026-04-19', '07:00:00', 'Gauteng',        'Orlando Stadium', 'Completed'),
    (2, 'Joburg Night Run',                   'Floodlit night run and fun run around Zoo Lake.',               'Running', '2027-03-13', '19:00:00', 'Gauteng',        'Zoo Lake', 'Published');
GO

-- Categories for each event. AgeGroup is populated for the new event to
-- show the feature in use; existing events default to 'Open' since they
-- were never split by age.
INSERT INTO Categories (EventId, Name, DistanceKm, EntryFee, MaxParticipants, AgeGroup) VALUES
    (1, '87km Ultramarathon', 87.00, 850.00, 5000, 'Open'),
    (1, '21km Half Marathon', 21.10, 450.00, 2000, 'Open'),
    (2, '109km Cycle Race',  109.00, 650.00, 3000, 'Open'),
    (2, '45km Fun Ride',      45.00, 350.00, 1500, 'Open'),
    (3, '5km Fun Walk',        5.00,   0.00, 1000, 'Open'),
    (3, '10km Charity Walk',  10.00,  50.00,  800, 'Open'),
    (4, '10km Run',           10.00, 150.00, 1000, 'Open'),
    (4, '5km Fun Run',         5.00,  80.00, 1500, 'Under 20');
GO

-- Routes (one per category, at least a representative sample)
INSERT INTO Routes (CategoryId, StartPoint, EndPoint, ElevationGainM, MapUrl) VALUES
    (1, 'Pietermaritzburg City Hall', 'Kingsmead Stadium, Durban', 1200, 'https://maps.raceday.co.za/routes/pmb-durban-87k'),
    (2, 'Pietermaritzburg City Hall', 'Cascades, Pietermaritzburg', 350, 'https://maps.raceday.co.za/routes/pmb-half'),
    (3, 'Grand Parade, Cape Town', 'Grand Parade, Cape Town', 980, 'https://maps.raceday.co.za/routes/cape-peninsula-109k'),
    (5, 'Orlando Stadium', 'Orlando Stadium', 20, 'https://maps.raceday.co.za/routes/soweto-5k');
GO

-- Weather snapshots (sample forecast history)
INSERT INTO WeatherSnapshots (EventId, TemperatureC, WindSpeedKmh, ConditionDescription, Source) VALUES
    (1, 14.5, 12.0, 'Clear skies, cool start', 'OpenWeatherMap'),
    (2, 19.0, 22.5, 'Partly cloudy, moderate coastal wind', 'OpenWeatherMap'),
    (3, 16.0,  8.0, 'Overcast, light drizzle expected', 'OpenWeatherMap');
GO

-- Enrolments (participants entering categories)
INSERT INTO Enrolments (ParticipantId, CategoryId, BibNumber, Status) VALUES
    (3, 1, 'A1001', 'Confirmed'),
    (3, 5, 'C5001', 'Confirmed'),
    (4, 2, 'B2001', 'Confirmed'),
    (4, 3, 'D3001', 'Confirmed');
GO

-- Results (captured by the owning organiser for completed enrolments)
INSERT INTO Results (EnrolmentId, CapturedByUserId, FinishTime, Position, Status) VALUES
    (1, 1, '08:42:15', 134, 'Finished'),
    (3, 1, '01:58:03', 27,  'Finished');
GO
