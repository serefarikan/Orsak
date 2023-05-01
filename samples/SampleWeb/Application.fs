module Application

open Orsak
open Orsak.Extensions
open Orsak.Extensions.Message

open Fleece
open Fleece.SystemTextJson

type Response<'a> =
    | StatusOk of 'a
    | Created of string

type Message = {
    message: string
} with

    static member ToJson(x: Message) = jobj [ "message" .= x.message ]


let ping () : Effect<_, _, _> = eff {
    do! Log.logInformation ("Hi", 1, 2.)
    let msg = { message = "hi"; batchId = "1"; orderId = "2" }
    do! Message.send msg
    let! silly = Message.tmp()
    return ()
}

let post (target: string) = eff { return StatusOk { message = target } }
