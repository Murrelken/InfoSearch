using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfoSearch
{
    // софист материал 16: 0,003565 25: 0,00343
    public class VectorSearch
    {
        private Dictionary<string, double> EmptyTfidf => GetEmptyTfidf();
        const string IDFPath = "D:/InfoSearch/TFAndIDF/IDF.txt";
        const string TFIDFPath = "D:/InfoSearch/TFAndIDF/TFIDF";
        
        public void Run()
        {
            Console.WriteLine("Waiting for vector search expressions. Type \"Exit\" to exit.");

            Console.WriteLine();

            var readLine = string.Empty;

            while (readLine != "Exit")
            {
                readLine = (Console.ReadLine() ?? "").Trim();

                var words = readLine.Split(" ");

                var searchTfIdf = EmptyTfidf;
                
                foreach (var word in words)
                {
                    searchTfIdf[word] = (double) 1 / words.Length;
                }

                var results = new List<ResultType>();
                
                foreach (var i in Enumerable.Range(1, 100))
                {
                    double multiplicationResult = 0;
                    
                    var tfidfs = File.ReadAllLines($"{TFIDFPath}/{i}.txt");

                    foreach (var tfidf in tfidfs)
                    {
                        var splitted = tfidf.Split("\t");
                        var word = splitted[0];
                        var tfidfValue = Math.Round(Convert.ToDouble(splitted[1]), 5);

                        multiplicationResult += Math.Round(searchTfIdf[word] * tfidfValue, 5);
                    }

                    results.Add(new ResultType(i, multiplicationResult));
                }
                
                foreach (var resultType in results.OrderByDescending(x => x.RelevanceScore))
                {
                    Console.Write(resultType.DocumentNumber + " ");
                }

                Console.WriteLine();
            }
        }

        static Dictionary<string, double> GetEmptyTfidf()
        {
             var res = new Dictionary<string, double>();
             
             var idfs = File.ReadAllLines(IDFPath);

             foreach (var idf in idfs)
             {
                 res.Add(idf.Split("\t")[0], 0);
             }

             return res;
        }
    }

    public class ResultType
    {
        public int DocumentNumber { get; set; }
        
        public double RelevanceScore { get; set; }

        public ResultType(int documentNumber, double relevanceScore)
        {
            DocumentNumber = documentNumber;
            RelevanceScore = relevanceScore;
        }
    }
}