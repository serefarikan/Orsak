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
    static member s_processingPriorityItem  =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(13, ""),
            "Epic failure processing item!")



    static member getLogger(s: string) =
        Effect.Create(fun (provider: #ILoggerProvider) -> provider.Logger(s))

    static member logInformation
        (
            message,
            [<ParamArray>] args
        ) =
        Effect.Create(fun (provider: #ILoggerProvider) -> provider.Logger("").LogInformation(message, args))

    static member PriorityItemProcessed(workItem) =
        Effect.Create(fun (provider: #ILoggerProvider) ->
            let s: Printf.StringFormat<_> = "%i"
            let b = sprintf s 1
            let _formatter = String.Format(CultureInfo.InvariantCulture, "", 1) 
            Log.s_processingPriorityItem.Invoke(provider.Logger(""), workItem, Unchecked.defaultof<_>))


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
