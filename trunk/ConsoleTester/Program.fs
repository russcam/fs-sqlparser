open System

let x = "   
        SELECT top 1 percent blaa.t1.x, fun(y) as funny, dbo.fun(y)
        FROM t1   
        LEFT JOIN t2
        INNER JOIN t3 as bla ON bla.ID = t2.ID  
        WHERE t1.x = 50 AND y = 20    
        ORDER BY x ASC, y DESC, z   
    "   

//let x = "   
//        SELECT x.y, x.y.z, fun(y), dbo.fun(y), dbo.fun(y) as funny
//        FROM q.x as v
//    " 

Parser.parseit x

Console.ReadKey(true) |> ignore



