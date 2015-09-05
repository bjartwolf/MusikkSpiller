// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.Collections.Generic
open System.IO
open System.Linq
type WaveStream() =
    inherit Stream()
    override this.CanRead with get () = true 
    override this.CanSeek with get () = false 
    override this.CanWrite with get () = false 
    override this.Length with get () = failwith "no length I am infinite" 
    override this.Position with get () = failwith "no position" 
                           and set (value) = failwith "no set pos" 
    override this.Flush() = ()
    override this.Read(buffer: byte[], offset:int, count: int) = failwith "no read yet"
    override this.Seek(offset:int64, origin: SeekOrigin):int64 = failwith "no seek"
    override this.SetLength(value: int64) = failwith "no set length"
    override this.Write(buffer: byte[], offset:int, count:int) = failwith "no write"

//16383
let PlayBeep (frequency:UInt16) (msDuration:int) (volume :UInt16 )= 
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
    let samples = (int)((decimal)(samplesPerSecond * msDuration) / 1000m)//wat
    let dataChunkSize = samples * (int)frameSize;
    let fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
    // var encoding = new System.Text.UTF8Encoding();
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
    for step = 0 to samples do
        let s = (uint16)(amp * Math.Sin(theta * (double)step))
        writer.Write(s)
    mStrm.Seek(0L, SeekOrigin.Begin) |> ignore
    (new System.Media.SoundPlayer(mStrm)).Play()
    writer.Close();
    mStrm.Close();
[<EntryPoint>]
let main argv = 
    PlayBeep 440us 2000 16300us 
    printfn "%A" argv
    Console.ReadKey() |> ignore
    0 // return an integer exit code
