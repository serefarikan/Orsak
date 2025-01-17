open System
open System.Threading.Channels
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open Giraffe
open Orsak
open FSharpPlus
open FSharpPlus.Control

let getService<'t> (app: IApplicationBuilder) =
    app.ApplicationServices.GetService<'t>()

type MainEnv = {
    context: HttpContext
    loggerFactory: ILoggerFactory
} with

    interface Orsak.Extensions.IContextProvider with
        member this.Context = this.context

    interface Orsak.Extensions.ILoggerProvider with
        member this.Logger = this.loggerFactory.CreateLogger("EffectLogger")


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Logging.AddSimpleConsole(fun opts -> opts.IncludeScopes <- true)
    |> ignore

    let app = builder.Build()
    let loggerFactory = getService<ILoggerFactory> app
    let r = Random(420)

    let mainEnv ctx = {
        context = ctx
        loggerFactory = loggerFactory
    }

    app.UseGiraffe(Routes.R.webApp mainEnv)

    app.Run()

    0 // Exit code
