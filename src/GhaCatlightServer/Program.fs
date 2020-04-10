module GhaCatlightServer.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.ContextInsensitive
open Newtonsoft.Json
open Microsoft.FSharpLu.Json
open Giraffe.Serialization

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Models
// ---------------------------------

type CatlightProtocol = string

type ServerId = string

type ServerName = string

type ServerVersion = string

type CurrentUser = Unknown

type Space = Unknown

type BasicServerPayload =
    { protocol: CatlightProtocol
      id: ServerId
      webUrl: Option<Uri>
      name: ServerName
      serverVersion: Option<ServerVersion>
      currentUser: Option<CurrentUser>
      spaces: Space list }


// ---------------------------------
// Web app
// ---------------------------------

let basicCatlightHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let data: BasicServerPayload =
            { protocol = "catlight.io/protocol/v1.0/basic"
              id = "SERVER_ID"
              webUrl = Some(Uri("http://www.perdu.com"))
              name = "Server Name"
              serverVersion = None
              currentUser = None
              spaces = [] }
        task {
            // Do stuff
            return! ctx.WriteJsonAsync data }


let webApp =
    choose
        [ GET >=> choose
                      [ route "/" >=> text "Nothing to see here"
                        route "/basic" >=> basicCatlightHandler ]
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
    // Now customize only the IJsonSerializer by providing a custom
    // object of JsonSerializerSettings
    let customSettings = JsonSerializerSettings()
    customSettings.NullValueHandling <- NullValueHandling.Ignore
    customSettings.Converters.Add(CompactUnionJsonConverter(true))

    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(customSettings)) |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder().UseKestrel().Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices).ConfigureLogging(configureLogging).Build().Run()
    0
