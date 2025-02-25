namespace WeasyPrint.Wrapper.Test
{
    using WeasyPrint.Wrapper.Test.Helpers;
    using System.Reflection;

    public class WeasyPrintClientTest
    {
        private static readonly string _workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        private readonly string _inputFolder = Path.Combine(_workingDir, "inputs");
        private readonly string _outputFolder = Path.Combine(_workingDir, "outputs");
        
        [Fact]
        public async Task Should_Create_Pdf_From_Input_Text_Async()
        {
            var trace = new DebugTraceWriter();

            using (WeasyPrintClient client = new WeasyPrintClient(trace, _workingDir))
            {
                var data = await client.GeneratePdfAsync("<h1>Hello World </h1>");

                Assert.NotNull(data);
            }
        }

        [Fact]
        public void Should_Create_Pdf_From_Input_Text()
        {
            var trace = new DebugTraceWriter();

            using (WeasyPrintClient client = new WeasyPrintClient(trace, _workingDir))
            {
                var data = client.GeneratePdf("<h1>Hello World </h1>");

                Assert.NotNull(data);
            }
        }

        [Fact]
        public void Should_Create_Pdf_From_Input_File()
        {
            var trace = new DebugTraceWriter();

            var input = $"{_inputFolder}\\complex.html";
            var output = $"{_outputFolder}\\output.pdf";

            using (WeasyPrintClient client = new WeasyPrintClient(trace, _workingDir))
            {
                client.GeneratePdf(input, output);

            }
        }

        [Fact]
        public async Task Should_Create_Pdf_From_Input_File_Async()
        {
            var trace = new DebugTraceWriter();

            var input = $"{_inputFolder}\\complex.html";
            var output = $"{_outputFolder}\\output.pdf";

            using (WeasyPrintClient client = new WeasyPrintClient(trace, _workingDir))
            {
                await client.GeneratePdfAsync(input, output);

            }
        }

        [Fact]
        public async Task Should_Create_Pdf_From_Url_Async()
        {
            var trace = new DebugTraceWriter();

            var url = "https://www.google.com";

            using (WeasyPrintClient client = new WeasyPrintClient(trace, _workingDir))
            {
                var result = await client.GeneratePdfFromUrlAsync(url);

                File.WriteAllBytes($"{_outputFolder}\\url.pdf", result!);
            }
        }
    }
}
