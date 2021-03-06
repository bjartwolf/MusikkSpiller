﻿module WorkingProgram

open System
open System.Collections.Generic
open System.IO
open System.Linq

let generateStream() =
    let frequency = 440us
    let msDuration = 1000
    let volume = 16300us    
    let mStrm = new MemoryStream();
    let writer = new BinaryWriter(mStrm);
    let TAU :double= 2.0 * Math.PI
    let formatChunkSize = 16
    let headerSize = 8
    let formatType= 1s
    let tracks = 1s
    let samplesPerSecond = 44100
    let bitsPerSample = 16s
    let frameSize = tracks * ((bitsPerSample + 7s) / 8s)
    let bytesPerSecond = samplesPerSecond * (int)frameSize
    let waveSize = 4;
    let samples = (int)(samplesPerSecond * msDuration / 1000)//wat
    let dataChunkSize = samples * (int)frameSize;
    let fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
    writer.Write(0x46464952); // = encoding.GetBytes("RIFF")
    writer.Write(fileSize);
    writer.Write(0x45564157); // = encoding.GetBytes("WAVE")
    writer.Write(0x20746D66); // = encoding.GetBytes("fmt ")
    writer.Write(formatChunkSize);
    writer.Write(formatType);
    writer.Write(tracks);
    writer.Write(samplesPerSecond);
    writer.Write(bytesPerSecond);
    writer.Write(frameSize);
    writer.Write(bitsPerSample);
    writer.Write(0x61746164); // = encoding.GetBytes("data")
    writer.Write(dataChunkSize);
    let theta :double= (double)frequency * TAU / (double)samplesPerSecond;
    // 'volume' is UInt16 with range 0 thru Uint16.MaxValue ( = 65 535)
    // we need 'amp' to have the range of 0 thru Int16.MaxValue ( = 32 767)
    let amp:double = (double)(volume >>> 2) // so we simply set amp = volume / 2
//    for step = 0 to samples do
//        let s = (uint16)(amp * Math.Sin(theta * (double)step))
//        let s1 = (uint16)(amp *2.0* Math.Cos(3.0 + theta * (double)step*2.0))
//        writer.Write(s+s1)
    mStrm.Seek(0L, SeekOrigin.Begin) |> ignore
    mStrm 
// should probably use a list for the bytes or something
let strmAsSeq (stream: Stream): byte seq = 
    Diagnostics.Debug.Assert(stream.Length < (int64)Int32.MaxValue)
    let len = (int)stream.Length
    let arr:byte[] = Array.zeroCreate len 
    let bytesRead = stream.Read(arr,0,len) 
    if not (bytesRead = len) then failwith "Could not read everything"
    arr |> Array.toSeq

let rec sound t = seq {
    let volume = 16300us    
    let samplesPerSecond = 44100
    let amp:double = (double)(volume >>> 2) // so we simply set amp = volume / 2
    let frequency = 440us
    let TAU :double= 2.0 * Math.PI
    let theta :double= (double)frequency * TAU / (double)samplesPerSecond;
    let volume = 16300us    
    yield! BitConverter.GetBytes((uint16)(amp * Math.Sin(theta * (double)t)))
    yield! sound ((t+1) % 100000)
} 

type WaveStreamOrig() =
   inherit Stream()
   let firstData = generateStream() |> strmAsSeq
   let sounddata = (sound 0)  
   let data = Seq.append firstData sounddata 
   let mutable pos = 0
   override this.CanRead with get () = true 
   override this.CanSeek with get () = false 
   override this.CanWrite with get () = false 
   override this.Read(buffer: byte[], offset:int, count: int) =
            let copyTo (dst:Array) (src: Array) = src.CopyTo(dst,offset)
            let arr = data |> Seq.skip pos |> Seq.take count |> Seq.toArray
            arr |> copyTo buffer 
            pos <- pos + count
            if (pos > 200000) then 0 else count
   override this.Seek(offset:int64, origin: SeekOrigin):int64 = failwith "no seek"
   override this.SetLength(value: int64) = failwith "no set length"
   override this.Write(buffer: byte[], offset:int, count:int) = failwith "no write"
   override this.Length with get () = failwith "no length I am infinite" 
   override this.Position with get () = failwith "no position" 
                          and set (value) = failwith "no set pos" 
   override this.Flush() = ()