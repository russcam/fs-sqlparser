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
        public SqlTable(SqlSchema schema, string alias, string tableName, List<SqlColumn> columns) {
            this.Schema = schema;
            this.Alias = alias;
            this.TableName = tableName;
            this.Columns = columns;
        }

        public SqlSchema Schema { get; set; }
        public string Alias { get; set; }
        public string TableName { get; set; }
        public IEnumerable<SqlColumn> Columns { get; set; }
    };
    public class SqlSchema
    {
        public SqlSchema(string schemaName) {
            this.SchemaName = schemaName;
        }
        public string SchemaName { get; set; }
    };
    public class SqlColumn
    {
        public SqlColumn(string alias, string columnName) {
            this.Alias = alias;
            this.ColumnName = columnName;
        }
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
