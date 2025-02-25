namespace WeasyPrint.Wrapper
{
    public class FilesManager
    {
        public string FolderPath { get; }

        public FilesManager(string path)
        {
            FolderPath = path;
        }

        public Task<string> CreateFile(string fileName, byte[] data)
        {
            return Task.Run(() =>
            {
                var path = Path.Combine(FolderPath, fileName);

                File.WriteAllBytes(path, data);

                return path;
            });
        }

        public Task Delete(string fileName)
        {
            return Task.Run(() =>
            {
                var path = Path.Combine(FolderPath, fileName);

                if (File.Exists(path))
                    File.Delete(path);
            });
        }

        public Task<byte[]?> ReadFile(string fileName)
        {
            return Task.Run(() =>
            {
                var path = Path.Combine(FolderPath, fileName);
                byte[]? bytes;

                if (File.Exists(path))
                    bytes = File.ReadAllBytes(path);
                else
                    bytes = null;

                return bytes;
            });
        }
    }
}
