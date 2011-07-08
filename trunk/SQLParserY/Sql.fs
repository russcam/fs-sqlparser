module Sql

open System

type ISql =
   abstract member toSql : unit -> string
        
let toSql (isql: ISql) =
    isql.toSql()

let castUpToISql valIn = 
    valIn :> ISql

//This allows me to apply a function to an option type without writing the pattern matching every time
let apply op func = 
    match op with   
        | Some(a) -> func a
        | None -> ""

//This allows to concatenate two option types
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

//This takes a list and does toSql on each item
//The match is a bit different that usual
//It is this way so that you have "A, B, C" instead of "A, B, C,"
let mapReduce delim (lstIn: 'A list)  =
    let lst = List.map castUpToISql lstIn
    let rec mapReduceISql delim (lst: ISql list) =
        match lst with
            | [] -> ""
            | hd :: [] -> hd.toSql()
            | hd :: tl :: [] -> hd.toSql() + delim + tl.toSql()
            | hd :: tl -> hd.toSql() + delim + mapReduceISql delim tl
    mapReduceISql delim lst

type alias = Alias of string
type dot = Dot of string
type dbo = Dbo 
type functionName = FunctionName of string

//TODO change into schema instead of DBO
type table = 
    | Table of (dbo option * string)
    | AliassedTable of (table * alias)
    interface ISql with
        member this.toSql() =
            match this with
                    | Table(Some(_), str) -> "dbo." + str
                    | Table(None, str) -> str
                    | AliassedTable(tbl, Alias(al)) -> toSql tbl + " AS " + al
    end

    member this.Rename (oldName: string) newName = 
            match this with
                    | Table(_, name) -> if name.ToLower() = oldName.ToLower() then AliassedTable(this, Alias(newName)) else this
                    | AliassedTable(tb, Alias(name))   
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(tb, Alias(newName)) else this

    member this.Alias (oldName: string) newName =
            match this with
                    | Table(d, name) 
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(this, Alias(newName)) else this
                    | _ -> this.Rename oldName newName

//When we Alias a value, should we add square brackets? That way we would be sure that we can use any string as an alias
//TODO RENAME "RENAME" TO ALIAS
type value =   
    | Int of string  
    | Float of string  
    | String of string  
    | Field of string
    | TableField of table * value
    | Function of (dbo option * functionName * value list)
    | AliassedValue of (value * string)
    with 
        member this.Rename oldName newName =
            match this with
                | TableField(tbl, fld) -> TableField(tbl.Rename oldName newName, fld)
                | Function(d, f, vals) -> Function(d, f, vals |> List.map (fun vl -> vl.Rename oldName newName))
                | AliassedValue(vl, str) -> AliassedValue(vl.Rename oldName newName, str)
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
                    | AliassedValue(vl, str) -> toSql vl + " AS " + str


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
        //Here we recreate a proper SQL string. Note that all previous formatting will be lost at the parser
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
            Columns = this.Columns |> List.map  (fun col -> col.Rename oldName newName);
            Joins = this.Joins |> List.map (fun jn -> jn.AliasTables oldName newName);
            Where = 
                match this.Where with
                    | Some(wh) -> Some(wh.Rename oldName newName);
                    | None -> None;
            OrderBy = this.OrderBy |> List.map (fun ob -> ob.Rename oldName newName); }

//Here I'm building a merge function in order to merge 2 different queries
//The type signature shows a bit how I intend to do it
//
//let merge (smt1: sqlStatement) (smt2: sqlStatement) (jn: join) =
//    {   TopN = mergeOptions smt1.TopN smt2.TopN (fun a b -> ??)    //Still deciding what to do here? Should we pick the largest one? But what if one is in percent and the other is not?
//        Table1 = smt1.Table1;
//        Columns = List.append smt1.Columns smt2.Columns;
//        Joins = List.append smt1.Joins smt2.Joins;
//        Where = mergeOptions smt1.Where smt2.Where And;
//        OrderBy = List.append smt1.OrderBy smt2.OrderBy;
//    }
//
