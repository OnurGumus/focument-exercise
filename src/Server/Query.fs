// =============================================================================
// QUERY MODULE (Read Side of CQRS)
// =============================================================================
// This module provides read-only queries against the projected read model.
// These queries are optimized for reading - they hit the SQLite database
// that was built by the Projection module, NOT the event store.
//
// Benefits of this separation:
//   1. Queries can be optimized independently of writes
//   2. No contention between reads and writes
//   3. Can use different data models for reading vs writing
//   4. Easy to rebuild read models by replaying events
//
// EXERCISE: Implement the SQL queries using Dapper
// =============================================================================

module ServerQuery

open Microsoft.Data.Sqlite
open Dapper
open Hole

// Gets the last processed event offset (used for projection resumption)
// EXERCISE: Query the Offsets table for the 'DocumentProjection' row
// Hint: Use conn.QueryFirstOrDefault<int64>(sql, parameters)
let getLastOffset (connString: string) : int64 =
    use conn = new SqliteConnection(connString)
    conn.Open()
    Hole?TODO_QueryLastOffset

// Returns all documents, ordered by most recently updated
// EXERCISE: Query all documents from the Documents table
// Hint: select Id, Title, Body, Version, CreatedAt, UpdatedAt from Documents order by UpdatedAt desc
let getDocuments (connString: string) : Query.Document list =
    use conn = new SqliteConnection(connString)
    conn.Open()
    Hole?TODO_QueryAllDocuments

// Returns version history for a specific document (enables time-travel queries)
// EXERCISE: Query DocumentVersions table filtered by document Id
// Hint: Use anonymous record {| Id = docId |} for parameters
let getDocumentHistory (connString: string) (docId: string) : Query.DocumentVersion list =
    use conn = new SqliteConnection(connString)
    conn.Open()
    Hole?TODO_QueryDocumentHistory
