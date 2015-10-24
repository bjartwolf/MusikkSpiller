﻿open System
open System.Collections.Generic
open System.IO
open System.Linq
open NAudio.Wave

let samplesPerSecond = 44100
let duration = 10*1000
let msDuration = duration*1000
let bitsPerSample = 16s
let tracks = 1s

let getHeaders () =
    let formatChunkSize = 16
    let headerSize = 8
    let formatType= 1s
    let frameSize = tracks * ((bitsPerSample + 7s) / 8s)
    let bytesPerSecond = samplesPerSecond * (int)frameSize
    let waveSize = 4;
    let samples = (int)(samplesPerSecond * msDuration / 1000)
    let dataChunkSize = samples * (int)frameSize;
    let fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
    seq {
        yield [|'R'B ; 'I'B; 'F'B;'F'B |]
        yield (BitConverter.GetBytes(fileSize))
        yield [|'W'B ; 'A'B; 'V'B;'E'B |]
        yield [|'f'B ; 'm'B; 't'B; ' 'B|] 
        yield (BitConverter.GetBytes(formatChunkSize))
        yield (BitConverter.GetBytes(formatType))
        yield (BitConverter.GetBytes(tracks))
        yield (BitConverter.GetBytes(samplesPerSecond))
        yield (BitConverter.GetBytes(bytesPerSecond))
        yield (BitConverter.GetBytes(frameSize))
        yield (BitConverter.GetBytes(bitsPerSample))
        yield [|'d'B ; 'a'B; 't'B;'a'B |]
        yield BitConverter.GetBytes(dataChunkSize)
     } |> Array.concat

let volume = 16300us
let amp:double = (double)(volume >>> 2) // so we simply set amp = volume / 2
let frequency = 440us
let tau :double= 2.0 * Math.PI
let theta :double= (double)frequency * tau / (double)samplesPerSecond;
let rec sound t = seq {
    // 'volume' is UInt16 with range 0 thru Uint16.MaxValue ( = 65 535)
    // we need 'amp' to have the range of 0 thru Int16.MaxValue ( = 32 767)
    yield! BitConverter.GetBytes((uint16)(amp * Math.Sin(theta * (double)t)))
    if (t < samplesPerSecond*duration) then yield! sound ((t+1))
} 

let takeSkip (s: seq<byte>) (n: int) : (byte[] * seq<byte>) = 
    let takenValues = s |> Seq.truncate n |> Seq.toArray
    let s' = s |> Seq.skip takenValues.Length//try 
    (takenValues, s')

type WaveStream() =
   inherit Stream()
   let sounddata = sound 0
   let mutable data = Seq.append (getHeaders()) sounddata 
   override this.CanRead with get () = true 
   override this.CanSeek with get () = false 
   override this.CanWrite with get () = false 
   override this.Read(buffer: byte[], offset:int, count: int) =
            let copyTo (dst:Array) (src: Array) = src.CopyTo(dst,offset)
            let (bytes,data') = takeSkip data count 
            data <- data'
            bytes |> copyTo buffer 
            bytes.Length
   override this.Seek(offset:int64, origin: SeekOrigin):int64 = failwith "no seek"
   override this.SetLength(value: int64) = failwith "no set length"
   override this.Write(buffer: byte[], offset:int, count:int) = failwith "no write"
   override this.Length with get () = failwith "no length I am infinite" 
   override this.Position with get () = failwith "no position" 
                          and set (value) = failwith "no set pos" 
   override this.Flush() = ()

[<EntryPoint>]
let main argv = 

    let ws = new WaveStream()
    let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))
    let wavePlayer = new DirectSoundOut(latency=2000);
    wavePlayer.Init(reader);
    wavePlayer.Play()
    Console.ReadKey() |> ignore
    0 
