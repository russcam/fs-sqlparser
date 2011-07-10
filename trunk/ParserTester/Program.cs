using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = @"   
        SELECT top 1 percent blaa.t1.x, fun(y) as funny, dbo.fun(y)
        FROM t1   
        LEFT JOIN t2
        INNER JOIN t3 as bla ON bla.ID = t2.ID  
        WHERE t1.x = 50 AND y = 20    
        ORDER BY x ASC, y DESC, z   
    ";   


           Sql.sqlStatement stmnt =  Module1.ParseSql(query);;
            //Sql.ISql temp = (Sql.ISql)stmnt.Table1;
            //temp.toSql();
        }
    }
}
