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

CREATE TABLE [Buildings] (
    [Id] uniqueidentifier NOT NULL,
    [BldgNr] nvarchar(50) NOT NULL,
    [BldgName] nvarchar(100) NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Buildings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Committees] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Committees] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Degrees] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(10) NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Degrees] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Extensions] (
    [Id] uniqueidentifier NOT NULL,
    [ExtNr] nvarchar(10) NOT NULL,
    [AcademicId] uniqueidentifier NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Extensions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Subjects] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Universities] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Universities] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Rooms] (
    [Id] uniqueidentifier NOT NULL,
    [RoomNr] nvarchar(10) NOT NULL,
    [BuildingId] uniqueidentifier NOT NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rooms_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AcademicDegrees] (
    [AcademicId] uniqueidentifier NOT NULL,
    [DegreeId] uniqueidentifier NOT NULL,
    [UniversityId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AcademicDegrees] PRIMARY KEY ([AcademicId], [DegreeId]),
    CONSTRAINT [FK_AcademicDegrees_Degrees_DegreeId] FOREIGN KEY ([DegreeId]) REFERENCES [Degrees] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Academics] (
    [Id] uniqueidentifier NOT NULL,
    [EmpNr] nvarchar(10) NOT NULL,
    [EmpName] nvarchar(100) NOT NULL,
    [Rank] nvarchar(10) NOT NULL,
    [IsTenured] bit NOT NULL,
    [ContractEndDate] datetime2 NULL,
    [HomePhoneNumber] nvarchar(20) NULL,
    [DepartmentId] uniqueidentifier NULL,
    [RoomId] uniqueidentifier NULL,
    [ExtensionId] uniqueidentifier NULL,
    [ChairId] uniqueidentifier NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Academics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Academics_Extensions_ExtensionId] FOREIGN KEY ([ExtensionId]) REFERENCES [Extensions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Academics_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [AcademicSubjects] (
    [AcademicId] uniqueidentifier NOT NULL,
    [SubjectId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AcademicSubjects] PRIMARY KEY ([AcademicId], [SubjectId]),
    CONSTRAINT [FK_AcademicSubjects_Academics_AcademicId] FOREIGN KEY ([AcademicId]) REFERENCES [Academics] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AcademicSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Chairs] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ProfessorId] uniqueidentifier NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Chairs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Chairs_Academics_ProfessorId] FOREIGN KEY ([ProfessorId]) REFERENCES [Academics] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [CommitteeTeachingProfessors] (
    [CommitteeId] uniqueidentifier NOT NULL,
    [AcademicId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CommitteeTeachingProfessors] PRIMARY KEY ([CommitteeId], [AcademicId]),
    CONSTRAINT [FK_CommitteeTeachingProfessors_Academics_AcademicId] FOREIGN KEY ([AcademicId]) REFERENCES [Academics] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CommitteeTeachingProfessors_Committees_CommitteeId] FOREIGN KEY ([CommitteeId]) REFERENCES [Committees] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Departments] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ResearchBudgetAmount] decimal(18,2) NULL,
    [TeachingBudgetAmount] decimal(18,2) NULL,
    [HeadHomePhoneNumber] nvarchar(20) NULL,
    [HeadProfessorId] uniqueidentifier NULL,
    [ChairId] uniqueidentifier NULL,
    [Version] rowversion NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_Academics_HeadProfessorId] FOREIGN KEY ([HeadProfessorId]) REFERENCES [Academics] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Departments_Chairs_ChairId] FOREIGN KEY ([ChairId]) REFERENCES [Chairs] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_AcademicDegrees_DegreeId] ON [AcademicDegrees] ([DegreeId]);
GO

CREATE INDEX [IX_Academics_ChairId] ON [Academics] ([ChairId]);
GO

CREATE INDEX [IX_Academics_DepartmentId] ON [Academics] ([DepartmentId]);
GO

CREATE UNIQUE INDEX [IX_Academics_EmpNr] ON [Academics] ([EmpNr]);
GO

CREATE INDEX [IX_Academics_ExtensionId] ON [Academics] ([ExtensionId]);
GO

CREATE INDEX [IX_Academics_RoomId] ON [Academics] ([RoomId]);
GO

CREATE INDEX [IX_AcademicSubjects_SubjectId] ON [AcademicSubjects] ([SubjectId]);
GO

CREATE UNIQUE INDEX [IX_Buildings_BldgName] ON [Buildings] ([BldgName]);
GO

CREATE INDEX [IX_Chairs_ProfessorId] ON [Chairs] ([ProfessorId]);
GO

CREATE INDEX [IX_CommitteeTeachingProfessors_AcademicId] ON [CommitteeTeachingProfessors] ([AcademicId]);
GO

CREATE UNIQUE INDEX [IX_Degrees_Code] ON [Degrees] ([Code]);
GO

CREATE INDEX [IX_Departments_ChairId] ON [Departments] ([ChairId]);
GO

CREATE INDEX [IX_Departments_HeadProfessorId] ON [Departments] ([HeadProfessorId]);
GO

CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
GO

CREATE UNIQUE INDEX [IX_Extensions_ExtNr] ON [Extensions] ([ExtNr]);
GO

CREATE INDEX [IX_Rooms_BuildingId] ON [Rooms] ([BuildingId]);
GO

CREATE UNIQUE INDEX [IX_Rooms_RoomNr_BuildingId] ON [Rooms] ([RoomNr], [BuildingId]);
GO

CREATE UNIQUE INDEX [IX_Subjects_Code] ON [Subjects] ([Code]);
GO

ALTER TABLE [AcademicDegrees] ADD CONSTRAINT [FK_AcademicDegrees_Academics_AcademicId] FOREIGN KEY ([AcademicId]) REFERENCES [Academics] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [Academics] ADD CONSTRAINT [FK_Academics_Chairs_ChairId] FOREIGN KEY ([ChairId]) REFERENCES [Chairs] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Academics] ADD CONSTRAINT [FK_Academics_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250716042336_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

