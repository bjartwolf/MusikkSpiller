module SolverTests
open System
open Xunit
open MusicPlayerCore.Player

[<Fact>]
let ``Read a byte`` () =
    let firstPart = guitarSol |> Seq.take 1 
    Assert.Equal([| 235uy |], firstPart)

