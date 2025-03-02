module SimulationApi.App

open System
open System.IO
open MathNet.Numerics.LinearAlgebra
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "SimulationApi" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial (initialUrl: string) =
            [
            h1 [] [ encodedText "SimulationApi" ]
            canvas [_id "chart"; _width "1500"; _height "1500"] []
            script [_type "application/javascript"] [
                rawText $"""
                const chart = document.getElementById("chart");
                const chartCtx = chart.getContext("2d");
                chartCtx.strokeStyle = 'blue';
                chartCtx.lineWidth = 2;
                const width = chart.width;
                console.log("Chart with: " + width);

                async function fetchNext(url) {{
                   const response = await fetch (url);
                   const result = await response.json();
                   chartCtx.clearRect(0, 0, chart.width, chart.height);
                   chartCtx.beginPath();
                   let i = 0;
//                   let nrOfResults = result.values.length;
//                   for (const value of result.values) {{
                   for (const value of result.twodvalues) {{
                        i = i + 1;
//                        chartCtx.lineTo(i*width/nrOfResults,(value+2.0)*100); 
                          chartCtx.lineTo((value[0]+1)*300, (value[1]+1)*300); 
                   }}
                   chartCtx.stroke();
                   fetchNext(result.nextResult);
                }}
                 async function init() {{
                   const response = await fetch("%s{initialUrl}");
                   const result = await response.json();
                   console.log(result.values);
                   fetchNext(result.nextResult)
                }}
                window.onload = function () {{
                    init(); 
                }}
                """
        ]
        ]

    let index (initialUrl: string) = partial(initialUrl) |> layout

let indexHandler (initialUrl: string) =
    let view = Views.index (initialUrl)
    htmlView view

// do we need to return time... time is annoying
// maybe just ignore time and return the next values and the delta T
// delta T can for now be implicit
// we do not return the entire state vector, just the measured values
// but we do yield the latest state vector implicitly in the URL
type SimulationResult2D =
    {
        twodvalues: double array array
        nextResult: Uri
    }
   
let n = 100
let simulationHarmonic (x, q)  =
    publicResponseCaching 10 None >=> 
    let nextValues = Simulations.HarmonicOscillator.solve (vector [x;q]) |> Seq.take n |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%.3f/%.3f" nextState.[0] nextState.[1]
    let model = { twodvalues = nextValues |> Array.map Vector.toArray |> Array.skip 1; nextResult= new Uri(sprintf "/simulation/harmonic/%s" serializedState, UriKind.Relative)}
    json model 
let initHarmonicSimulator () =
    let x_0 = (vector [1.0;0.0]) 
    let nextValues = Simulations.HarmonicOscillator.solve x_0 |> Seq.take n |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%f/%f" nextState.[0] nextState.[1]
    let model = { twodvalues = nextValues |> Array.map Vector.toArray |> Array.skip 1; nextResult= new Uri(sprintf "/simulation/harmonic/%s" serializedState, UriKind.Relative)}
    json model

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler ("/harmonicinit/")
                route "/harmonic" >=> indexHandler ("/harmonicinit")
                route "/harmonicinit" >=> initHarmonicSimulator () 
                routef "/simulation/harmonic/%f/%f" simulationHarmonic 
            ]
        setStatusCode 404 >=> text "Not Found" ]


let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddResponseCaching() 
            .AddCors()    
            .AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0