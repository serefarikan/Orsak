module Application

open Orsak
open Orsak.Extensions
open Orsak.Extensions.Message

open Fleece
open Fleece.SystemTextJson
open Orsak.Scoped
open Orsak.Effects
open SampleWeb
open Microsoft.AspNetCore.Http

type Response<'a> =
    | StatusOk of 'a
    | Created of string

type Message = {
    message: string
} with

    static member ToJson(x: Message) = jobj [ "message" .= x.message ]



let ping () : Effect<_, _, _> = eff {
    do! Log.logInformation ("Hi", 1, 2.)
    let! batchId = GuidGenerator.newGuid ()
    let msg = { message = "hi"; batchId = batchId.ToString(); orderId = "2" }
    do! Message.send msg
    return ()
}

let ping2 (x: int) : Effect<_, _, _> = eff {
    do! Log.logInformation ("Hi", 1, 2.)
    let! batchId = GuidGenerator.newGuid ()
    let msg = { message = "hi"; orderId = batchId.ToString(); batchId = "2" }
    do! Message.send msg
    return Results.Ok()
}

let post (target: string) = eff { return StatusOk { message = target } }
