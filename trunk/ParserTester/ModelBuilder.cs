using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Core;
using System.Diagnostics;

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

        public static string getVal(this FSharpOption<Sql.dir> fOp) {
            if(FSharpOption<Sql.dir>.get_IsNone(fOp)) {
                return "";
            } else {
                return fOp.Value.Name;
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
        private SqlQuery qry;
        private Dictionary<string, SqlValue> builtValues = new Dictionary<string, SqlValue>();

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

            stmnt.Columns.ForEach(col => getOrBuildValue(col, true));

            if (FSharpOption<Sql.cond>.get_IsSome(stmnt.Where)){
                qry.Where = buildWhere(stmnt.Where.Value);
            }

            if (FSharpOption<Sql.top>.get_IsSome(stmnt.TopN)){
                Sql.top tp = stmnt.TopN.Value;
                String tpType;
                if(tp.IsTopPercent) {
                    tpType = "PERCENT";
                }else { 
                    tpType = ""; 
                }

                qry.Top = new SqlTop() {
                    N = tp.N,
                    type = tpType
                };
            }

            qry.OrderByColumns.AddRange(stmnt.OrderBy.Select(ob => new SqlOrderBy() {
                    Column = getOrBuildValue(ob.Column, false),
                    Dir = ob.Direction.getVal()
                }
            ));
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
            if(cond == null)
                return null;
            if(cond.isValue) {
                throw new Exception("Didn't expect value");
            }

            Sql.where whr = cond.Condition.Value;
            SqlValue lft;
            SqlValue rgt;
            if(whr.Left.isValue) {
                lft = getOrBuildValue(whr.Left.Value.Value, false);
            } else {
                lft = buildWhere(whr.Left);
            }

            if(whr.Right.isValue) {
                rgt = getOrBuildValue(whr.Right.Value.Value, false);
            } else {
                rgt = buildWhere(whr.Right);
            }

            return new SqlWhere() {
                LeftOperand = lft,
                RightOperand = rgt,
                Operator = whr.Operator
            };
        }

        private SqlValue getOrBuildValue(Sql.value vlIn, bool makeVisible) {
            string key = vlIn.ToString().ToLower();
            SqlValue val;
            if (builtValues.ContainsKey(key)){
                val = builtValues[key];
                if (makeVisible) { val.IsSelected = true; }
                return val;
            } else{
                val = buildValue(vlIn, makeVisible);
                builtValues.Add(key, val);
                qry.Columns.Add(val);
                Debug.Assert(val.IsSelected == makeVisible);
                return val;
            }
        }

        private SqlValue buildValue(Sql.value vlIn, bool isVisible) {
            string alias = null;
            Sql.value vl;
            if(vlIn.IsAliassedValue){
                alias = vlIn.Name;
                vl = vlIn.Value.Value;
            } else {
                vl = vlIn;
            }
            if(vl.IsField) {
                return new SqlField(){ Field = vl.Name, Alias = alias, IsSelected = isVisible };
            }
            if(vl.IsTableField) {
                Sql.table tbl = vl.Table.Value;
                
                return new SqlField() {
                    Alias = alias,
                    Field = vl.Name,
                    Table = qry.Tables[tbl.Identifier],
                    IsSelected = isVisible
                };
            }
            if(vl.IsFloat) {
                return new SqlFloat() { Float = vl.Name, Alias = alias, IsSelected = isVisible }; 
            }
            if(vl.IsFunction) {
                return new SqlFunction() {
                    Alias = alias,
                    FunctionName = vl.Name,
                    Parameters = vl.Params.Value.Select(prm => getOrBuildValue(prm, false)).ToList(),
                    Schema = getSchema(vl.Schema),
                    IsSelected = isVisible
                };
            }
            if(vl.IsString) {
                return new SqlString() { Str = vl.Name, Alias = alias, IsSelected = isVisible };
            }
            if(vl.IsInt) {
                return new SqlInt() { Int = vl.Name, Alias = alias, IsSelected = isVisible };
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
            if(qry.Schemas.ContainsKey(schemaName)) {
                return qry.Schemas[schemaName];
            } else {
                SqlSchema nw = new SqlSchema() { SchemaName = schemaName };
                qry.Schemas.Add(schemaName, nw);
                return nw;
            }
        }

    }
}
