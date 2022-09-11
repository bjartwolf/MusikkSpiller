namespace MusicApi.Controllers

open NAudio.Wave
open WaveFormat
open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open MusicPlayerCore.Player 
open System.IO
open System.Text
open Microsoft.Net.Http.Headers
open Microsoft.Extensions.Primitives

[<ApiController>]
[<Route("wave.wav")>]
type WaveController (logger : ILogger<WaveController>) =
    inherit ControllerBase()

    [<HttpGet>]
    member __.Get() = async {
        let samplesPerSecond = 44100
        let duration = 10*1000
        let msDuration = duration*1000
        let bitsPerSample = 16s
        let tracks = 1s

        let ws = new SoundsStream(MusicPlayerCore.Player.guitarSol, tracks, bitsPerSample, samplesPerSecond, msDuration )
        let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))

        __.Response.StatusCode <- 200

        __.Response.Headers.Add( HeaderNames.ContentType, StringValues( "audio/wave" ) )

        let outputStream = __.Response.Body
        let bufferSize = 1 <<< 16 
        let buffer = Array.zeroCreate<byte> bufferSize
        let mutable loop = true
        while loop do
            let! bytesRead = reader.ReadAsync( buffer, 0, bufferSize ) |> Async.AwaitTask
            match bytesRead with
            | 0 -> loop <- false
            | _ -> do! outputStream.WriteAsync( buffer, 0, bytesRead ) |> Async.AwaitTask
        do! outputStream.FlushAsync() |> Async.AwaitTask
        return EmptyResult()

    }
