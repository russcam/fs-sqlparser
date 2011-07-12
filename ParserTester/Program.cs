using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
//using Microsoft.FSharp.Collections;

namespace ParserTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var query =
            @"   
                SELECT top 1 percent blaa.t1.x, fun(y) as funny, dbo.fun(y)
                FROM t1   
                LEFT JOIN t2
                INNER JOIN t3 as bla ON bla.ID = t2.ID  
                WHERE t1.x = 50 AND y = 20    
                ORDER BY x ASC, y DESC, z   
            ";


            Sql.sqlStatement stmnt = Module1.ParseSql(query);
            SqlObject so = new SqlObject() { Top = stmnt.TopN.Value.Name, TableName = stmnt.Table1.Name };
            
            var columns = stmnt.Columns.ToList();
            foreach (var item in columns)
            {
                var col = item;
                so.Columns.Add(col.Name);
            }
        }

        //public static IEnumerable<TItemType> ToEnumerable<TItemType>(this FSharpList<TItemType> fList)
        //{
        //    return Microsoft.FSharp.Collections.SeqModule.OfList<TItemType>(fList);
        //}
    };

    public class SqlObject
    {
        #region Properties

        public string Top { get; set; }
        public string TableName { get; set; }
        public List<string> Columns { get; set; }

        #endregion

        #region ctor

        public SqlObject()
        {
            this.Columns = new List<string>();
        }

        #endregion
    };

}
