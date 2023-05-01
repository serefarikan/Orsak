namespace Orsak.Extensions

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Orsak
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open System.Security.Cryptography
open Microsoft.AspNetCore.SignalR
open System.Threading.Tasks
open System.Globalization


type RNGProvider =
    abstract member Gen: RandomNumberGenerator

module RandomNumberGenerator =
    let getBytes (x: byte array) =
        Effect.Create(fun (provider: #RNGProvider) -> provider.Gen.GetBytes(x))

type ILoggerProvider =
    abstract member Logger: string -> ILogger

type Log =
    static member getLogger(s: string) =
        Effect.Create(fun (provider: #ILoggerProvider) -> provider.Logger(s))

    static member logInformation(message, [<ParamArray>] args) =
        Effect.Create(fun (provider: #ILoggerProvider) -> provider.Logger("").LogInformation(message, args))


type IContextProvider =
    abstract member Context: HttpContext

module HttpContext =
    open Giraffe

    let current () =
        Effect.Create(fun (provider: #IContextProvider) -> provider.Context)

type IChatHub =
    abstract member SendMessage: user: string * message: string -> Task

type ChatHub() =
    inherit Hub<IChatHub>()

    member this.SendMessage(user, message) =
        this.Clients.Caller.SendMessage(user, message)

    override this.OnConnectedAsync() =
        let caller = this.Clients.Caller
        let user = this.Context.User
        Task.CompletedTask


type IChatHubProvider =
    abstract member Hub: IChatHub

module ChatHub =
    let sendMessage user message =
        Effect.Create(fun (provider: IChatHubProvider) -> task { do! provider.Hub.SendMessage(user, message) })


open Orsak.Scoped
open Azure.Storage.Queues
open Azure.Storage.Queues.Models

type MessageScope(queue: QueueClient, msg: QueueMessage) =
    member val Message = msg

    interface TransactionScope with
        member this.CommitAsync() : Task = task {
            let! _ = queue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt)
            return ()
        }

        member this.DisposeAsync() : ValueTask = ValueTask.CompletedTask

type MessageSink =
    abstract member Send: byte[] -> Task<unit>

type MessageSinkProvider =
    abstract member Sink: MessageSink

module Message =
    open System.Text.Json
    open Orsak.Effects
    open FSharp.Control
    open Fleece
    open Fleece.SystemTextJson
    open FSharpPlus

    type MessageModel = {
        message: string
        batchId: string
        orderId: string
    } with

        static member ToJson(x: MessageModel) =
            jobj [ "message" .= x.message; "batchId" .= x.batchId; "orderId" .= x.orderId ]

        static member OfJson json =
            match json with
            | JObject o -> monad {
                let! message = o .@ "message"
                let! batchId = o .@ "batchId"
                let! orderId = o .@ "orderId"
                return { message = message; batchId = batchId; orderId = orderId }
              }
            | x -> Decode.Fail.objExpected x

    let inline read () =
        mkEffect (fun (scope: MessageScope) -> vtask {
            match
                JsonDocument.Parse(scope.Message.Body.ToArray()).RootElement
                |> Encoding.Wrap
                |> Operators.ofJson
            with
            | Ok a -> return Ok a
            | Error decodeError -> return Error(decodeError.ToString())
        })

    ///This is mostly for max control, this can be done in much fewer lines of code if so desired.
    let send (message: MessageModel) =
        Effect.Create(fun (provider: #MessageSinkProvider) -> task {
            use ms = new IO.MemoryStream()
            use writer = new Utf8JsonWriter(ms)
            let enc = Operators.toJsonValue message
            enc.WriteTo writer
            writer.Flush()
            let result = ms.ToArray()
            do! provider.Sink.Send(result)
        })

    let msgEff = TransactionalEffectBuilder<MessageScope>()

    let tmp () : Effect<_, MessageModel, _> = msgEff { return! read () }

    let markAsRead (message: MessageModel) = commitEff { return () }
