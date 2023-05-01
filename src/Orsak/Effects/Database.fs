namespace Orsak.Effects

open System.Threading
open System.Runtime.CompilerServices

#nowarn "57"

open System
open System.Data.Common
open System.Threading.Tasks
open Orsak
open Orsak.ScopeAware
open Orsak.Scoped
open FSharp.Control

[<Experimental("Experimental feature, API not stable")>]
type DbTransactional(tran: DbTransaction) =
    member val Connection = tran.Connection
    interface TransactionScope with
        member this.CommitAsync() : Task = tran.CommitAsync()

        member this.DisposeAsync() : ValueTask =
            vtask {
                do! tran.DisposeAsync()
                do! this.Connection.DisposeAsync()
            }

type Transaction<'a, 'err> = Effect<DbTransactional, 'a, 'err>

type EnlistedTransaction<'r, 'a, 'err> = Effect<'r * DbTransactional, 'a, 'err>

[<AutoOpen>]
module Builder =
    let commitEff = TransactionalEffectBuilder<DbTransactional>()


