using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfoSearch
{
    public class TFIDFCalculator
    {
        const string TFIDFAggregatedPath = "D:/InfoSearch/TFAndIDF/AggregatedTFIDF";
        const string TFIDFFilePath = "D:/InfoSearch/TFAndIDF/AggregatedTFIDF/TFIDF.txt";
        const string IDFPath = "D:/InfoSearch/TFAndIDF/IDF.txt";
        const string TFIDFPath = "D:/InfoSearch/TFAndIDF/TFIDF";
        private static Dictionary<string, double[]> SingleTFIDF = new Dictionary<string, double[]>();
        
        public void Run()
        {
            ClearDirectories();
            
            InitializeDictionary();
            
            FillDictionary();

            WriteAggregatedTfidf();
        }

        static void WriteAggregatedTfidf()
        {
            using var file = new StreamWriter(TFIDFFilePath, true);
            foreach (var (key, value) in SingleTFIDF)
            {
                var pages = string.Join(" ", value);
                file.WriteLine($"{key}\t{pages}");
            }
        }

        static void FillDictionary()
        {
            foreach (var i in Enumerable.Range(1, 100))
            {
                var tfidfs = File.ReadAllLines($"{TFIDFPath}/{i}.txt");

                foreach (var tfidf in tfidfs)
                {
                    var splitted = tfidf.Split("\t");
                    var word = splitted[0];
                    var tfidfValue = Math.Round(Convert.ToDouble(splitted[1]), 5);

                    SingleTFIDF[word][i - 1] = tfidfValue;
                }
            }
        }

        private static void InitializeDictionary()
        {
            var idfs = File.ReadAllLines(IDFPath);

            foreach (var idf in idfs)
            {
                SingleTFIDF.Add(idf.Split("\t")[0], new double[100]);
            }
        }

        static void ClearDirectories()
        {
            var TFIDFDirectoryPath = new DirectoryInfo(TFIDFAggregatedPath);

            foreach (var file in TFIDFDirectoryPath.GetFiles())
            {
                file.Delete();
            }
            
            using (File.Create(TFIDFFilePath))
            {
            }
        }
    }
}