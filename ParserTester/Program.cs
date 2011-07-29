﻿using System;
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
                SELECT top 1 percent blaa.t1.x, t1.q as t1q, fun(y) as funny, dbo.fun(y)
                FROM blaa.t1   
                LEFT JOIN t2
                INNER JOIN t3 as bla ON bla.ID = t2.ID  
                WHERE t1.x = 50 AND y = 20    
                ORDER BY x ASC, y DESC, z   
            ";

            // YOU SHOULD TRY RIGHTCLICK ON A F# FUNCTION AND THEN GO TO DEFINITION   -> WOW WTF


            Sql.sqlStatement stmnt = Parser.ParseSql(query);
            ModelBuilder bld = new ModelBuilder();

            SqlQuery qry = bld.build(query);
            Console.Write(query + "\n\n");
            Console.Write(qry.ToString());
            Console.ReadKey();
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
