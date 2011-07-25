using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Core;

namespace ParserTester {

    static class FSharpOptionExt {
        //Makes an F# option behave like a nullable value where None == Null
        public static string getVal(this FSharpOption<string> fOp) {
            if(FSharpOption<string>.get_IsNone(fOp)) {
                return null;
            } else {
                return fOp.Value;
            }
        }
    }
    
    /*
    //I wanted to do this :(
    static class FSharpOptionExt<T> {
        //Makes an F# option behave like a nullable value where None == Null
        public static T getVal(this FSharpOption<T> fOp) {
            if(FSharpOption<T>.get_IsNone(fOp)) {
                return default(T);
            } else {
                return fOp.Value;
            }
        }
    }
    */

    class ModelBuilder {
        //I introduced this class so that only this class has dependencies on the F# project
        private Dictionary<string, SqlSchema> schemas = new Dictionary<string,SqlSchema>();
        private SqlQuery qry;
        
        public SqlQuery build(string SQLString) {
            Sql.sqlStatement stmnt;
            try {
                stmnt = Parser.ParseSql(SQLString);
            } catch(Exception) {
                //TODO catch the rigth exception here
                //I dont want to throw the parse exception because it would introduce a dependencies between the model and the parser
                throw new Exception("Parse Error");
            }
            qry = new SqlQuery();


            List<SqlTable> tbls = new List<SqlTable>();
            tbls.Add(new SqlTable() {
                    Schema = getSchema(stmnt.Table1.SchemaName),
                    AliasName = stmnt.Table1.AliasName.getVal(),
                    TableName = stmnt.Table1.TableName
                }
            );

            tbls.AddRange(
                    stmnt
                    .Joins
                    .Select(jn => new SqlTable() {
                            AliasName = jn.JoinTable.AliasName.getVal(),
                            Schema = getSchema(jn.JoinTable.SchemaName),
                            TableName = jn.JoinTable.TableName
                    }
                )
            );

            qry.Tables = tbls.ToDictionary(tbl => tbl.Identifier);

            qry.JoinItems.AddRange(
                stmnt
                .Joins
                .Select(
                    jn => new SqlJoin() {
                        RhsTable = qry.Tables[jn.JoinTable.Identifier],
                        SqlJoinType = jn.JoinType,
                        Criteria = buildWhere(jn),
                    }
                )
            );

            //stmnt.Columns
            //TODO Add rest
            return qry;
        }

        private SqlWhere buildWhere(Sql.join jn) {
            if(FSharpOption<Sql.cond>.get_IsSome(jn.Where)) {
                return buildWhere(jn.Where.Value);
            } else {
                return null;
            }
        }

        private SqlWhere buildWhere(Sql.cond cond) {
            if(cond.isValue) {
                throw new Exception("Didn't expect value");
            }

            Sql.where whr = cond.Condition.Value;
            SqlValue lft;
            SqlValue rgt;
            if(whr.Left.isValue) {
                lft = buildValue(whr.Left.Value.Value);
            } else {
                lft = buildWhere(whr.Left);
            }

            if(whr.Right.isValue) {
                rgt = buildValue(whr.Right.Value.Value);
            } else {
                rgt = buildWhere(whr.Right);
            }

            return new SqlWhere() {
                LeftOperand = lft,
                RightOperand = rgt,
                Operator = whr.Operator
            };
        }

        private SqlValue buildValue(Sql.value vlIn) {
            string alias = null;
            Sql.value vl;
            if(vlIn.IsAliassedValue){
                alias = vlIn.Name;
                vl = vlIn.Value.Value;
            } else {
                vl = vlIn;
            }
            if(vl.IsField) {
                return new SqlField(){ Field = vl.Name, Alias = alias };
            }
            if(vl.IsTableField) {
                Sql.table tbl = vl.Table.Value;
                
                return new SqlField() {
                    Alias = alias,
                    Field = vl.Name,
                    Table = qry.Tables[tbl.Identifier] 
                };
            }
            if(vl.IsFloat) {
                return new SqlFloat() { Float = vl.Name, Alias = alias }; 
            }
            if(vl.IsFunction) {
                return new SqlFunction() {
                    Alias = alias,
                    FunctionName = vl.Name,
                    Parameters = vl.Params.Value.Select(prm => buildValue(prm)).ToList(),
                    Schema = getSchema(vl.Schema)
                };
            }
            if(vl.IsString) {
                return new SqlString() { Str = vl.Name, Alias = alias };
            }
            if(vl.IsInt) {
                return new SqlInt() { Int = vl.Name, Alias = alias };
            }
            throw new NotImplementedException(vl.ToString());
        }

        private SqlSchema getSchema(FSharpOption<Sql.schema> schemaOp) {
            if(FSharpOption<Sql.schema>.get_IsSome(schemaOp)) {
                return getSchema(schemaOp.Value.Name);
            } else {
                return null;
            }
        }

        private SqlSchema getSchema(string schemaName) {
            if (String.IsNullOrEmpty(schemaName)){
                return null;   
            }
            if(schemas.ContainsKey(schemaName)) {
                return schemas[schemaName];
            } else {
                SqlSchema nw = new SqlSchema() { SchemaName = schemaName };
                schemas.Add(schemaName, nw);
                return nw;
            }
        }

    }
}
