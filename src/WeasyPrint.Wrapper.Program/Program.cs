namespace WeasyPrint.Wrapper.Program
{
    using System.Reflection;

    class Program
    {
        static async Task Main(string[] args)
        {
            string workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var trace = new ConsoleTraceWriter();

            using (WeasyPrintClient client = new WeasyPrintClient(trace, workingDir))
            {

                var html = "<!DOCTYPE html><html><body><h1>Hello World</h1></body></html>";

                var data = await client.GeneratePdfAsync(html);

                //File.WriteAllBytes("test.pdf", data!);
            }

            using (WeasyPrintClient client = new WeasyPrintClient(trace, workingDir))
            {

                var url = "https://www.google.com";

                var data = await client.GeneratePdfFromUrlAsync(url);

                //File.WriteAllBytes("url.pdf", data!);
            }

            Console.ReadLine();
        }

    }
}
