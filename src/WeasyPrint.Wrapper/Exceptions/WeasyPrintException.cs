namespace WeasyPrint.Wrapper
{
    [Serializable]
    public class WeasyPrintException : Exception
    {

        public WeasyPrintException() { }
        public WeasyPrintException(string message) : base(message) { }
        public WeasyPrintException(string message, Exception inner) : base(message, inner) { }
        protected WeasyPrintException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) { }
    }
}
