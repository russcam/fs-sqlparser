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
        //private SqlQuery qry;
        //private Dictionary<string, SqlValue> builtValues = new Dictionary<string, SqlValue>();

        public static SqlQuery build(string SQLString) {
            return append(new SqlQuery(), SQLString);
        }

        public static SqlQuery append(SqlQuery qry, string SQLString) {
            if(qry == null) { throw new Exception("Cannot append to empty query"); }
            if(string.IsNullOrEmpty(SQLString)) { throw new Exception("Empty SQL String cannot be build into a query object"); }

            Sql.sqlStatement stmnt = Parse(SQLString);
            qry = readTables(qry, stmnt);
            qry = readJoins(qry, stmnt);
            qry = readColumns(qry, stmnt);
            qry = readWhere(qry, stmnt);
            qry = readTopN(qry, stmnt);
            qry = readOrderBy(qry, stmnt);

            return qry;
        }

        private static SqlQuery readOrderBy(SqlQuery qry, Sql.sqlStatement stmnt) {
            qry.OrderByColumns.AddRange( 
                stmnt.OrderBy.Select(ob => new SqlOrderBy() {
                    Column = getOrBuildValue(qry, ob.Column, false),
                    Dir = ob.Direction.getVal()
                }
            ));
            return qry;
        }

        private static SqlQuery readTopN(SqlQuery qry, Sql.sqlStatement stmnt) {
            if(FSharpOption<Sql.top>.get_IsSome(stmnt.TopN)) {
                Sql.top tp = stmnt.TopN.Value;
                String tpType = "";

                //If one is top percent and the other is topN we go with topN
                if(tp.IsTopPercent && qry.Top == null) {
                    tpType = "PERCENT";
                }

                if(qry.Top == null) {
                    qry.Top = new SqlTop() {
                        N = tp.N,
                        type = tpType
                    };
                } else {
                    try {
                        qry.Top.N = Math.Max(int.Parse(qry.Top.N), int.Parse(tp.N)).ToString();
                    } catch {
                        //Ignore in runtime throw something in debug.
                        Debug.Assert(false, "TopN parse Error"); 
                    }
                }
            }
            return qry;
        }

        private static SqlQuery readWhere(SqlQuery qry, Sql.sqlStatement stmnt) {
            if(FSharpOption<Sql.cond>.get_IsSome(stmnt.Where)) {
                if(qry.Where == null) {
                    qry.Where = buildWhere(qry, stmnt.Where.Value);
                } else {
                    //If there already was one we create a new rootnode for the Where tree
                    qry.Where = new SqlWhere() {
                        IsSelected = false,
                        LeftOperand = qry.Where,
                        RightOperand = buildWhere(qry, stmnt.Where.Value),
                        Operator = "AND"
                    };
                }
            }
            return qry;
        }

        private static SqlQuery readColumns(SqlQuery qry, Sql.sqlStatement stmnt) {
            stmnt.Columns.ForEach(col => getOrBuildValue(qry, col, true));
            return qry;
        }

        private static SqlQuery readJoins(SqlQuery qry, Sql.sqlStatement stmnt) {
            qry.JoinItems.AddRange(
                stmnt
                .Joins
                .Select(
                    jn => new SqlJoin() {
                        RhsTable = qry.Tables[jn.JoinTable.Identifier],
                        SqlJoinType = jn.JoinType,
                        Criteria = buildWhere(qry, jn),
                    }
                )
            );

            var joinedTbls = new List<string>();
            joinedTbls.AddRange(qry.JoinItems.Select(jn => jn.LhsTable).Where(tbl => tbl != null).Select(tbl => tbl.Identifier));
            joinedTbls.AddRange(qry.JoinItems.Select(jn => jn.RhsTable).Where(tbl => tbl != null).Select(tbl => tbl.Identifier));

            string tbl1Ident = "";
            if(qry.Tables.Count > 0) {
                tbl1Ident = qry.Tables.First().Value.Identifier;
            }

            var unJoinedTbls = qry.Tables.Keys.Where(ky => !joinedTbls.Contains(ky) && ky != tbl1Ident);
            unJoinedTbls.ForEach(
                tblName =>
                    qry.JoinItems.Add(new SqlJoin() {
                    RhsTable = qry.Tables[tblName],
                    SqlJoinType = "FULL OUTER JOIN"
            }));
            return qry;
        }


        private static SqlQuery readTables(SqlQuery qry, Sql.sqlStatement stmnt) {
            Debug.Assert(stmnt != null);

            List<SqlTable> tbls = new List<SqlTable>();

            //Reads Table1
            tbls.Add(new SqlTable() {
                Schema = getOrBuildSchema(qry, stmnt.Table1.SchemaName),
                AliasName = stmnt.Table1.AliasName.getVal(),
                TableName = stmnt.Table1.TableName
            });

            //Gets the rest of the tables from the joins
            tbls.AddRange(stmnt.Joins.Select(jn => new SqlTable() {
                AliasName = jn.JoinTable.AliasName.getVal(),
                Schema = getOrBuildSchema(qry, jn.JoinTable.SchemaName),
                TableName = jn.JoinTable.TableName
            }));

            foreach(SqlTable tbl in tbls) {
                if(!qry.Tables.ContainsKey(tbl.Identifier)) {
                    qry.Tables.Add(tbl.Identifier, tbl);
                }
            }

            return qry;
        }

        private static Sql.sqlStatement Parse(string SQLString) {
            Sql.sqlStatement stmnt;
            try {
                stmnt = Parser.ParseSql(SQLString);
            } catch(Exception) {
                //TODO catch the rigth exception here
                //I dont want to throw the parse exception because it would introduce a dependencies between the model and the parser
                throw new Exception("Parse Error");
            }
            return stmnt;
        }

        private static SqlWhere buildWhere(SqlQuery qry, Sql.join jn) {
            if(FSharpOption<Sql.cond>.get_IsSome(jn.Where)) {
                return buildWhere(qry, jn.Where.Value);
            } else {
                return null;
            }
        }

        private static SqlWhere buildWhere(SqlQuery qry, Sql.cond cond) {
            if(cond == null)
                return null;
            if(cond.isValue) {
                throw new Exception("Didn't expect value");
            }

            Sql.where whr = cond.Condition.Value;
            SqlValue lft;
            SqlValue rgt;
            if(whr.Left.isValue) {
                lft = getOrBuildValue(qry, whr.Left.Value.Value, false);
            } else {
                lft = buildWhere(qry, whr.Left);
            }

            if(whr.Right.isValue) {
                rgt = getOrBuildValue(qry, whr.Right.Value.Value, false);
            } else {
                rgt = buildWhere(qry, whr.Right);
            }

            return new SqlWhere() {
                LeftOperand = lft,
                RightOperand = rgt,
                Operator = whr.Operator
            };
        }

        private static SqlValue getOrBuildValue(SqlQuery qry, Sql.value vlIn, bool makeVisible) {
            Debug.Assert(vlIn != null);
            //Going to build the value anyway. Throw it away if it existed.
            SqlValue val = buildValue(qry, vlIn, makeVisible);

            if(!qry.Columns.Contains(val, new SqlValue.SqlValueComparer())) {
                qry.Columns.Add(val);
            }

            Debug.Assert(val.IsSelected == makeVisible);
            return val;
        }

        private static SqlValue buildValue(SqlQuery qry, Sql.value vlIn, bool isVisible) {
            Debug.Assert(vlIn != null);

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
                    Parameters = vl.Params.Value.Select(prm => getOrBuildValue(qry, prm, false)).ToList(),
                    Schema = getOrBuildSchema(qry, vl.Schema),
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

        private static SqlSchema getOrBuildSchema(SqlQuery qry, FSharpOption<Sql.schema> schemaOp) {
            if(FSharpOption<Sql.schema>.get_IsSome(schemaOp)) {
                return getOrBuildSchema(qry, schemaOp.Value.Name);
            } else {
                return null;
            }
        }

        private static SqlSchema getOrBuildSchema(SqlQuery qry, string schemaName) {
            Debug.Assert(qry != null);

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
