// =============================================================================
// PROJECTION (Read Model Builder)
// =============================================================================
// This module implements the QUERY side of CQRS by projecting events into
// a read-optimized SQLite database.
//
// How it works:
//   1. Events are read from the event store (Akka.Persistence journal)
//   2. Each event is processed by handleEventWrapper
//   3. The read model (SQLite) is updated to reflect the event
//   4. Offset tracking ensures exactly-once processing
//
// This creates "eventual consistency" - the read model may lag slightly
// behind the write model, but will eventually catch up.
//
// EXERCISE: Implement the event projection logic
// =============================================================================

module Projection

open System
open Microsoft.Extensions.Logging
open Microsoft.Data.Sqlite
open Dapper
open FCQRS.Common
open FCQRS.Model.Data
open Model.Command
open Model.Command.Document
open Hole

// -----------------------------------------------------------------------------
// SCHEMA INITIALIZATION
// -----------------------------------------------------------------------------
// Creates the read model tables if they don't exist.
// This is idempotent - safe to call on every startup.
// -----------------------------------------------------------------------------
let ensureTables (connString: string) =
    use conn = new SqliteConnection(connString)
    conn.Open()

    conn.Execute(
        """
        CREATE TABLE IF NOT EXISTS Documents (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            Body TEXT NOT NULL,
            Version INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
    """
    )
    |> ignore

    conn.Execute(
        """
        CREATE TABLE IF NOT EXISTS Offsets (
            OffsetName TEXT PRIMARY KEY,
            OffsetCount INTEGER NOT NULL
        )
    """
    )
    |> ignore

    conn.Execute(
        """
        INSERT OR IGNORE INTO Offsets (OffsetName, OffsetCount) VALUES ('DocumentProjection', 0)
    """
    )
    |> ignore

    conn.Execute(
        """
        CREATE TABLE IF NOT EXISTS DocumentVersions (
            Id TEXT NOT NULL,
            Version INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Body TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            PRIMARY KEY (Id, Version)
        )
    """
    )
    |> ignore

// -----------------------------------------------------------------------------
// EVENT HANDLER (Projection Logic)
// -----------------------------------------------------------------------------
// This function is called for each event from the event store.
// It transforms domain events into read model updates.
//
// Parameters:
//   - offsetValue: Sequential position in the event stream (for resumption)
//   - event: The domain event to process
//
// The offset is stored atomically with the projection update to ensure
// exactly-once semantics even if the process crashes mid-projection.
//
// EXERCISE: Implement the projection logic for CreatedOrUpdated events
// =============================================================================
let handleEventWrapper (loggerFactory: ILoggerFactory) (connString: string) (offsetValue: int64) (event: obj) =
    let log = loggerFactory.CreateLogger "Projection"
    log.LogInformation("Event: {Event} Offset: {Offset}", event.ToString(), offsetValue)

    try
        use conn = new SqliteConnection(connString)
        conn.Open()
        use transaction = conn.BeginTransaction()

        let dataEvent =
            match event with
            | :? Event<Event> as docEvent ->
                let version = docEvent.Version |> ValueLens.Value
                let eventTime = docEvent.CreationDate.ToString("o")

                match docEvent.EventDetails with
                | CreatedOrUpdated doc ->
                    let docId = doc.Id.ToString()
                    let title = doc.Title.ToString()
                    let content = doc.Content.ToString()

                    // EXERCISE: Check if document exists, then INSERT or UPDATE accordingly
                    // 1. Query to check if document exists: SELECT Id FROM Documents WHERE Id = @Id
                    // 2. If null (new document): INSERT INTO Documents (Id, Title, Body, Version, CreatedAt, UpdatedAt) VALUES (...)
                    // 3. If exists: UPDATE Documents SET Title = @Title, Body = @Body, Version = @Version, UpdatedAt = @UpdatedAt WHERE Id = @Id
                    // Hint: Use conn.Execute(sql, {| parameters |}, transaction)
                    let existing : string =
                        Hole?TODO_CheckIfDocumentExists

                    if isNull existing then
                        Hole?TODO_InsertNewDocument
                    else
                        Hole?TODO_UpdateExistingDocument

                    // EXERCISE: Store version history for time-travel
                    // INSERT OR IGNORE INTO DocumentVersions (Id, Version, Title, Body, CreatedAt) VALUES (...)
                    Hole?TODO_InsertVersionHistory

                    [ docEvent :> IMessageWithCID ]

                | Error _ -> []

            | _ -> []

        conn.Execute(
            "UPDATE Offsets SET OffsetCount = @Offset WHERE OffsetName = @Name",
            {|
                Offset = offsetValue
                Name = "DocumentProjection"
            |},
            transaction
        )
        |> ignore

        transaction.Commit()
        dataEvent

    with ex ->
        log.LogError(
            ex,
            "Projection failed for event at offset {Offset}: {EventType}",
            offsetValue,
            event.GetType().Name
        )

        reraise ()
