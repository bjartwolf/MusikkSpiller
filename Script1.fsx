#r @"packages\MathNet.Numerics.3.8.0\lib\net40\MathNet.Numerics.dll"
#r @"packages\MathNet.Numerics.FSharp.3.8.0\lib\net40\MathNet.Numerics.FSharp.dll"
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

let x0: Vector<double>= vector [1.0; 2.0; 2.0;0.0;0.0;0.0]
let rho = 1.0
let w = 5.84
let b (t:double): Vector<double> = 0.0 * rho * vector [Math.Sin (w*t) ;
                                                      Math.Sqrt(2.0) * Math.Sin(w*t);
                                                      Math.Sin(w*t);
                                                      0.0;
                                                      0.0;
                                                      0.0]
let L = 10.0
let T = 100.0
let deltaX = L/4.0
let alpha:double = (T/rho)/(deltaX ** 2.0)
let m : Matrix<double> = matrix [[  0.0;  0.0;  0.0; 1.0; 0.0; 0.0 ]
                                 [  0.0;  0.0;  0.0; 0.0; 1.0; 0.0 ]
                                 [  0.0;  0.0;  0.0; 0.0; 0.0; 1.0 ]
                                 [  -2.0*alpha; 1.0*alpha;  0.0; 0.0; 0.0; 0.0 ]
                                 [ 1.0*alpha;  -2.0*alpha; 1.0*alpha; 0.0; 0.0; 0.0 ]
                                 [  0.0; 1.0*alpha; -2.0*alpha; 0.0; 0.0; 0.0 ]]
let sol = Seq.unfold (fun xu -> Some(xu, rk4 0.0014 (fun (t,x) -> m * x + (b t)) xu)) (0.0, x0) 
//        |> Seq.take 100 
        |> Seq.map snd
//        |> Seq.toArray
        |> Seq.map (fun x -> x.[1]) 

for value in sol do
    printf "%A 
        " value 