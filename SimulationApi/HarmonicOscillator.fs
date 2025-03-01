module HarmonicOscillator

open MathNet.Numerics.LinearAlgebra
let rk4 h (f: Vector<double> -> Vector<double>) x =
  let k1:Vector<double> = h * f(x)
  let k2 = h * f(x + 0.5*k1)
  let k3 = h * f(x + 0.5*k2)
  let k4 = h * f(x + k3)
  x + (k1 / 6.0) + (k2 / 3.0) + (k3 / 3.0) + (k4 / 6.0)

let forward_euler (h: double) (f: Vector<double> -> Vector<double>) x =
    x + h*(f x) 
// statevector is p q
// p_dot = - w * q
// q_dot = w * p
let w = 1.0
let A = matrix [[ 0.0; -w ];[w; 0.0]]

let ode (x: Vector<double>) = A * x
let h = 0.0001
//let solve = Seq.unfold (fun xu -> Some(xu, (forward_euler h) ode xu)) 
let solve = Seq.unfold (fun xu -> Some(xu, (rk4 h) ode xu)) 
