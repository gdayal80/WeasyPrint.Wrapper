namespace WeasyPrint.Wrapper
{
    public class OutputEventArgs : EventArgs
    {
        public string? Data { get; set; }

        public OutputEventArgs()
        {

        }

        public OutputEventArgs(string data)
        {
            Data = data;
        }
    }
}
