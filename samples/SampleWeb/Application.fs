module Application

open Orsak
open Orsak.Extensions

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
    return ()
}

let post (target: string) = eff { return StatusOk { message = target } }
