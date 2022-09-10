module Tests

open System
open Xunit
open NAudio.Wave
open WaveFormat
open System.IO 

[<Fact>]
let ``Stream can initialize as wave`` () =
    let samplesPerSecond = 44100
    let duration = 10*1000
    let msDuration = duration*1000
    let bitsPerSample = 16s
    let tracks = 1s

    let ws = new SoundsStream(MusicPlayerCore.Player.guitarSol, tracks, bitsPerSample, samplesPerSecond, msDuration )
    let sr = new StreamReader(ws) 
    let array = Array.zeroCreate 3 
    sr.Read(array, 0,3) |> ignore
    let expected = "RIF" 
    
    Assert.Equal(expected, array |> Array.toList)
