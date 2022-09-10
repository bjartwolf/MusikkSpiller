namespace MusicApi.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open MusicPlayerCore.Player 

[<ApiController>]
[<Route("[controller]")>]
type WaveController (logger : ILogger<WaveController>) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() =
        "foo"
