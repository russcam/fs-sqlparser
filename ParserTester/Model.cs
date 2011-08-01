using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserTester;
using System.Diagnostics;

namespace ParserTester
{
    public enum SqlValueTypes {
        Function,
        Int,
        String,
        Float,
        Field,
        Where
    }

    public class SqlQuery
    {
        #region Properties

        public SqlTop Top { get; set; }
        public Dictionary<string, SqlTable> Tables { get; set; }
        public Dictionary<string, SqlSchema> Schemas { get; set; }
        public SqlWhere Where { get; set; }
        public List<SqlOrderBy> OrderByColumns { get; set; }
        public List<SqlJoin> JoinItems { get; set; }
        public List<SqlValue> Columns { get; set; }
        #endregion

        #region ctor

        public SqlQuery()
        {
            this.Tables = new Dictionary<string, SqlTable>();
            this.Schemas = new Dictionary<string, SqlSchema>();
            this.OrderByColumns = new List<SqlOrderBy>();
            this.Columns = new List<SqlValue>();
            this.JoinItems = new List<SqlJoin>();
        }

        public override string ToString() {
            //Just make sure the lists were initialised
            Debug.Assert(this.OrderByColumns != null);
            Debug.Assert(this.Columns != null);
            Debug.Assert(this.OrderByColumns != null);
            Debug.Assert(this.Tables != null);

            SqlTable tbl1 = null;
            string ob = "";
            if(this.Tables.Count > 0) {
                tbl1 = this.Tables.First().Value;
            }
            if(this.OrderByColumns.Count > 0) {
                ob = "\nORDER BY\n\t" + this.OrderByColumns.ToString2(",\n\t");
            }

            return "SELECT\n" 
                + str.getR(this.Top, "\n\t") 
                + Columns.Where(col => col.IsSelected).ToString2(",\n\t")
                + "\nFROM\n\t"
                + str.getR(tbl1, "\n\t")
                + JoinItems.ToString2("\n\t") 
                + str.getL(this.Where, "\nWHERE\n\t")
                + ob;
        }

        #endregion
    }

    static class str {
        public static string getR(object obj, string rest = ""){
            if (obj == null){ return ""; }
            return obj.ToString() + rest;
        }

        public static string getL(object obj, string rest = "") {
            if(obj == null) { return ""; }
            return rest + obj.ToString();
        }
    }

    public class SqlTop {
        public string N { get; set; }
        public string type { get; set; }

        public override string ToString() {
            return "TOP " + this.N + " " + type;
        }
    }

    public class SqlTable
    {
        public SqlSchema Schema { get; set; }
        public String AliasName { get; set; }
        public string TableName { get; set; }
        public string Identifier { 
            //Dont want to store this one because this way we can add an alias later on without any possible problems
            get { 
                if (!String.IsNullOrEmpty(AliasName)){
                    return AliasName;
                } else {
                    return TableName;
                }
            }
        }

        public override string ToString() {
            return str.getR(this.Schema, ".") + this.TableName + str.getL(this.AliasName, " AS ");
        }
    }

    public class SqlSchema
    {
        public string SchemaName { get; set; }

        public override string ToString() {
            return this.SchemaName;
        }
    }

    public class SqlWhere : SqlValue
    {
        public SqlValue LeftOperand { get; set; }
        public string Operator { get; set; }
        public SqlValue RightOperand { get; set; }

        public SqlTable getLeftTable(SqlTable rightTable) {
            SqlTable tbl = getTable(LeftOperand as SqlField, rightTable);
            if(tbl != null) { return tbl; }
            tbl = getTable(RightOperand as SqlField, rightTable);

            return null;
        }
        private static SqlTable getTable(SqlField fld, SqlTable rightTable) {
            if(rightTable == null) { throw new Exception("Right table is empty"); }
            if(fld == null) { return null; }
            if(fld.Table == null) { return null; }

            if(!rightTable.Equals(fld.Table)) {
                return fld.Table;
            }
            return null;
        }

        public override string ToString() {
            return str.getR(this.LeftOperand) + " " + this.Operator + " " + str.getR(this.RightOperand);
        }

        public override SqlValueTypes ValueType() { return SqlValueTypes.Where; }
    }

    

    public class SqlOrderBy
    {
        public SqlValue Column { get; set; }
        public string Dir { get; set; }

        public override string ToString() {
            return this.Column.ToString() + " " + Dir;
        }
    }

    public class SqlJoin
    {
        public SqlTable LhsTable { get; set; }
        public string SqlJoinType { get; set; }
        public SqlTable RhsTable { get; set; }
        private SqlWhere criteria;
        public SqlWhere Criteria { 
            set {
                this.criteria = value;
                if(value != null) {
                    LhsTable = value.getLeftTable(RhsTable);
                }
            }
            get {
                return this.criteria;
            }
        }

        public override string ToString() {
            return this.SqlJoinType + " " + this.RhsTable + str.getL(this.Criteria, " ON ") + " ";
        }
    }

    public abstract class SqlValue {
        public string Alias { get; set; }
        protected string value;
        public override string ToString() {
            return value + str.getL(this.Alias, " AS ");
        }
        public bool IsSelected { get; set; }
        public abstract SqlValueTypes ValueType();

        public class SqlValueComparer : IEqualityComparer<SqlValue> {
           public bool Equals(SqlValue x, SqlValue y) {
                if(x.ValueType() != y.ValueType())
                    return false;
                if(x.value != y.value)
                    return false;

                var valTyp = x.ValueType();

                if(valTyp == SqlValueTypes.Float
                    || valTyp == SqlValueTypes.Int
                    || valTyp == SqlValueTypes.String) {
                        return true;
                }

                return x.ToString() == y.ToString();
            }

           public int GetHashCode(SqlValue obj) {
               return 1;
           }
        }
    }

    public class SqlString : SqlValue {
        public string Str {
            get { return base.value; }
            set { base.value = value; }
        }
        public override SqlValueTypes ValueType() { return SqlValueTypes.String; }
    }

    public class SqlInt : SqlValue {
        public string Int { 
            get { return base.value; }
            set { base.value = value; }
        }
        public override SqlValueTypes ValueType() { return SqlValueTypes.Int; }
    }

    public class SqlFloat : SqlValue {
        public string Float {
            get { return base.value; }
            set { base.value = value; }
        }
        public override SqlValueTypes ValueType() { return SqlValueTypes.Float; }
    }

    public class SqlField : SqlValue {
        public string Field {
            get { return base.value; }
            set { base.value = value; }
        }

        public string TblIdentifier {
            get { if(this.Table != null) { return this.Table.Identifier; }
                return null;
            }
        }
        public SqlTable Table { get; set; }
        public override string ToString() {
            string tbl = "";
            if(this.Table != null) { 
                tbl = str.getR(this.Table.Schema, ".") + this.Table.Identifier + "."; 
            }
            return tbl + base.ToString();
        }

        public override SqlValueTypes ValueType() { return SqlValueTypes.Field; }
    }

    public class SqlFunction : SqlValue {
        public string FunctionName {
            get { return base.value; }
            set { base.value = value; }
        }
        public SqlSchema Schema { get; set; }
        public List<SqlValue> Parameters { get; set; }
        public override string ToString() {
            string prms = "";
            if (this.Parameters != null) { prms = this.Parameters.ToString2(", "); }
            return str.getR(this.Schema, ".") + this.FunctionName + "(" + prms + ")";
        }

        public override SqlValueTypes ValueType() { return SqlValueTypes.Function; }
    }

}
