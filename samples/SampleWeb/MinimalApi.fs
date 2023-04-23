namespace Orsak.MinimalApi

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Orsak
open Microsoft.AspNetCore.Builder

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Orsak.Extensions

[<AutoOpen>]
module Extensions =
    type IEndpointRouteBuilder with

        member builder.MapGet(route, effect: Effect<IChatHubProvider, IResult, string>) =
            builder.MapGet(
                route,
                Func<_, _>(fun (a) -> task {
                    let! result = effect |> Effect.run a

                    match result with
                    | Ok(res: IResult) -> return res
                    | Error e -> return Results.BadRequest(e)
                })
            )
