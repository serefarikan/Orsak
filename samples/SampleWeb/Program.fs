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

open Azure.Storage.Queues // Namespace for Queue storage types
open Azure.Storage.Queues.Models // Namespace for PeekedMessage

open SampleWeb.Transactional
open FSharp.Control

let getService<'t> (app: IApplicationBuilder) =
    app.ApplicationServices.GetService<'t>()

type MainEnv = {
    context: HttpContext
    loggerFactory: ILoggerFactory
    queueClient: QueueClient
} with

    interface IContextProvider with
        member this.Context = this.context

    interface ILoggerProvider with
        member this.Logger s = this.loggerFactory.CreateLogger(s)

    interface Scoped.TransactionScopeProvider<MessageScope> with
        member this.BeginScope() = vtask {
            let! msg = this.queueClient.ReceiveMessageAsync()
            return MessageScope(this.queueClient, msg.Value)
        }

    interface Scoped.TransactionScopeProvider<DbTransactional> with
        member this.BeginScope() = Transaction.create()

    interface Scoped.ExceptionHandler<string> with
        member this.Handle(arg1: exn): string = 
            raise (System.NotImplementedException())

    interface MessageSink with
        member this.Send(bytes: byte array): Task<unit> = 
            task {
                let! _ = this.queueClient.SendMessageAsync (BinaryData.FromBytes bytes)
                return ()
            }

    interface MessageSinkProvider with
        member this.Sink: MessageSink = this

[<EntryPoint>]
let main args =
    let queueCLient = QueueClient("UseDevelopmentStorage=true", "my-ku")
    let _ = queueCLient.CreateIfNotExists()
    let x = queueCLient.ReceiveMessages()
    for message in x.Value do
        queueCLient.DeleteMessage(message.MessageId, message.PopReceipt) |> ignore
        ()
    let builder = WebApplication.CreateBuilder(args)

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
        queueClient = queueCLient
    }

    app.UseRouting().UseGiraffe(EndpointRouting.webApp mainEnv) |> ignore

    app.Run()

    0 // Exit code
