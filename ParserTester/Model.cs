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
        public List<SqlTable> Tables { get; set; }
        public List<SqlWhere> WhereItems { get; set; }
        public List<SqlOrderBy> OrderByColumns { get; set; }
        public List<SqlJoin> JoinItems { get; set; }

        #endregion

        #region ctor

        public SqlQuery()
        {
            this.Tables = new List<SqlTable>();
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
        public SqlTable LeftOperand { get; set; }
        public string SqlJoinType { get; set; }
        public SqlTable RightOperand { get; set; }
        public SqlWhere Criteria { get; set; }
    };

}
