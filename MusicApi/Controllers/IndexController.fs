namespace MusicApi.Controllers

open NAudio.Wave
open WaveFormat
open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open MusicPlayerCore.Player 
open System.IO
open System.Text
open Microsoft.Net.Http.Headers
open Microsoft.Extensions.Primitives

[<ApiController>]
[<Route("/")>]
type IndexController (logger : ILogger<IndexController>) =
    inherit ControllerBase()

    [<HttpGet>]
    member __.Get() = 
        let content = ContentResult()
        content.Content <- File.ReadAllText("./html/index.html") 
        content.ContentType <- "text/html"
        content
