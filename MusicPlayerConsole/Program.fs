open NAudio.Wave
open WaveFormat
open System

[<EntryPoint>]
let main argv = 

    let samplesPerSecond = 44100
    let duration = 10*1000
    let msDuration = duration*1000
    let bitsPerSample = 16s
    let tracks = 1s

    let ws = new SoundsStream(MusicPlayerCore.Player.guitarSol, tracks, bitsPerSample, samplesPerSecond, msDuration )
    let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))
    let wavePlayer = new DirectSoundOut(latency=2000);
    wavePlayer.Init(reader);
    wavePlayer.Play()
    Console.ReadKey() |> ignore
    0 
