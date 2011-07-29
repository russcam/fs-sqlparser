using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserTester
{
    static class Ext
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action(element);
            return source;
        }

        //Cant override the interface otherwise would have done that
        public static string ToString2(this IEnumerable<object> source, string sep = ""){
            string agg = "";
            foreach(object element in source) {
                agg += element.ToString() + sep;
            }

            if(agg.Length > 1 && sep.Length > 0) {
                return agg.Substring(0, agg.Length - sep.Length);
            } else {
                return agg;
            }
        }
    }

}
