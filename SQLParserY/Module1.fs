// Learn more about F# at http://fsharp.net
#light

module Module1

open System   
open Sql   
 

let parseit x =
    let lexbuf = Lexing.LexBuffer<_>.FromString x   
    try
        let y = SqlParser.start SqlLexer.tokenize lexbuf 
        printfn "%A" y  
        let z = y.RenameTables "T1" "X1"
        printfn "%A" z
        printfn "%A" z.toSql
    with
       | ex -> printfn "%A" ex
