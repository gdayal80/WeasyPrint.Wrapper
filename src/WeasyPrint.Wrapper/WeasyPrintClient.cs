namespace WeasyPrint.Wrapper
{
    using WeasyPrint.Wrapper.Internals;
    using System.Runtime.InteropServices;
    using System.Text;

    public class WeasyPrintClient : IWeasyPrintClient
    {
        private string workingDirectory;
        private readonly FilesManager _fileManager;
        private ProcessInvoker? _invoker;
        private readonly ITraceWriter _trace;
        
        public delegate void WeasyPrintEventHandler(OutputEventArgs e);
        public event WeasyPrintEventHandler? OnDataError;

        public WeasyPrintClient(ITraceWriter traceWriter, string workingDirectory)
        {
            this.workingDirectory = workingDirectory;

            _fileManager = new FilesManager(workingDirectory);

            _trace = traceWriter;
        }

        public byte[]? GeneratePdf(string htmlText)
        {
            byte[]? result;

            try
            {
                result = GeneratePdfInternal(htmlText).GetAwaiter().GetResult();

                return result;
            }
            catch (Exception ex)
            {
                OnDataError?.Invoke(new OutputEventArgs(ex.ToString()));

                throw new WeasyPrintException(ex.Message, ex);
            }

        }

        public async Task<byte[]?> GeneratePdfAsync(string htmlText)
        {
            byte[]? result;

            try
            {
                result = await GeneratePdfInternal(htmlText).ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());

                throw new WeasyPrintException(ex.Message, ex);
            }
        }

        public void GeneratePdf(string inputPathFile, string outputPathFile)
        {
            try
            {
                GeneratePdfInternal(inputPathFile, outputPathFile).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                throw new WeasyPrintException(ex.Message, ex);
            }

        }

        public async Task GeneratePdfAsync(string inputPathFile, string outputPathFile)
        {
            try
            {
                await GeneratePdfInternal(inputPathFile, outputPathFile).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());

                throw new WeasyPrintException(ex.Message, ex);
            }
        }

        public byte[]? GeneratePdfFromUrl(string url)
        {
            byte[]? result;

            try
            {
                result = GeneratePdfFromUrlInternal(url).GetAwaiter().GetResult();

                return result;

            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                throw new WeasyPrintException(ex.Message, ex);
            }
        }

        public async Task<byte[]?> GeneratePdfFromUrlAsync(string url)
        {
            byte[]? result;

            try
            {
                result = await GeneratePdfFromUrlInternal(url);

                return result;
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());

                throw new WeasyPrintException(ex.Message, ex);
            }
        }

        private async Task<byte[]?> GeneratePdfInternal(string htmlText)
        {
            byte[]? result;

            _trace.Info($"workingDirectory:{workingDirectory}");

            var argsPrepend = string.Empty;
            var shellName = "weasyprint";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shellName = "cmd.exe";
                argsPrepend = "/c";
            }

            var data = Encoding.UTF8.GetBytes(htmlText);
            var fileName = $"{Guid.NewGuid().ToString().ToLower()}";

            var inputFileName = $"{fileName}.html";
            var outputFileName = Path.Combine(workingDirectory, $"{fileName}.pdf");

            var fullFilePath = await _fileManager.CreateFile(inputFileName, data)
                .ConfigureAwait(false);

            var cmd = !string.IsNullOrEmpty(argsPrepend) ? $"{argsPrepend} weasyprint \"{fullFilePath}\" \"{outputFileName}\" -e utf8" : $"\"{fullFilePath}\" \"{outputFileName}\" -e utf8";

            _invoker = new ProcessInvoker(workingDirectory, shellName, cmd, false, Encoding.UTF8, _trace);

            await _invoker.ExcuteAsync(workingDirectory, shellName, cmd)
                .ConfigureAwait(false);

            await _fileManager.Delete(fullFilePath)
                .ConfigureAwait(false);

            result = await _fileManager.ReadFile(outputFileName)
                .ConfigureAwait(false);

            await _fileManager.Delete(outputFileName)
                .ConfigureAwait(false);

            return result;
        }

        private async Task GeneratePdfInternal(string inputPathFile, string outputPathFile)
        {
            if (!File.Exists(inputPathFile))
                throw new FileNotFoundException();

            _trace.Info($"workingDirectory:{workingDirectory}");

            var argsPrepend = string.Empty;
            var shellName = "weasyprint";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shellName = "cmd.exe";
                argsPrepend = "/c";
            }

            var cmd = !string.IsNullOrEmpty(argsPrepend) ? $"{argsPrepend} weasyprint \"{inputPathFile}\" \"{outputPathFile}\" -e utf8" : $"\"{inputPathFile}\" \"{outputPathFile}\" -e utf8";

            _invoker = new ProcessInvoker(workingDirectory, shellName, cmd, false, Encoding.UTF8, _trace);

            await _invoker.ExcuteAsync(workingDirectory, shellName, cmd)
                .ConfigureAwait(false);
        }

        private async Task<byte[]?> GeneratePdfFromUrlInternal(string url)
        {
            byte[]? result;

            _trace.Info($"workingDirectory:{workingDirectory}");

            var argsPrepend = string.Empty;
            var shellName = "weasyprint";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shellName = "cmd.exe";
                argsPrepend = "/c";
            }

            var fileName = $"{Guid.NewGuid().ToString().ToLower()}.pdf";

            var outputFileName = Path.Combine(workingDirectory, $"{fileName}.pdf");

            var cmd = !string.IsNullOrEmpty(argsPrepend) ? $"{argsPrepend} weasyprint \"{url}\" \"{outputFileName}\" -e utf8" : $"\"{url}\" \"{outputFileName}\" -e utf8";

            _invoker = new ProcessInvoker(workingDirectory, shellName, cmd, false, Encoding.UTF8, _trace);

            await _invoker.ExcuteAsync(workingDirectory, shellName, cmd)
                .ConfigureAwait(false);


            result = await _fileManager.ReadFile(outputFileName)
                .ConfigureAwait(false);

            await _fileManager.Delete(fileName)
                .ConfigureAwait(false);

            return result;
        }

        private void LogError(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            OnDataError?.Invoke(new OutputEventArgs(data));

            _trace?.Info($"Error: {data}");
        }

        public void Dispose()
        {
            _invoker?.Dispose();
        }
    }
}
