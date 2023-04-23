open System
open System.Threading.Channels
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open Microsoft.AspNetCore.SignalR.Client

open Giraffe
open Giraffe.EndpointRouting
open Orsak
open Orsak.Effects
open Orsak.Extensions
open Orsak.MinimalApi
open FSharpPlus
open FSharpPlus.Control
open Microsoft.AspNetCore.SignalR
open Rebus
open Rebus.Activation
open Rebus.Bus
open Rebus.Config
open Rebus.Routing
open Rebus.Routing.TypeBased
open Rebus.Transport.InMem
open Rebus.Transport
open SampleWeb.Transactional

let getService<'t> (app: IApplicationBuilder) =
    app.ApplicationServices.GetService<'t>()

type MainEnv = {
    context: HttpContext
    loggerFactory: ILoggerFactory
} with

    interface Orsak.Extensions.IContextProvider with
        member this.Context = this.context

    interface Orsak.Extensions.ILoggerProvider with
        member this.Logger s = this.loggerFactory.CreateLogger(s)

    interface Orsak.Scoped.TransactionScopeProvider<DbTransactional> with
        member this.BeginScope() = Transaction.create()


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    use activator = new BuiltinHandlerActivator()

    use bus =
        Configure
            .With(activator)
            .Transport(fun (t: StandardConfigurer<_>) -> t.UseInMemoryTransport(InMemNetwork(), ""))
            .Routing(fun r -> r.TypeBased().Map<int>("asdasd") |> ignore)
            .Start()

    let _ = bus.Subscribe<int>()

    builder.Services
        .AddSingleton<IChatHubProvider>(fun ctx ->
            let cty = ctx.GetRequiredService<IHubContext<ChatHub, IChatHub>>()

            { new IChatHubProvider with
                member this.Hub =
                    { new IChatHub with
                        member this.SendMessage(x, y) = cty.Clients.All.SendMessage(x, y)
                    }
            })
        .AddHostedService(fun _ ->
            { new BackgroundService() with
                member _.ExecuteAsync token = task { return () }
            })
        .AddSignalR()
    |> ignore

    builder.Logging.AddSimpleConsole(fun opts -> opts.IncludeScopes <- true)
    |> ignore

    let app = builder.Build()
    let loggerFactory = getService<ILoggerFactory> app
    let r = Random(420)

    let mainEnv ctx = {
        context = ctx
        loggerFactory = loggerFactory
    }

    app.UseGiraffe(EndpointRouting.webApp mainEnv) |> ignore

    app.Run()

    0 // Exit code
