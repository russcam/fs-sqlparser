using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserTester {
    class ModelBuilder {
        //I introduced this class so that only this class has dependencies on the F# project
        private Dictionary<string, SqlSchema> schemas = new Dictionary<string,SqlSchema>();

        public SqlQuery build(string SQLString) {
            Sql.sqlStatement stmnt;
            try {
                stmnt = Parser.ParseSql(SQLString);
            } catch(Exception) {
                //TODO catch the rigth exception here
                //I dont want to throw the parse exception because it would introduce a dependencies between the model and the parser
                throw new Exception("Parse Error");
            }
            SqlQuery qry = new SqlQuery();
            qry.Tables.AddRange(stmnt.Tables.Select(tbl => buildTable(tbl, stmnt)));
            
            //TODO Add rest
            return qry;
        }

        private SqlTable buildTable(Sql.table tbl, Sql.sqlStatement stmnt) {
            string tblOrAliasName;

            if (!String.IsNullOrEmpty(tbl.AliasName)) {
                tblOrAliasName = tbl.AliasName;
            } else {
                tblOrAliasName = tbl.Name;
            }

            return new SqlTable(
                getSchema(tbl.SchemaName), 
                tbl.AliasName, 
                tbl.Name, 
                stmnt
                    .getTableFields(tblOrAliasName)
                    .Select(fld => new SqlColumn(fld.Item2, fld.Item1))
                    .ToList()
            );
        }

        private SqlColumn buildColumn(Tuple<string, string> tup) {
            return new SqlColumn(tup.Item2, tup.Item1);
        }

        private SqlSchema getSchema(string schemaName) {
            if (String.IsNullOrEmpty(schemaName)){
                return null;   
            }
            if(schemas.ContainsKey(schemaName)) {
                return schemas[schemaName];
            } else {
                SqlSchema nw = new SqlSchema(schemaName);
                schemas.Add(schemaName, nw);
                return nw;
            }
        }
    }
}
