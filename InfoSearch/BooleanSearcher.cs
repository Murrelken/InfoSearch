using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoSearch
{
    public class BooleanSearcher
    {
        public void Run()
        {
            var pagesByTerm = new Dictionary<string, List<int>>();
            var lines = System.IO.File.ReadAllLines("D:/InfoSearch/Index.txt");

            foreach (var line in lines)
            {
                var splitted = line.Split("\t");
                var key = splitted[0];
                var pages = splitted.Last().Split(" ").Select(x => Convert.ToInt32(x)).ToList();
                
                pagesByTerm.Add(key, pages);
                
            }

            Console.WriteLine("Waiting for bool search expressions. Type \"Exit\" to exit.");

            var readLine = string.Empty;

            while (readLine != "Exit")
            {
                readLine = (Console.ReadLine() ?? "").Trim();

                var toPerformOr = new Stack<TypeForBooleanSearchOperations>();
                foreach (var expr in readLine.Split("|"))
                {
                    var toPerformAnd = new Stack<TypeForBooleanSearchOperations>();
                    foreach (var expr2 in expr.Split("&"))
                    {
                        var isTrue = expr2[0] != '!';
                        var term = expr2.Replace("!", "");
                        pagesByTerm.TryGetValue(term, out var pages);
                        toPerformAnd.Push(new TypeForBooleanSearchOperations(pages ?? new List<int>(), isTrue));
                    }

                    var resultToPerformOr = toPerformAnd.Pop();

                    while (toPerformAnd.TryPop(out var anotherOptionToAnd))
                    {
                        resultToPerformOr =
                            TypeForBooleanSearchOperations.BooleamnAmd(resultToPerformOr, anotherOptionToAnd);
                    }

                    toPerformOr.Push(resultToPerformOr);
                }

                var resultPerformed = toPerformOr.Pop();

                while (toPerformOr.TryPop(out var anotherOptionToAnd))
                {
                    resultPerformed =
                        TypeForBooleanSearchOperations.BooleanOr(resultPerformed, anotherOptionToAnd);
                }

                var result = resultPerformed.Pages;

                if (result == null || result.Count == 0)
                    result = new List<int>() {0};

                Console.WriteLine("Result: ");
                foreach (var page in result)
                {
                    Console.Write(page + " ");
                }

                Console.WriteLine();
            }
        }
    }
}