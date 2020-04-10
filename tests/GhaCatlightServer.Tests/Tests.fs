module Tests

open System
open System.IO
open System.Net
open System.Net.Http
open Xunit
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json
open GhaCatlightServer.App

// ---------------------------------
// Helper functions (extend as you need)
// ---------------------------------

let createHost() =
    WebHostBuilder().UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> GhaCatlightServer.App.configureApp)
        .ConfigureServices(Action<IServiceCollection> GhaCatlightServer.App.configureServices)

let runTask task =
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let httpGet (path: string) (client: HttpClient) =
    path
    |> client.GetAsync
    |> runTask

let isStatus (code: HttpStatusCode) (response: HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let ensureSuccess (response: HttpResponseMessage) =
    if not response.IsSuccessStatusCode then
        response.Content.ReadAsStringAsync()
        |> runTask
        |> failwithf "%A"
    else
        response

let readText (response: HttpResponseMessage) = response.Content.ReadAsStringAsync() |> runTask

let jsonStringCleanUp (s: string) = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(s), Formatting.Indented)


let shouldEqual expected actual = Assert.Equal(expected, actual)

let shouldEqualJson expected actual = Assert.Equal(expected |> jsonStringCleanUp, actual |> jsonStringCleanUp)

let shouldContain (expected: string) (actual: string) = Assert.Contains(expected, actual)


// ---------------------------------
// Tests
// ---------------------------------

[<Fact>]
let ``Route / returns "Nothing to see here"``() =
    use server = new TestServer(createHost())
    use client = server.CreateClient()

    client
    |> httpGet "/"
    |> ensureSuccess
    |> readText
    |> shouldContain "Nothing to see here"


[<Fact>]
let ``Route /basic returns Catlight basic protocol"``() =
    use server = new TestServer(createHost())
    use client = server.CreateClient()

    let expectedData: BasicServerPayload =
        { protocol = "catlight.io/protocol/v1.0/basic"
          id = "SERVER_ID"
          webUrl = Some(Uri("http://www.perdu.com"))
          name = "Server Name"
          serverVersion = None
          currentUser = None
          spaces = [] }

    let expectedJson = """
        {
            "protocol":"catlight.io/protocol/v1.0/basic",
            "id": "SERVER_ID",
            "webUrl": "http://www.perdu.com",
            "name": "Server Name",
            "spaces": []
        }"""

    client
    |> httpGet "/basic"
    |> ensureSuccess
    |> readText
    |> shouldEqualJson expectedJson


[<Fact>]
let ``Route which doesn't exist returns 404 Page not found``() =
    use server = new TestServer(createHost())
    use client = server.CreateClient()

    client
    |> httpGet "/route/which/does/not/exist"
    |> isStatus HttpStatusCode.NotFound
    |> readText
    |> shouldEqual "Not Found"
