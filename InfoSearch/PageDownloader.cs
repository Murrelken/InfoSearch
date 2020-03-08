using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LemmaSharp;
using mshtml;

namespace InfoSearch
{
    public class PageDownloader
    {
        private const string StartUrl = "https://ru.wikipedia.org/wiki/%D0%A1%D0%BE%D1%84%D0%B8%D1%81%D1%82%D1%8B";
        const string BaseDomain = "https://ru.wikipedia.org";
        const string SavedPagesFilePath = "D:/InfoSearch/SavedPages.txt";
        const string IndexFilePath = "D:/InfoSearch/Index.txt";
        const string BaseDirectoryPath = "D:/InfoSearch";
        const string PagesDirectoryPath = "D:/InfoSearch/Pages";
        const string LemmatizedPagesDirectoryPath = "D:/InfoSearch/LemmatizedPages";
        const string TFAndIDFPath = "D:/InfoSearch/TFAndIDF";
        const string TFPath = "D:/InfoSearch/TFAndIDF/TF";
        const string TFIDFPath = "D:/InfoSearch/TFAndIDF/TFIDF";
        const string IDFPath = "D:/InfoSearch/TFAndIDF/IDF.txt";
        static Queue<string> LinksQueue = new Queue<string>();
        private static Dictionary<string, int> SavedPages = new Dictionary<string, int>();
        private static int _fileNumber = 1;
        private static LemmatizerPrebuiltFull Lemmatizer;
        private static Dictionary<string, HashSet<int>> PagesByTerm = new Dictionary<string, HashSet<int>>();

        public async Task Run()
        {
            Lemmatizer = new LemmatizerPrebuiltFull(LanguagePrebuilt.Russian);

            var t = new Stopwatch();
            t.Start();

            ClearFileAndInitializeQueue();

            while (!IsTimeToStop() && LinksQueue.TryDequeue(out var nextUrl))
            {
                await ReadNewPage(nextUrl);
            }

            WriteIndexToFile();

            CountIDF();

            t.Stop();
            Console.WriteLine($"\n{t.ElapsedMilliseconds}");
        }

        static void CountIDF()
        {
            var idfs = new Dictionary<string, double>();
            using var file = new StreamWriter(IDFPath, true);
            foreach (var (key, value) in PagesByTerm)
            {
                var idf = Math.Round((double) 100 / value.Count, 5);
                idfs.Add(key, idf);
                file.WriteLine($"{key}\t{idf}");
            }

            foreach (var i in Enumerable.Range(1, 100))
            {
                var tfs = File.ReadAllLines($"{TFPath}/{i}.txt");

                using var sw = File.CreateText($"{TFIDFPath}/{i}.txt");

                foreach (var tf in tfs)
                {
                    var splitted = tf.Split("\t");
                    var word = splitted[0];
                    var tfValue = Convert.ToDouble(splitted[1]);

                    var tfIdf = Math.Round(idfs[word] * tfValue, 5);

                    sw.WriteLine($"{word}\t{tfIdf}");
                }
            }
        }

        static void WriteIndexToFile()
        {
            using var file = new StreamWriter(IndexFilePath, true);
            foreach (var (key, value) in PagesByTerm)
            {
                var pages = string.Join(" ", value);
                file.WriteLine($"{key}\t{pages}");
            }
        }

        static void ClearFileAndInitializeQueue()
        {
            var baseDirectoryPath = new DirectoryInfo(BaseDirectoryPath);
            var pagesDirectoryPath = new DirectoryInfo(PagesDirectoryPath);
            var lemmatizedPagesDirectoryPath = new DirectoryInfo(LemmatizedPagesDirectoryPath);
            var TFAndIDFDirectoryPath = new DirectoryInfo(TFAndIDFPath);
            var TFDirectoryPath = new DirectoryInfo(TFPath);

            foreach (var file in baseDirectoryPath.GetFiles())
            {
                file.Delete();
            }

            foreach (var file in pagesDirectoryPath.GetFiles())
            {
                file.Delete();
            }

            foreach (var file in lemmatizedPagesDirectoryPath.GetFiles())
            {
                file.Delete();
            }

            foreach (var file in TFAndIDFDirectoryPath.GetFiles())
            {
                file.Delete();
            }

            foreach (var file in TFDirectoryPath.GetFiles())
            {
                file.Delete();
            }

            LinksQueue.Enqueue(StartUrl);

            using (File.Create(SavedPagesFilePath))
            {
            }

            using (File.Create(IndexFilePath))
            {
            }

            using (File.Create(IDFPath))
            {
            }
        }

        static bool IsTimeToStop()
            => SavedPages.Count >= 100;

        static async Task ReadNewPage(string url)
        {
            if (SavedPages.ContainsKey(url))
                return;

            var html = await GetFromHttpClient(url);

            var text = HtmlToString(html);

            var words = text
                .ToLower()
                .Split(" ")
                .Where(x => !string.IsNullOrEmpty(x))
                .Where(x => x != "·")
                .Where(x => x != "—")
                .ToArray();

            var wordsCount = words.Length;

            if (wordsCount < 1000)
                return;

            try
            {
                var lemmatizedWords = LemmatizeWordsArray(words)
                    .ToArray();

                var frequencyByTerm = new Dictionary<string, int>();

                foreach (var lemmatizedWord in lemmatizedWords)
                {
                    if (PagesByTerm.TryGetValue(lemmatizedWord, out var pages))
                    {
                        pages.Add(_fileNumber);
                    }
                    else
                    {
                        PagesByTerm.Add(lemmatizedWord, new HashSet<int>() {_fileNumber});
                    }

                    if (frequencyByTerm.TryGetValue(lemmatizedWord, out var count))
                    {
                        frequencyByTerm[lemmatizedWord] = ++count;
                    }
                    else
                    {
                        frequencyByTerm.Add(lemmatizedWord, 1);
                    }
                }

                await using var swTF = File.CreateText($"{TFPath}/{_fileNumber}.txt");
                foreach (var (key, count) in frequencyByTerm)
                {
                    var tf = Math.Round((double) count / wordsCount, 5);
                    swTF.WriteLine($"{key}\t{tf}");
                }

                await using var sw = File.CreateText($"{LemmatizedPagesDirectoryPath}/{_fileNumber}.txt");
                sw.WriteLine(string.Join(" ", lemmatizedWords));
            }
            catch (Exception e)
            {
                Console.WriteLine("\nUnexpected Exception in lemmatization");
                Console.WriteLine($"Message :{e.Message}");
                return;
            }

            await using (var file = new StreamWriter(SavedPagesFilePath, true))
            {
                file.WriteLine($"{url} {_fileNumber}");
                SavedPages.TryAdd(url, _fileNumber);
            }

            await using (var sw = File.CreateText($"{PagesDirectoryPath}/{_fileNumber}.txt"))
            {
                sw.WriteLine(text);
            }

            _fileNumber++;

            var allLinks = GetAllLinks(html);
            foreach (var link in allLinks)
            {
                LinksQueue.Enqueue(link);
            }
        }

        static string[] GetAllLinks(string html)
        {
            var regexp = "href=\"[^\\\"]*\"";

            var allLinks = Regex
                .Matches(html, regexp)
                .Select(x => x.Value.Replace("href=\"", "").Replace("\"", ""))
                .Where(x => !x.StartsWith("http") || x.StartsWith(BaseDomain))
                .Where(x => !x.StartsWith("#"))
                .Select(x => x.StartsWith("http") ? x : BaseDomain + x)
                .Where(x =>
                    !x.Contains(".php") &&
                    !x.Contains(".css") &&
                    !x.Contains(".js") &&
                    !x.Contains(".pdf") &&
                    !x.Contains(".ttf") &&
                    !x.Contains(".wasm") &&
                    !x.Contains(".jpg") &&
                    !x.Contains(".jpeg") &&
                    !x.Contains(".png") &&
                    !x.Contains(".bmp") &&
                    !x.Contains(".ico"))
                .ToArray();

            return allLinks;
        }

        static string HtmlToString(string html)
        {
            if (html == "")
                return "";

            var indexOfHead = html.IndexOf("</head>");
            var index = indexOfHead + 8;
            var resultHtml = html.Substring(index);

            HTMLDocument htmldoc = new HTMLDocument();
            IHTMLDocument2 htmldoc2 = (IHTMLDocument2) htmldoc;
            htmldoc2.write(new object[] {resultHtml});

            return htmldoc2
                .body
                .outerText
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "");
        }

        static readonly HttpClient client = new HttpClient();

        static async Task<string> GetFromHttpClient(string url)
        {
            var responseBody = String.Empty;
            HttpStatusCode statusCode = default;

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
                statusCode = response.StatusCode;
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nUnexpected Exception in http request");
                Console.WriteLine($"Message :{e.Message}");
                Console.WriteLine(url);
            }

            return statusCode == HttpStatusCode.OK ? responseBody : "";
        }

        static IEnumerable<string> LemmatizeWordsArray(IEnumerable<string> words)
        {
            return words.Select(word => Lemmatizer.Lemmatize(word));
        }
    }
}