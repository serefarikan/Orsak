open System
open System.Threading.Channels
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open ClassLibrary1
open Microsoft.AspNetCore.SignalR.Client
open SampleWeb.BackgroundWorker
open Giraffe
open Giraffe.EndpointRouting
open Orsak
open Orsak.Scoped
open Orsak.Effects
open Orsak.Extensions
open Orsak.MinimalApi
open FSharpPlus
open FSharpPlus.Control
open Microsoft.AspNetCore.SignalR

open Azure.Storage.Queues // Namespace for Queue storage types
open Azure.Storage.Queues.Models // Namespace for PeekedMessage
open Microsoft.AspNetCore.Mvc
open SampleWeb
open FSharp.Control
open Orsak.Extensions.Message

[<EntryPoint>]
let main args =
    let queueCLient = QueueClient("UseDevelopmentStorage=true", "my-ku")
    Transaction.setup ()
    let builder = WebApplication.CreateBuilder(args)

    builder.Services
        .AddHttpContextAccessor()
        .AddSingleton<BackgroundEnv>(fun ctx -> {
            loggerFactory = ctx.GetRequiredService<_>()
            queueClient = MessageScope queueCLient
        })
        .AddSingleton<MainEnv>(fun ctx -> {
            context = ctx.GetRequiredService<IHttpContextAccessor>().HttpContext
            loggerFactory = ctx.GetRequiredService<_>()
            queueClient = MessageScope queueCLient

        })
        .AddSingleton<IChatHubProvider>(fun ctx ->
            let cty = ctx.GetRequiredService<IHubContext<ChatHub, IChatHub>>()

            { new IChatHubProvider with
                member this.Hub =
                    { new IChatHub with
                        member this.SendMessage(x, y) = cty.Clients.All.SendMessage(x, y)
                    }
            })
        .AddEffectWorker<BackgroundEnv, string>(msgWork)
        .AddSignalR()
    |> ignore

    builder.Logging.AddSimpleConsole(fun opts -> opts.IncludeScopes <- true)
    |> ignore

    let app = builder.Build()

    let loggerFactory = app.Services.GetService<ILoggerFactory>()

    let mainEnv ctx = { context = ctx; loggerFactory = loggerFactory; queueClient = MessageScope queueCLient }

    app
        .UseRouting()
        .UseEndpoints(fun builder ->
            builder.MapGet("/todos/{x:int}", Func<_, _>(fun (x: int) -> x)) |> ignore

            builder.MapGetx<int, MainEnv, string>("/test/{x:int}", Application.ping2)
            |> ignore)
    //.UseGiraffe(EndpointRouting.webApp mainEnv)
    |> ignore

    app.Run()

    0 // Exit code
