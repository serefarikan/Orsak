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
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

[<AutoOpen>]
module Extensions =
    type IEndpointRouteBuilder with

        member builder.MapGet(route, effect: Effect<'r, IResult, string>) =
            builder.MapGet(
                route,
                Func<'r, Task<IResult>>(fun (a: 'r) -> task {
                    let! result = effect |> Effect.run a

                    match result with
                    | Ok(res: IResult) -> return res
                    | Error e -> return Results.BadRequest(e)
                })
            )

        member builder.MapGet<'r, 'p>(route, effect: 'p -> Effect<'r, IResult, string>) =
            builder.MapGet(
                route,
                Func<'r * 'p, Task<IResult>>(fun ([<FromServices>] a: 'r, p: 'p) -> task {
                    let! result = effect p |> Effect.run a

                    match result with
                    | Ok(res: IResult) -> return res
                    | Error e -> return Results.BadRequest(e)
                })
            )
