module Orsak.EndpointRouting

open Application
open Microsoft.AspNetCore.Http

open Orsak

open Giraffe
open Giraffe.EndpointRouting
open Orsak
open Fleece
open Fleece.SystemTextJson

let wrap (e: Effect<_, _, _>) = eff {
    //other things before the effect is run
    let! result = e
    //stuff after the effect is run
    return result
}



[<Struct>]
type EffectRunner<'a> =
    | RunWith of (HttpContext -> 'a)

    static member inline ( *>> )(effect: Effect<_, unit, string>, RunWith runEnv) : HttpHandler =
        fun next ctx -> task {
            match! Effect.run (runEnv ctx) (wrap effect) with
            | Ok() -> return! setStatusCode 204 <|| (next, ctx)
            | Error e -> return! setStatusCode 400 >=> setBodyFromString e <|| (next, ctx)
        }

    static member inline ( *>> )(effect: Effect<_, Response<'b>, string>, RunWith runEnv) : HttpHandler =
        fun next ctx -> task {
            match! Effect.run (runEnv ctx) (wrap effect) with
            | Ok(StatusOk b) ->
                let payload = Operators.toJsonText b

                return!
                    setStatusCode 200
                    >=> setHttpHeader "Content-Type" "application/json"
                    >=> setBodyFromString payload
                    <|| (next, ctx)
            | Ok(Created href) ->
                return!
                    setStatusCode 201
                    >=> setHttpHeader "Content-Type" "application/json"
                    >=> setHttpHeader "Location" href
                    <|| (next, ctx)
            | Error e -> return! setStatusCode 400 >=> setBodyFromString e <|| (next, ctx)
        }



let webApp runEnv : Endpoint list =

    [
        GET [ route "/ping" (ping () *>> RunWith runEnv) ]
        POST [ routef "/pong/%s" (fun s -> post s *>> RunWith runEnv) ]
    ]
