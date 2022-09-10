open System
open System.IO
open NAudio.Wave

let samplesPerSecond = 44100
let duration = 10*1000
let msDuration = duration*1000
let bitsPerSample = 16s
let tracks = 1s

open MathNet.Numerics.LinearAlgebra

let rk4 h (f: double * Vector<double> -> Vector<double>) (t, x) =
  let k1:Vector<double> = h * f(t, x)
  let k2 = h * f(t + 0.5*h, x + 0.5*k1)
  let k3 = h * f(t + 0.5*h, x + 0.5*k2)
  let k4 = h * f(t + h, x + k3)
  (t + h, x + (k1 / 6.0) + (k2 / 3.0) + (k3 / 3.0) + (k4 / 6.0))

//old link is broken
//http://www4.ncsu.edu/eos/users/w/white/www/white/ma302/less1108.pdf
// Perhaps this is the new linke
// https://mmedvin.math.ncsu.edu/Teaching/MA302/A7_ma302.pdf

// Or this?
// https://www.youtube.com/watch?v=ndt-qwlCSLg
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
    let ws = new WaveStream()
    let reader = new NAudio.Wave.RawSourceWaveStream(ws, new WaveFormat(samplesPerSecond,(int)bitsPerSample,(int)tracks))
    let wavePlayer = new DirectSoundOut(latency=2000);
    wavePlayer.Init(reader);
    wavePlayer.Play()
    Console.ReadKey() |> ignore
    0 
