module WaveFormat 
    open System
    open System.IO

    let getHeaders tracks bitsPerSample samplesPerSecond msDuration =
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

    let takeSkip (s: seq<byte>) (n: int) : (byte[] * seq<byte>) = 
        let takenValues = s |> Seq.truncate n |> Seq.toArray
        let s' = s |> Seq.skip takenValues.Length//try 
        (takenValues, s')

    type SoundsStream (sound: seq<byte>, tracks, bitsPerSample, samplesPerSecond, msDuration) =
       inherit Stream()
       let sounddata = sound 
       let mutable data = Seq.append (getHeaders tracks bitsPerSample samplesPerSecond msDuration) sounddata 
       override this.CanRead with get () = true 
       override this.CanSeek with get () = false 
       override this.CanWrite with get () = false 
       override this.Read(buffer: byte[], offset:int, count: int) =
                let (bytes,data') = takeSkip data count 
                data <- data'
                bytes.CopyTo(buffer,offset )
                bytes.Length
       override this.Seek(offset:int64, origin: SeekOrigin):int64 = failwith "no seek"
       override this.SetLength(value: int64) = failwith "no set length"
       override this.Write(buffer: byte[], offset:int, count:int) = failwith "no write"
       override this.Length with get () = failwith "no length I am infinite" 
       override this.Position with get () = failwith "no position" 
                              and set (value) = failwith "no set pos" 
       override this.Flush() = ()

