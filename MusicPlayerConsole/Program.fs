open System
open System.Collections.Generic
open System.IO
open System.Linq
open NAudio.Wave
//open FSharp.Charting

let samplesPerSecond = 44100
let duration = 10*1000
let msDuration = duration*1000
let bitsPerSample = 16s
let tracks = 1s

open MathNet.Numerics.LinearAlgebra
open MathNet
open System
open MathNet.Numerics

let rk4 h (f: double * Vector<double> -> Vector<double>) (t, x) =
  let k1:Vector<double> = h * f(t, x)
  let k2 = h * f(t + 0.5*h, x + 0.5*k1)
  let k3 = h * f(t + 0.5*h, x + 0.5*k2)
  let k4 = h * f(t + h, x + k3)
  (t + h, x + (k1 / 6.0) + (k2 / 3.0) + (k3 / 3.0) + (k4 / 6.0))

//http://www4.ncsu.edu/eos/users/w/white/www/white/ma302/less1108.pdf

let x0: Vector<double>= vector [1.0; 2.0; 1.0;0.0;0.0;0.0]
let rho = 1.0
//let w = 5.84
let w = 9.6812 
let b (t:double): Vector<double> = rho * vector [Math.Sin (w*t) ;
                                                 Math.Sqrt(2.0) * Math.Sin(w*t);
                                                 Math.Sin(w*t);
                                                 0.0;
                                                 0.0;
                                                 0.0]
let L = 2.0
let T = 10.0
let deltaX = L/4.0
let alpha:double = (T/rho)/(deltaX ** 2.0)
let m : Matrix<double> = matrix [[  0.0;  0.0;  0.0; 1.0; 0.0; 0.0 ]
                                 [  0.0;  0.0;  0.0; 0.0; 1.0; 0.0 ]
                                 [  0.0;  0.0;  0.0; 0.0; 0.0; 1.0 ]
                                 [  -2.0*alpha; 1.0*alpha;  0.0; 0.0; 0.0; 0.0 ]
                                 [ 1.0*alpha;  -2.0*alpha; 1.0*alpha; 0.0; 0.0; 0.0 ]
                                 [  0.0; 1.0*alpha; -2.0*alpha; 0.0; 0.0; 0.0 ]]
let ode (t, x) = m * x + b t
let t0 = 0.0
let h = 0.01
let sol = Seq.unfold (fun xu -> Some(xu, rk4 h ode xu)) (t0, x0) 
        |> Seq.map ( fun (t,x) -> (t, x.[2])) 
//        |> Seq.map (snd >> fun x -> x.[2]) 

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

    // 'volume' is UInt16 with range 0 thru Uint16.MaxValue ( = 65 535)
    // we need 'amp' to have the range of 0 thru Int16.MaxValue ( = 32 767)
let rec sound t = seq {
    let volume = 16300us
    let amp:double = (double)(volume >>> 2) // so we simply set amp = volume / 2
    let frequency = 440us
    let tau :double= 2.0 * Math.PI
    let theta :double= (double)frequency * tau / (double)samplesPerSecond;
    yield ((double)t/4.0, Math.Sin(theta * (double)t ))
    yield! sound (t+1)
} 

let amp (value: double) =
    let volume = 16300us
    let amp:double = (double)(volume >>> 2) 
    (uint16)(value * amp)

let guitarSol = sol |> Seq.map (snd >> amp >> BitConverter.GetBytes) |> Seq.collect id

let takeSkip (s: seq<byte>) (n: int) : (byte[] * seq<byte>) = 
    let takenValues = s |> Seq.truncate n |> Seq.toArray
    let s' = s |> Seq.skip takenValues.Length//try 
    (takenValues, s')

type WaveStream() =
   inherit Stream()
//   let sounddata = sound 0 |> Seq.map (amp >> BitConverter.GetBytes) |> Seq.collect id
   let sounddata = guitarSol 
   let mutable data = Seq.append (getHeaders()) sounddata 
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

[<EntryPoint>]
let main argv = 
//    Control.UseManaged()
    let ws = new WaveStream()
//    Chart.Combine( 
//        [ Chart.Line(sol|> Seq.take 5000, "rk4")
//          Chart.Line (sound 0|> Seq.take 5000, "sine")]) |> Chart.Show
    let samples = sol |> Seq.map (fun (x,y) -> new complex(x,y)) |> Seq.take 2000 |> Seq.toArray
 //   MathNet.Numerics.IntegralTransforms.Fourier.BluesteinForward(samples, Numerics.IntegralTransforms.FourierOptions.Default)
//    Chart.Point(samples  |> Array.map (fun x -> (x.Imaginary, x.Real)) |> Array.toList) |> Chart.Show 
    let buffer = new BufferedStream(ws)
//    Console.WriteLine(Control.LinearAlgebraProvider);
    let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))
    let wavePlayer = new DirectSoundOut(latency=2000);
    wavePlayer.Init(reader);
    wavePlayer.Play()
    Console.ReadKey() |> ignore
    0 
