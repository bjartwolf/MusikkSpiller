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
open Microsoft.IO
open MusicPlayerCore

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

                async function fetchNext(url) {{
                   const response = await fetch (url);
                   const result = await response.json();
                   chartCtx.clearRect(0, 0, chart.width, chart.height);
                   chartCtx.beginPath();
                   let i = 0;
                   for (const value of result.values) {{
                        i = i + 1; 
                //        console.log(i,value)
                        chartCtx.lineTo(i,(value+2.0)*100); 
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
type SimulationResult =
    {
        values: double array 
        nextResult: Uri
    }
let solver = Player.sol_time_invariant
type stateVector = float * float * float * float * float * float  // floats are double in F#
let simulationHandler ((s1,s2,s3,s4,s5,s6): stateVector) =
    let x: Vector<double>= vector [s1;s2;s3;s4;s5;s6] // need to find some nice ways to map strings with numbers in them to a statevector
    let nextValues = solver x |> Seq.take 100 |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%f/%f/%f/%f/%f/%f" nextState.[0] nextState.[1] nextState.[2] nextState.[3] nextState.[4] nextState.[5] 
    let model = {  values = (nextValues |> Array.map (fun x -> x.[2])); nextResult= new Uri(sprintf "/simulationstring/%s" serializedState, UriKind.Relative)}
    json model
    
let initSimulation () =
    let nextValues = solver Player.x0 |> Seq.take 100 |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%f/%f/%f/%f/%f/%f" nextState.[0] nextState.[1] nextState.[2] nextState.[3] nextState.[4] nextState.[5] 
    let model = { values = (nextValues |> Array.map (fun x -> x.[2])); nextResult= new Uri(sprintf "/simulationstring/%s" serializedState, UriKind.Relative)}
    json model
    
let simulationHarmonic (x, q)  =
    let nextValues = HarmonicOscillator.solve (vector [x;q]) |> Seq.take 10000 |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%f/%f" nextState.[0] nextState.[1]
    let model = { values = (nextValues |> Array.map (fun x -> x.[1])); nextResult= new Uri(sprintf "/simulation/harmonic/%s" serializedState, UriKind.Relative)}
    json model
let initHarmonicSimulator () =
    let x_0 = (vector [1.0;0.0]) 
    let nextValues = HarmonicOscillator.solve x_0 |> Seq.take 10000 |> Seq.toArray
    let nextState = nextValues |> Array.last
    let serializedState = sprintf "%f/%f" nextState.[0] nextState.[1]
    let model = { values = (nextValues |> Array.map (fun x -> x.[1])); nextResult= new Uri(sprintf "/simulation/harmonic/%s" serializedState, UriKind.Relative)}
    json model

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler ("/initstring/")
                route "/harmonic" >=> indexHandler ("/harmonicinit")
                route "/harmonicinit" >=> initHarmonicSimulator () 
                routef "/simulation/harmonic/%f/%f" simulationHarmonic 
                route "/initstring/" >=> initSimulation () 
                routef "/simulationstring/%f/%f/%f/%f/%f/%f" simulationHandler 
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
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

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