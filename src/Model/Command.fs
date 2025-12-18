// =============================================================================
// DOMAIN MODEL - Command Side (Write Model)
// =============================================================================
// This module defines the domain types used on the COMMAND side of CQRS.
// These types are used when processing commands and generating events.
//
// Key Patterns Used:
//   1. Value Objects with private constructors (enforce invariants)
//   2. Smart constructors (Create/CreateFrom) for validated creation
//   3. Lens pattern (Value_) for controlled access to wrapped values
//
// EXERCISE: Fill in the holes marked with Hole?TODO_*
// =============================================================================

module Model.Command

open System
open FCQRS.Model.Data
open FsToolkit.ErrorHandling
open Hole

// -----------------------------------------------------------------------------
// VALUE OBJECTS
// -----------------------------------------------------------------------------
// Value objects wrap primitive types and enforce business rules.
// The 'private' keyword prevents direct construction - must use smart constructors.
// This ensures invalid values can never exist in the system.
// -----------------------------------------------------------------------------

// DocumentId: A validated GUID wrapper
type DocumentId =
    private
    | DocumentId of Guid

    // Lens for getting/setting the inner value (used by FCQRS serialization)
    static member Value_ = (fun (DocumentId id) -> id), (fun (id: Guid) _ -> DocumentId id)

    // Smart constructor: creates a new random ID
    static member Create() = DocumentId(Guid.NewGuid())

    // Smart constructor: parses from string, returns Result for validation
    // EXERCISE: Parse the string as a GUID. Return Ok(DocumentId) if valid, Error if not.
    // Hint: Use Guid.TryParse which returns (bool * Guid)
    static member CreateFrom(s: string) : Result<DocumentId, ModelError list> =
        Hole?TODO_ParseGuidAndReturnResult

    member _.IsValid = true
    override this.ToString() = (ValueLens.Value this).ToString()

// Title: Wraps ShortString (length-limited string from FCQRS)
type Title =
    private
    | Title of ShortString

    static member Value_ = (fun (Title s) -> s), (fun (s: ShortString) _ -> Title s)

    member this.IsValid = (ValueLens.Value this).IsValid
    override this.ToString() = (ValueLens.Value this).ToString()

// Content: Wraps LongString (larger length limit than ShortString)
type Content =
    private
    | Content of LongString

    static member Value_ = (fun (Content s) -> s), (fun (s: LongString) _ -> Content s)

    member this.IsValid = (ValueLens.Value this).IsValid
    override this.ToString() = (ValueLens.Value this).ToString()

// -----------------------------------------------------------------------------
// AGGREGATE ROOT ENTITY
// -----------------------------------------------------------------------------
// Document is the main entity (aggregate root) in this bounded context.
// It composes multiple value objects and provides a factory method.
// -----------------------------------------------------------------------------
type Document = {
    Id: DocumentId
    Title: Title
    Content: Content
} with
    // Factory method using F# computation expression for Result
    // EXERCISE: Create a Document by:
    //   1. Create DocumentId from the guid using ValueLens.Create
    //   2. Create Title from string using ValueLens.CreateAsResult (use let! for Result binding)
    //   3. Create Content from string using ValueLens.CreateAsResult
    //   4. Return the Document record
    static member Create(docId: Guid, title: string, content: string) =
        result {
            return Hole?TODO_CreateValidatedDocument
        }
     member this.IsValid =
        this.Id.IsValid && this.Title.IsValid && this.Content.IsValid
    override this.ToString() = this.IsValid.ToString()


// -----------------------------------------------------------------------------
// COMMANDS AND EVENTS
// -----------------------------------------------------------------------------
// Commands: Intentions to change state (what the user wants to do)
// Events: Facts that happened (immutable history of what occurred)
//
// In Event Sourcing, commands produce events, and events are the source of truth.
// -----------------------------------------------------------------------------
module Document =
    // Commands represent user intentions
    type Command =
        | CreateOrUpdate of Document

    // Domain errors (business rule violations)
    type Error =
        | DocumentNotFound

    // Events represent facts that happened
    type Event =
        | CreatedOrUpdated of Document
        | Error of Error
