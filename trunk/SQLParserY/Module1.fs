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
        printfn "%A" y.Name  
        let z = y.Alias "T1" "TEE1"
        printfn "%A" z.Name
    with
       | ex -> printfn "%A" ex


let ParseSql x = 
    let lexbuf = Lexing.LexBuffer<_>.FromString x
    let y = SqlParser.start SqlLexer.tokenize lexbuf   
    y