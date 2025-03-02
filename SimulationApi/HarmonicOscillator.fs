namespace Simulations
module Solvers = 
    open MathNet.Numerics.OdeSolvers
    open MathNet.Numerics.LinearAlgebra
    // https://en.wikipedia.org/wiki/Runge%E2%80%93Kutta_methods
    let rk4 (h:float) (f: Vector<float> -> Vector<float>) x =
      let k1 = h * f x
      let k2 = h * f (x + 0.5*k1)
      let k3 = h * f (x + 0.5*k2)
      let k4 = h * f (x + k3)
      x + (k1 + 2.0*k2 + 2.0*k3 + k4)/6.0

    let forward_euler (h: double) (f: Vector<float> -> Vector<float>) x =
        x + h*(f x)
        
module HarmonicOscillator =
    open MathNet.Numerics.LinearAlgebra
    open Solvers
    // statevector is p q
    // p_dot = - w * q
    // q_dot = w * p
    let w = 1.0
    let A = matrix [[ 0.0; -w ];[w; 0.0]]
    let f x = A * x
    let h = 0.005
    let solve = Seq.unfold (fun xu -> Some(xu, (forward_euler h) f xu)) 
