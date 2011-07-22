// Learn more about F# at http://fsharp.net
#light

module Parser

open System   
open Sql   
 

let parseit x =
    let lexbuf = Lexing.LexBuffer<_>.FromString x   
    try
        let y = SqlParser.start SqlLexer.tokenize lexbuf 
        printfn "%A" y  
    with
       | ex -> printfn "%A" ex


let ParseSql x = 
    let lexbuf = Lexing.LexBuffer<_>.FromString x
    SqlParser.start SqlLexer.tokenize lexbuf   