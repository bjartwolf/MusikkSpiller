module HarmonicOscillator

open MathNet.Numerics.LinearAlgebra

let forward_euler (h: double) (f: Vector<double> -> Vector<double>) x =
    x + h*(f x) 
// statevector is p q
// p_dot = - w * q
// q_dot = w * p
let w = 1.0
let A = matrix [[ 0.0; -w ];[w; 0.0]]

let ode (x: Vector<double>) = A * x
let h = 0.001
let solve = Seq.unfold (fun xu -> Some(xu, (forward_euler h) ode xu)) 
