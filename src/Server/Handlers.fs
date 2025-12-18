// =============================================================================
// HTTP HANDLERS (API Layer)
// =============================================================================
// This module contains the HTTP handlers that bridge the web API to the
// CQRS/ES domain. Handlers either:
//   - Query the read model (simple database queries)
//   - Send commands to actors and await events (write operations)
//
// Pattern for write operations:
//   1. Parse and validate HTTP request
//   2. Create domain objects using smart constructors
//   3. Generate correlation ID for tracking
//   4. Subscribe to events with that correlation ID
//   5. Send command to actor
//   6. Await the resulting event (confirms persistence)
//   7. Return response to client
//
// EXERCISE: Implement the command handlers
// =============================================================================

module Handlers

open System
open Microsoft.AspNetCore.Http
open FCQRS
open FCQRS.Model.Data
open Command
open Model.Command
open FsToolkit.ErrorHandling
open Hole

type ISubscribe<'T> = Query.ISubscribe<'T>

// -----------------------------------------------------------------------------
// QUERY HANDLERS (Read Side)
// -----------------------------------------------------------------------------
// These simply query the read model - fast and simple
// -----------------------------------------------------------------------------

let getDocuments (connectionString: string) () =
    ServerQuery.getDocuments connectionString |> Seq.toArray

let getDocumentHistory (connectionString: string) (ctx: HttpContext) =
    let id = ctx.Request.RouteValues["id"].ToString()
    ServerQuery.getDocumentHistory connectionString id |> Seq.toArray

// -----------------------------------------------------------------------------
// COMMAND HANDLERS (Write Side)
// -----------------------------------------------------------------------------
// These send commands to actors and await confirmation via events
// -----------------------------------------------------------------------------

// Creates a new document or updates an existing one
// EXERCISE: Implement the full CQRS write flow
let createOrUpdateDocument
    (cid: unit -> CID)                              // Correlation ID factory
    (subs: ISubscribe<IMessageWithCID>)             // Event subscription service
    (commandHandler: CommandHandler.ICommandHandlers)
    (ctx: HttpContext)
    =
    task {
        let! result =
            taskResult {
                // EXERCISE Step 1: Parse form data from HTTP request
                // Hint: let! form = ctx.Request.ReadFormAsync()
                //       let title = form["Title"].ToString()
                let! form = ctx.Request.ReadFormAsync()
                let title = form["Title"].ToString()
                let content = form["Content"].ToString()
                let existingId = form["Id"].ToString()

                // EXERCISE Step 2: Generate new ID or use existing
                // If existingId is empty, create new Guid, otherwise parse it
                let docId : Guid =
                    Hole?TODO_DetermineDocumentId

                // EXERCISE Step 3: Create validated domain objects
                // Hint: Use ValueLens.CreateAsResult for aggregateId
                //       Use Document.Create(docId, title, content) for the document
                let! aggregateId = Hole?TODO_CreateAggregateId
                let! document = Hole?TODO_CreateValidatedDocument

                // Generate correlation ID to track this operation
                let correlationId = cid ()

                // Subscribe to events with this correlation ID BEFORE sending command
                // This ensures we don't miss the event due to race conditions
                use awaiter = subs.Subscribe((fun e -> e.CID = correlationId), 1)

                // EXERCISE Step 4: Send command to the actor
                // Hint: commandHandler.DocumentHandler (fun _ -> true) correlationId aggregateId (Document.CreateOrUpdate document)
                let! _ = Hole?TODO_SendCommandToActor

                // Wait for the event to be projected (confirms read model is updated)
                do! awaiter.Task

                return "Document received!"
            }

        return
            match result with
            | Ok msg -> msg
            | Error err -> $"Error: %A{err}"
    }

// Restores a document to a previous version (time-travel feature)
// EXERCISE: Implement version restoration
let restoreVersion
    (connectionString: string)
    (cid: unit -> CID)
    (subs: ISubscribe<IMessageWithCID>)
    (commandHandler: CommandHandler.ICommandHandlers)
    (ctx: HttpContext)
    =
    task {
        let! result =
            taskResult {
                let! form = ctx.Request.ReadFormAsync()
                let docId = form["Id"].ToString()
                let version = form["Version"].ToString() |> int64

                // EXERCISE: Look up the historical version from the read model
                // Hint: Use ServerQuery.getDocumentHistory and Seq.tryFind
                //       Then use Result.requireSome to convert Option to Result
                let history = ServerQuery.getDocumentHistory connectionString docId
                let! (versionData: Query.DocumentVersion) =
                    Hole?TODO_FindVersionInHistory

                // EXERCISE: Recreate the document from historical data and send command
                // This is similar to createOrUpdateDocument but uses historical data
                let guid = Guid.Parse(docId)
                let! aggregateId:AggregateId = docId |> ValueLens.CreateAsResult
                let! document = Document.Create(guid, versionData.Title, versionData.Body)

                // EXERCISE Step 4: Generate correlation ID for tracking this operation
                // Hint: Use the cid factory function
                let correlationId : CID = Hole?TODO_GenerateCorrelationId

                // EXERCISE Step 5: Subscribe to events BEFORE sending command
                // This prevents race conditions - we must be listening before the event fires
                // Hint: subs.Subscribe((fun e -> e.CID = correlationId), 1)
                use awaiter = Hole?TODO_SubscribeToEvents : Query.IAwaitableDisposable

                // EXERCISE Step 6: Send the restore command to the actor
                // Hint: commandHandler.DocumentHandler (fun _ -> true) correlationId aggregateId (Document.CreateOrUpdate document)
                let! _ = Hole?TODO_SendRestoreCommand

                // EXERCISE Step 7: Wait for the event to confirm projection is updated
                // Hint: awaiter.Task
                do! Hole?TODO_AwaitEventConfirmation

                return "Version restored!"
            }

        return
            match result with
            | Ok msg -> msg
            | Error err -> $"Error: %A{err}"
    }
