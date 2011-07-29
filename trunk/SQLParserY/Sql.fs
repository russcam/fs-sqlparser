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
    member this.TableName = //Renamed this to tableName for consistent naming 
            match this with
                    | Table(_, tblName) -> tblName
                    | AliassedTable(tb, _) -> tb.TableName
    member this.AliasName =
            match this with
                    | Table(_, _) -> None
                    | AliassedTable(_,Alias(alName)) -> Some(alName)
    member this.Identifier =
            match this with 
                    | Table(_, tblName) -> tblName
                    | AliassedTable(_, Alias(alName)) -> alName
    member this.SchemaName = 
            match this with 
                    | Table(Some(Schema(schemaName)), _) -> schemaName
                    | Table(None,_) -> ""
                    | AliassedTable(tbl,_) -> tbl.SchemaName
    member this.Schema =
            match this with
                    | Table(sch,_) -> sch
                    | AliassedTable(tbl,_) -> tbl.Schema           
    override this.ToString() =
            match this with
                    | Table(None, tblName) -> tblName
                    | Table(Some(sch), tblName) -> sch.Name + "." + tblName
                    | AliassedTable(tbl, Alias(alName)) -> tbl.ToString() + " AS " + alName

type value =   
    | Int of string  
    | Float of string  
    | String of string  
    | Field of string
    | TableField of table * value
    | Function of (schema option * functionName * value list)
    | AliassedValue of (value * string)
    with 
        member this.Name =
                match this with
                    | Int(fld) -> fld
                    | Float(fld) -> fld
                    | String(fld) -> fld
                    | Field(fld) -> fld
                    | TableField(tbl, fld) -> fld.Name
                    | Function(_, fName, _) -> fName.Name
                    | AliassedValue(vl, al) -> al
        member this.Value =
            match this with
                | AliassedValue(vl, _) -> Some(vl)
                | TableField(_, fld) -> Some(fld)
                | _ -> None
        member this.Params =
            match this with
                | Function(_, _, vls) -> Some(vls)
                | _ -> None                   
        member this.Schema =
            match this with
                | TableField(tbl, _) -> tbl.Schema
                | Function(sch, _, _) -> sch
                | _ -> None
        member this.Table = 
            match this with
                | TableField(tbl, _) -> Some(tbl)
                | _ -> None
        override this.ToString() =
            match this with
                    | Int(fld) -> fld
                    | Float(fld) -> fld
                    | String(fld) -> fld
                    | Field(fld) -> fld
                    | TableField(tbl, fld) -> tbl.ToString() + "." + fld.ToString()
                    | Function(Some(sch), fName, vals) -> sch.Name + "." + fName.Name + vals.ToString()
                    | Function(None, fName, vals) -> fName.Name + vals.ToString()
                    | AliassedValue(vl, al) -> vl.ToString() + " AS " + al

type dir = Asc | Desc   
    with member this.Name = 
            match this with 
                | Asc -> "ASC"
                | Desc -> "DESC"

type op = Eq | Gt | Ge | Lt | Le  
    with member this.Name = 
            match this with 
                | Eq -> "="
                | Gt -> ">"
                | Ge -> ">="
                | Lt -> "<"
                | Le -> "<="

type order = Order of (value * dir option)
    with 
        member this.Column = 
            match this with
                | Order(vl, _) -> vl
        member this.Direction = 
            match this with
                | Order(_, dr) -> dr

type cond = 
    | WhereCond of (where)
    | CondValue of (value)
    with 
    member this.isValue =
        match this with 
            | CondValue(_) -> true
            | WhereCond(_) -> false
    member this.Value = 
        match this with 
            | CondValue(vl) -> Some(vl)
            | WhereCond(_) -> None
    member this.Condition =
        match this with 
            | CondValue(_) -> None
            | WhereCond(cnd) -> Some(cnd)
and where =   
    | Comp of (cond * op * cond)   
    | And of (cond * cond)
    | Or of (cond * cond)   
    with 
        member this.Operator =
            match this with
                | Comp(_, op1, _) -> op1.Name
                | And(_, _) -> "AND"
                | Or(_, _) ->  "OR"
        member this.Left =
            match this with
                | Comp(lhs, _, _) -> lhs
                | And(lhs, _) -> lhs
                | Or(lhs, _) -> lhs
        member this.Right =
            match this with
                | Comp(_, _, rhs) -> rhs
                | And(_, rhs) -> rhs
                | Or(_, rhs) -> rhs

type joinType = Inner | Left | Right | Outer
    with member this.Name = 
            match this with 
                | Inner -> "INNER JOIN"
                | Left -> "LEFT JOIN"
                | Right -> "RIGHT JOIN"
                | Outer -> "OUTER JOIN"

type join = Join of (table * joinType * cond option)   // table name, join, optional "on" clause   
    with 
        member this.JoinType =
            match this with
                | Join(_, jt, _) ->  jt.Name
        member this.RhsIdentifier =
            match this with
                | Join(tbl, _, _) -> tbl.Identifier
        member this.Where =
            match this with
                | Join(_, _, whr) -> whr
        member this.JoinTable =
            match this with
                | Join(tbl, _, _) -> tbl

type top = 
    | Top of (string)
    | TopPercent of (string)
    with
        member this.N =
                match this with
                    | Top(expr) -> expr
                    | TopPercent(expr) -> expr

type sqlStatement =   
    {   TopN : top option;
        Table1 : table;   
        Columns : value list;   
        Joins : join list;   
        Where : cond option;   
        OrderBy : order list }
    with
        member this.Identifiers =
            List.append (List.map (fun (jn: join) -> jn.RhsIdentifier) this.Joins) [this.Table1.Identifier]