// =============================================================================
// DOCUMENT AGGREGATE (Actor-based Event Sourcing)
// =============================================================================
// This module implements the Document aggregate using the Actor Model pattern.
// Each document entity is represented by an actor that:
//   1. Maintains its own state in memory
//   2. Processes commands sequentially (no concurrency issues)
//   3. Persists events to an event store (Akka.Persistence)
//   4. Recovers state by replaying events on startup
//
// EXERCISE: Implement the core event sourcing logic
// =============================================================================

module Command.Document

open FCQRS.Common
open Model.Command
open Model.Command.Document
open Hole

// -----------------------------------------------------------------------------
// STATE: The in-memory representation of a Document aggregate
// -----------------------------------------------------------------------------
// This state is:
//   - Rebuilt from events when the actor starts (event replay)
//   - Updated in memory after each event is persisted
//   - Never persisted directly (only events are persisted)
// -----------------------------------------------------------------------------
type State = {
    Document: Document option  // The current document data (None if not created)
    Version: int64             // Optimistic concurrency version counter
}

let initialState = { Document = None; Version = 0L }

// -----------------------------------------------------------------------------
// SHARD: The actor implementation following CQRS/ES patterns
// -----------------------------------------------------------------------------
// The "Shard" name comes from Akka.NET Cluster Sharding, which distributes
// actors across cluster nodes. Each entity ID maps to exactly one actor instance.
// -----------------------------------------------------------------------------
type Shard =

    // -------------------------------------------------------------------------
    // EVENT APPLICATION (Pure Function)
    // -------------------------------------------------------------------------
    // ApplyEvent: Event × State → State
    //
    // This function is called:
    //   1. During recovery: replaying all stored events to rebuild state
    //   2. After persistence: updating in-memory state with the new event
    //
    // IMPORTANT: This must be a PURE function with no side effects!
    // The same events replayed must always produce the same state.
    //
    // EXERCISE: Pattern match on the event and update state accordingly
    //   - CreatedOrUpdated: Set Document to Some doc, increment Version
    //   - Error: Don't change state (errors are recorded but don't affect state)
    // -------------------------------------------------------------------------
    static member ApplyEvent(event: Event<Event>, state: State) =
        match event.EventDetails with
        | CreatedOrUpdated doc ->
            Hole?TODO_ApplyCreatedOrUpdatedEvent
        | Error _ ->
            Hole?TODO_ApplyErrorEvent

    // -------------------------------------------------------------------------
    // COMMAND HANDLING (Decision Function)
    // -------------------------------------------------------------------------
    // HandleCommand: Command × State → EventDecision
    //
    // This is where business logic lives. Given a command and current state,
    // decide what event(s) should be produced.
    //
    // Return types:
    //   - PersistEvent: Event will be stored and then applied to state
    //   - DeferEvent: Event is emitted but NOT persisted (for errors/rejections)
    //
    // EXERCISE: Implement the business rules:
    //   - CreateOrUpdate with no existing doc → CreatedOrUpdated (persist)
    //   - CreateOrUpdate with matching ID → CreatedOrUpdated (persist)
    //   - CreateOrUpdate with different ID → Error DocumentNotFound (defer)
    // -------------------------------------------------------------------------
    static member HandleCommand(cmd: Command<Command>, state: State) =
        match cmd.CommandDetails, state.Document with
        // Create new document (no existing document)
        | CreateOrUpdate doc, None ->
            Hole?TODO_HandleCreateNew
        // Update existing document (IDs must match)
        | CreateOrUpdate doc, Some existing when existing.Id = doc.Id ->
            Hole?TODO_HandleUpdate
        // Reject: trying to update with wrong ID (business rule violation)
        | CreateOrUpdate _, Some _ ->
            Hole?TODO_HandleIdMismatch

    // -------------------------------------------------------------------------
    // ACTOR INITIALIZATION
    // -------------------------------------------------------------------------
    // This wires up the actor with the FCQRS framework:
    //   - initialState: Starting state for new entities
    //   - entityName: Used for actor path and event journal tagging
    //   - HandleCommand: Curried to match the framework's expected signature
    //   - ApplyEvent: Curried to match the framework's expected signature
    //
    // The Curry.curry converts (a, b) -> c to a -> b -> c form
    // -------------------------------------------------------------------------
    static member Init(actorApi: IActor, entityName) =
        actorApi.InitializeActor
            initialState
            entityName
            (Curry.curry Shard.HandleCommand)
            (Curry.curry Shard.ApplyEvent)

    // Factory: Creates a reference to a specific document actor by entity ID
    static member Factory actorApi =
        Shard.Init(actorApi, "Document").RefFor DEFAULT_SHARD

    // Handler: Creates a command handler that routes commands to the right actor
    static member Handler actorApi = commandHandler<Shard, _, _, _> actorApi
