module SampleWeb.Transactional

open System.Data
open System.Data.Common
open System.Threading.Tasks
open FSharp.Control
open FSharpPlus.Control
open FSharpPlus.Operators
open Dapper
open Microsoft.Data.Sqlite
open Orsak
open Orsak.Scoped
open Orsak.Effects

module Transaction =
    let create() =
        vtask {
            let conn: DbConnection = new SqliteConnection("Data Source=test.db;Cache=Shared")
            do! conn.OpenAsync()
            let! tran = conn.BeginTransactionAsync()
            return DbTransactional(tran)
        }

    let read<'a> (sql: string) =
        mkEffect (fun (tran: DbTransactional) -> vtask {
            let connection = tran.Connection
            let b = connection.Query<'a> sql
            return Ok b
        })



let aasd () = commitEff {
    do! Effect.ret ()
    let! list1 = Transaction.read<int> "asdasd"
    let list = Seq.toList list1
    do! Effect.ret ()
    let! list2 = Transaction.read<int> "asdasd"
    let x = Seq.toList list2
    let! y = Effect.ret x
    return! Effect.ret x
}
