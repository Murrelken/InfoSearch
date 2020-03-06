using System;
using System.Threading.Tasks;

namespace InfoSearch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var pageDownloader = new PageDownloader();
                await pageDownloader.Run();
                
                var booleanSearcher = new BooleanSearcher();
                booleanSearcher.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine("\nUnexpected Exception in app");
                Console.WriteLine($"Message :{e.Message}");
            }
        }
    }
}