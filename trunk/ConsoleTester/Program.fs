open System

let x = "   
        SELECT top 1 t1.x, fun(y) as funny, dbo.fun(y)
        FROM t1   
        LEFT JOIN t2
        INNER JOIN t3 ON t3.ID = t2.ID   
        WHERE t1.x = 50 AND y = 20    
        ORDER BY x ASC, y DESC, z   
    "   


Module1.parseit x

Console.ReadKey(true) |> ignore



