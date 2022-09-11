namespace MusicApi.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open System.IO

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
