module Sql

open System

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

let rec mapReduce delim lst func =
    match lst with
        | [] -> ""
        | hd :: [] -> func hd
        | hd :: tl :: [] -> func hd + delim + func tl
        | hd :: tl -> func hd + delim + mapReduce delim tl func 

type name = Name of string
type alias = Alias of string
type dot = Dot of string
type schema = Schema of string
    with
        member this.Name =
                match this with
                    | Schema(name) -> name

type functionName = FunctionName of string
    with
        member this.Name =
                match this with
                    | FunctionName(name) -> name

type table = 
    | Table of (schema option * string)
    | AliassedTable of (table * alias)
    member this.Rename (oldName: string) newName = 
            match this with
                    | Table(_, name) -> if name.ToLower() = oldName.ToLower() then AliassedTable(this, Alias(newName)) else this
                    | AliassedTable(tb, Alias(name))   
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(tb, Alias(newName)) else this

    member this.Alias (oldName: string) newName =
            match this with
                    | Table(_, name) 
                        -> if name.ToLower() = oldName.ToLower() then AliassedTable(this, Alias(newName)) else this
                    | _ -> this.Rename oldName newName
    member this.Name =
            match this with
                    | Table(_, tbl) -> tbl
                    | AliassedTable(tb, Alias(name)) -> name
    
//When we Alias a value, should we add square brackets? That way we would be sure that we can use any string as an alias
//TODO RENAME "RENAME" TO ALIAS
type value =   
    | Int of string  
    | Float of string  
    | String of string  
    | Field of string
    | TableField of table * value
    | Function of (schema option * functionName * value list)
    | AliassedValue of (value * string)
    with 
        member this.Rename oldName newName =
            match this with
                | TableField(tbl, fld) -> TableField(tbl.Rename oldName newName, fld)
                | Function(sch, fName, vals) -> Function(sch, fName, vals |> List.map (fun vl -> vl.Rename oldName newName))
                | AliassedValue(vl, str) -> AliassedValue(vl.Rename oldName newName, str)
                | _ -> this
        member this.Name =
                match this with
                    | Int(fld) -> fld
                    | Float(fld) -> fld
                    | String(fld) -> fld
                    | Field(fld) -> fld
                    | TableField(tbl, fld) -> tbl.Name + "." + fld.Name
                    | Function(sch, fName, vals) -> 
                        match sch with
                            | Some(sch) -> sch.Name + "." + fName.Name + "(" + (mapReduce ", " vals (fun vl -> vl.Name)) + ")"
                            | None -> fName.Name + "(" + (mapReduce ", " vals (fun vl -> vl.Name)) + ")"
                    | AliassedValue(vl, str) -> vl.Name + " AS " + str

type dir = Asc | Desc   
    with member this.Name = 
            match this with 
                | Asc -> "ASC "
                | Desc -> "DESC "

type op = Eq | Gt | Ge | Lt | Le   
    with member this.Name = 
            match this with 
                | Eq -> "= "
                | Gt -> "> "
                | Ge -> ">= "
                | Lt -> "< "
                | Le -> "<= "

type order = Order of (value * dir option)
    with 
        member this.Rename oldName newName =
            match this with
                | Order(vl, drOp) -> Order(vl.Rename oldName newName, drOp)
        member this.Name = 
            match this with
                | Order(vl, Some(dr)) -> vl.Name + dr.Name
                | Order(vl, _) -> vl.Name

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
        member this.Name =
            match this with
                | Cond(val1, op1, val2) -> val1.Name + " " + op1.Name + val2.Name
                | And(val1, val2) -> val1.Name + " AND \n\t" + val2.Name
                | Or(val1, val2) ->  val1.Name + " OR \n\t" + val2.Name

type joinType = Inner | Left | Right | Outer
    with member this.Name = 
            match this with 
                | Inner -> "INNER JOIN "
                | Left -> "LEFT JOIN "
                | Right -> "RIGHT JOIN "
                | Outer -> "OUTER JOIN "

type join = Join of (table * joinType * where option)   // table name, join, optional "on" clause   
    with 
        member this.AliasTables oldName newName =
            match this with
                | Join(tbl, jn, Some(whr)) -> Join((tbl.Alias oldName newName), jn, Some(whr.Rename oldName newName))
                | Join(tbl, jn, _) -> Join((tbl.Alias oldName newName), jn, None)
        member this.Name =
            match this with
                | Join(tbl, jntp, Some(whr)) -> jntp.Name + tbl.Name + whr.Name
                | Join(tbl, jntp, _) -> jntp.Name + tbl.Name

type top = 
    | Top of (string)
    | TopPercent of (string)
    with
        member this.Name =
                match this with
                    | Top(expr) -> "TOP " + expr
                    | TopPercent(expr) -> "TOP " + expr + " PERCENT"


type sqlStatement =   
    {   TopN : top option;
        Table1 : table;   
        Columns : value list;   
        Joins : join list;   
        Where : where option;   
        OrderBy : order list }
    with 
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
        member this.Name =
            "SELECT " 
            + match this.TopN with
                | Some(tp) -> tp.Name + " \n\t"
                | _ -> "\n\t"
            + mapReduce ", \n\t" this.Columns (fun vl -> vl.Name) + " \n"
            + "FROM \n\t" 
            + this.Table1.Name + "\n\t" 
            + mapReduce "\n\t" this.Joins (fun jn -> jn.Name)
            + match this.Where with 
                | Some(whr) -> "\nWHERE \n\t" + whr.Name
                | _ -> ""
            + if this.OrderBy.Length > 0 then 
                "\nORDER BY \n\t" + mapReduce ", \n\t" this.OrderBy (fun ob -> ob.Name) 
              else ""

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
