﻿// Learn more about F# at http://fsharp.net
#light

module Module1

open System   
open Sql   
 

let parseit x =
    let lexbuf = Lexing.LexBuffer<_>.FromString x   
    try
        let y = SqlParser.start SqlLexer.tokenize lexbuf 
        printfn "%A" y  
        printfn "%A" y.toSql  
    with
       | ex -> printfn "%A" ex
