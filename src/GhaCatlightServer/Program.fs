module GhaCatlightServer.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Hosting

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message


// ---------------------------------
// Web app
// ---------------------------------

let webApp =
    choose [ 
        GET >=> 
            choose [ 
                route "/" >=> text "Nothing to see here" 
            ]
        RequestErrors.notFound (text "Not Found") ]

// ---------------------------------
// Config and Main
// ---------------------------------


let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostEnvironment>()
    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler errorHandler).UseGiraffe(webApp)

let configureServices (services: IServiceCollection) = 
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices).ConfigureLogging(configureLogging).Build().Run()
    0
