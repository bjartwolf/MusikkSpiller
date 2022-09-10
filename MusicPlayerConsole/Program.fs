open NAudio.Wave
open MusicPlayerCore.Player 
open System

[<EntryPoint>]
let main argv = 
    let ws = new WaveStream()
    let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))
    let wavePlayer = new DirectSoundOut(latency=2000);
    wavePlayer.Init(reader);
    wavePlayer.Play()
    Console.ReadKey() |> ignore
    0 
