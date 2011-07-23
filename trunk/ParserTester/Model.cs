using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        #endregion

        #region ctor

        public SqlQuery()
        {
            this.Tables = new Dictionary<string, SqlTable>();
            this.WhereItems = new List<SqlWhere>();
            this.OrderByColumns = new List<SqlOrderBy>();
        }

        #endregion
    };


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
        public IEnumerable<SqlColumn> Columns { get; set; }
        public IEnumerable<SqlJoin> JoinItems { get; set; }
    };
    public class SqlSchema
    {
        public string SchemaName { get; set; }
    };
    public class SqlColumn
    {
        public string Alias { get; set; }
        public string ColumnName { get; set; }
    };
    public class SqlWhere
    {
        public string LeftOperand { get; set; }
        public string Operator { get; set; }
        public string RightOperand { get; set; }
    }
    public class SqlOrderBy
    {
        public SqlColumn Column { get; set; }
        public string Dir { get; set; }
    };
    public class SqlJoin
    {
        public SqlTable LhsTable { get; set; }
        public string SqlJoinType { get; set; }
        public SqlTable RhsTable { get; set; }
        public SqlWhere Criteria { get; set; }
    };

    abstract class SqlValue {
        public string Alias { get; set; }
        protected string value;
        public override string ToString() {
            if (!String.IsNullOrEmpty(value)){
                return value + " AS " + Alias;
            } else {
                return value;
            }
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
    }

    public class SqlFunction : SqlValue {
        public string FunctionName {
            get { return base.value; }
            set { base.value = value; }
        }
        public string SchemaName { get; set; }
        public List<SqlValue> Parameters { get; set; }
        public override string ToString() {
            string strParams = Parameters.Aggregate("", (sqlVal, acc) => sqlVal.ToString() + ", ");
            string sch = "";
            if(!String.IsNullOrEmpty(SchemaName)) {
                sch = SchemaName + ".";
            }
            if(strParams.Length > 2) {
                return sch + this.FunctionName + "(" + strParams.Substring(0, strParams.Length - 2) + ")";
            } else {
                return sch + this.FunctionName + "()";
            }
        }
    }
}
