// =============================================================================
// TYPED HOLES (Idris-style)
// =============================================================================
// This module provides typed holes for workshop exercises.
// When you see `Hole?TODO_Name`, you need to fill in the implementation.
//
// Benefits:
//   - Compile-time warning tells you what holes remain
//   - Runtime exception shows the expected type if you run incomplete code
//   - The type system guides you to the correct implementation
//
// Example:
//   let add x y = Hole?TODO_Addition    // Warning: Incomplete hole
//   add 1 2                              // Throws: Incomplete hole 'TODO_Addition : System.Int32'
// =============================================================================

module Hole

open System

/// Marker type for typed holes
type Hole = Hole

/// Creates a typed hole that shows a compile-time warning and throws at runtime.
/// Usage: Hole?TODO_DescriptiveName
[<CompilerMessage("Incomplete hole - implement this!", 130)>]
let (?) (_ : Hole) (id : string) : 'T =
    sprintf "Incomplete hole '%s : %O'" id typeof<'T>
    |> NotImplementedException
    |> raise
