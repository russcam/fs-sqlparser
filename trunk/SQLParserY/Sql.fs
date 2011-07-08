
module Sql

open System

type alias = Alias of string
type dot = Dot of string
type dbo = Dbo 

type ISql =
   abstract member toSql : unit -> string

let rec map func lst =  
    match lst with
        | [] -> []
        | hd :: tl -> func(hd) :: map func tl

         
let toSqlFun (isql: ISql) = 
    isql.toSql()

let toSql (isql: ISql) =
    isql.toSql()

let castUpToISql valIn = 
    valIn :> ISql

type table = 
    | Table of (dbo option * string)
    | AliassedTable of (alias * table )
    interface ISql with
        member this.toSql() =
            match this with
                    | Table(Some(d), str) -> "dbo." + str
                    | Table(None, str) -> str
                    | AliassedTable(Alias(al), tbl) -> toSql tbl + " AS " + al
    end

    member this.Rename (oldName: string) newName = 
            match this with
                    | Table(d, name) -> if name.ToLower() = oldName.ToLower() then Table(d, newName) else this
                    | AliassedTable(Alias(name), tb)   
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(Alias(newName), tb) else this

    member this.Alias (oldName: string) newName =
            match this with
                    | Table(d, name) 
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(Alias(newName), this) else this
                    | _ -> this.Rename oldName newName


type functionName = FunctionName of string

let mapReduce delim (lstIn: 'A list)  =
    let lst = map castUpToISql lstIn
    let rec mapReduceISql delim (lst: ISql list) =
        match lst with
            | [] -> ""
            | hd :: [] -> hd.toSql()
            | hd :: tl :: [] -> hd.toSql() + delim + tl.toSql()
            | hd :: tl -> hd.toSql() + delim + mapReduceISql delim tl
    mapReduceISql delim lst

type value =   
    | Int of string  
    | Float of string  
    | String of string  
    | Field of string
    | TableField of table * value
    | Function of (dbo option * functionName * value list)
    with 
        member this.Rename oldName newName =
            match this with
                | TableField(tbl, fld) -> TableField(tbl.Rename oldName newName, fld)
                | Function(d, f, vals) -> Function(d, f, vals |> map (fun vl -> vl.Rename oldName newName))
                | _ -> this
        interface ISql with 
            member this.toSql() = 
                match this with
                    | Int(str) -> str
                    | Field(str) -> str
                    | Float(str) -> str
                    | String(str) -> str 
                    | TableField(tbl, fld) -> toSql tbl + "." + toSql fld                 
                    | Function(None, FunctionName(name), vals) 
                        -> name + "(" + (mapReduce   ", " vals ) + ")"
                    | Function(Some(d), FunctionName(name), vals) 
                        -> "dbo." + name + "(" + (vals |> mapReduce ", ") + ")"


type dir = Asc | Desc   
   with interface ISql with 
            member this.toSql() =
                match this with
                    | Asc -> "Asc"
                    | Desc -> "Desc"

type op = Eq | Gt | Ge | Lt | Le   
   with interface ISql with 
            member this.toSql() =
                match this with
                    | Eq -> "="
                    | Gt -> ">" 
                    | Ge -> ">=" 
                    | Lt -> "<" 
                    | Le -> "<=" 

type order = Order of (value * dir option)
    with 
        member this.Rename oldName newName =
            match this with
                | Order(vl, drOp) -> Order(vl.Rename oldName newName, drOp)
        interface ISql with 
            member this.toSql() =
                match this with
                    | Order(vl, Some(dr)) -> toSql vl + " " + toSql dr
                    | Order(vl, None) -> toSql vl


type where =   
    | Cond of (value * op * value)   
    | And of (where * where)
    | Or of (where * where)   
    with 
        member this.Rename oldName newName =
            match this with
                | Cond(val1, op1, val2) -> Cond(val1.Rename oldName newName, op1, val2.Rename oldName newName)
                | And(val1, val2) -> And(val1.Rename oldName newName, val2.Rename oldName newName)
                | Or(val1, val2) -> Or(val1.Rename oldName newName, val2.Rename oldName newName)
        interface ISql with 
            member this.toSql() =
                match this with
                    | Cond(vl, p, vl2) -> toSql vl + " " + toSql p + " " + toSql vl2
                    | And(whr, whr2) -> toSql whr + " AND " + toSql whr2
                    | Or(whr, whr2) -> toSql whr + " OR " + toSql whr2

type joinType = Inner | Left | Right | Outer
    with interface ISql with 
            member this.toSql() =
                match this with 
                    | Inner -> "INNER JOIN"
                    | Left -> "LEFT JOIN"
                    | Right -> "RIGHT JOIN"
                    | Outer -> "FULL OUTER JOIN"

type join = Join of (table * joinType * where option)   // table name, join, optional "on" clause   
    with 
        member this.AliasTables oldName newName =
            match this with
                | Join(tbl, jn, Some(whr)) -> Join((tbl.Alias oldName newName), jn, Some(whr.Rename oldName newName))
                | Join(tbl, jn, _) -> Join((tbl.Alias oldName newName), jn, None)
        interface ISql with 
            member this.toSql() =
                match this with
                    | Join(tbl, jn, Some(whr)) -> toSql jn + " " + toSql tbl + " ON " + toSql whr
                    | Join(tbl, jn, _) -> toSql jn + " " + toSql tbl 


let apply op func = 
    match op with   
        | Some(a) -> func a
        | None -> ""

type top = 
    | Top of (string)
    | TopPercent of (string)
    with interface ISql with 
            member this.toSql() =
                match this with
                        | Top(vl) -> "TOP " + vl + " "
                        | TopPercent(vl) -> "TOP " + vl + " PERCENT "

type sqlStatement =   
    {   TopN : top option;
        Table1 : table;   
        Columns : value list;   
        Joins : join list;   
        Where : where option;   
        OrderBy : order list }
    with 
        member this.toSql =
            "SELECT " 
            + apply this.TopN toSql 
            + (this.Columns |> mapReduce ", ")
            + " FROM " + toSql this.Table1 
            + " " + (this.Joins |> mapReduce " ")
            + apply this.Where (fun whr -> " WHERE " + toSql whr)
            + " ORDER BY " + (this.OrderBy |> mapReduce ", ")
        member this.RenameTables oldName newName = 
          { TopN = this.TopN;
            Table1 = this.Table1.Alias oldName newName;
            Columns = this.Columns |> map  (fun col -> col.Rename oldName newName);
            Joins = this.Joins |> map (fun jn -> jn.AliasTables oldName newName);
            Where = 
                match this.Where with
                    | Some(wh) -> Some(wh.Rename oldName newName);
                    | None -> None;
            OrderBy = this.OrderBy |> map (fun ob -> ob.Rename oldName newName); }


let mergeOptions op1 op2 mergeFun = 
    match op1 with
        | Some(vl1) ->
            match op2 with
                | Some(vl2) -> Some(mergeFun(vl1, vl2))
                | None -> op1
        | None ->
            match op2 with
                | Some(vl2) -> op2
                | None -> None
//
//let merge (smt1: sqlStatement) (smt2: sqlStatement) (jn: join) =
//    {   TopN = mergeOptions smt1.TopN smt2.TopN (fun a b -> Int
//        Table1 = smt1.Table1;
//        Columns = List.append smt1.Columns smt2.Columns;
//        Joins = List.append smt1.Joins smt2.Joins;
//        Where = mergeOptions smt1.Where smt2.Where And;
//        OrderBy = List.append smt1.OrderBy smt2.OrderBy;
//    }
//
