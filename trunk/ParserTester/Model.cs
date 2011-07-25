using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserTester;

namespace ParserTester
{
    public class SqlQuery
    {
        #region Properties

        public string Top { get; set; }
        public Dictionary<string, SqlTable> Tables { get; set; }
        public Dictionary<string, SqlSchema> Schemas { get; set; }
        public List<SqlWhere> WhereItems { get; set; }
        public List<SqlOrderBy> OrderByColumns { get; set; }
        public List<SqlJoin> JoinItems { get; set; }
        public List<SqlValue> Columns { get; set; }
        #endregion

        #region ctor

        public SqlQuery()
        {
            this.Tables = new Dictionary<string, SqlTable>();
            this.WhereItems = new List<SqlWhere>();
            this.OrderByColumns = new List<SqlOrderBy>();
            this.Columns = new List<SqlValue>();
        }

        #endregion
    }


    public class SqlTable
    {
        public SqlSchema Schema { get; set; }
        public string AliasName { get; set; }
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
    }

    public class SqlSchema
    {
        public string SchemaName { get; set; }
    }

    public class SqlWhere : SqlValue
    {
        public SqlValue LeftOperand { get; set; }
        public string Operator { get; set; }
        public SqlValue RightOperand { get; set; }

        //public SqlTable getLeftTable(SqlTable rightTable) {
        //    SqlField fld = this.LeftOperand as SqlField;
        //    if(fld != null) {
        //        if(fld.Table != null) {
        //            if(!fld.Table.Equals(rightTable)) {
        //                return fld.Table;
        //            }
        //        }
        //    }

        //    fld = this.RightOperand as SqlField;
        //    if(fld != null) {
        //        if(fld.Table != null) {
        //            if(!fld.Table.Equals(rightTable)) {
        //                return fld.Table;
        //            }
        //        }
        //    }
        //    return null;
        //}
    }

    public class SqlOrderBy
    {
        public SqlValue Column { get; set; }
        public string Dir { get; set; }
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
                //TODO but how?
                //LhsTable = value.getLeftTable(RhsTable);
            }
            get {
                return this.criteria;
            }
        }
    }

    public abstract class SqlValue {
        public string Alias { get; set; }
        protected string value;
        public override string ToString() {
            if (!String.IsNullOrEmpty(value)){
                return value + " AS " + Alias;
            } else {
                return value;
            }
        }
        public bool IsSelected { get; set; } 
    }

    public class SqlString : SqlValue {
        public string Str {
            get { return base.value; }
            set { base.value = value; }
        }
    }

    public class SqlInt : SqlValue {
        public string Int { 
            get { return base.value; }
            set { base.value = value; }
        }
    }

    public class SqlFloat : SqlValue {
        public string Float {
            get { return base.value; }
            set { base.value = value; }
        }
    }

    public class SqlField : SqlValue {
        public string Field {
            get { return base.value; }
            set { base.value = value; }
        }
        public SqlTable Table { get; set; }
    }

    public class SqlFunction : SqlValue {
        public string FunctionName {
            get { return base.value; }
            set { base.value = value; }
        }
        public SqlSchema Schema { get; set; }
        public List<SqlValue> Parameters { get; set; }
        public override string ToString() {
            string strParams = Parameters.Aggregate("", (sqlVal, acc) => sqlVal.ToString() + ", ");
            string sch = "";
            if(Schema != null) {
                sch = Schema.SchemaName + ".";
            }
            if(strParams.Length > 2) {
                return sch + this.FunctionName + "(" + strParams.Substring(0, strParams.Length - 2) + ")";
            } else {
                return sch + this.FunctionName + "()";
            }
        }
    }
}
