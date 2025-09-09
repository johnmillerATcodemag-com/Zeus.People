IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory]
    (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EventStore]
(
    [Id] uniqueidentifier NOT NULL,
    [AggregateId] uniqueidentifier NOT NULL,
    [AggregateType] nvarchar(100) NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [EventData] nvarchar(max) NOT NULL,
    [Version] int NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [EventId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_EventStore] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_EventStore_AggregateId] ON [EventStore] ([AggregateId]);
GO

CREATE UNIQUE INDEX [IX_EventStore_AggregateId_Version] ON [EventStore] ([AggregateId], [Version]);
GO

CREATE INDEX [IX_EventStore_Timestamp] ON [EventStore] ([Timestamp]);
GO

CREATE UNIQUE INDEX [IX_EventStore_EventId] ON [EventStore] ([EventId]);
GO

INSERT INTO [__EFMigrationsHistory]
    ([MigrationId], [ProductVersion])
VALUES
    (N'20250716042336_InitialCreate', N'8.0.0');
GO

COMMIT;
GO
